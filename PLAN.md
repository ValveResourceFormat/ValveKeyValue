# KV2 (DMX / Datamodel Exchange) Format Support

## Context

DMX is Valve's typed data format used by Source engine tools (SFM, Hammer, model compiler, particle editor). Unlike KV1/KV3 (trees), DMX is a flat set of elements that reference each other by GUID. Each element has a class name, a name, a unique GUID, and typed attributes. Attributes can reference other elements, forming arbitrary graphs.

The library currently supports KV1 text, KV1 binary, and KV3 text. A private `KV2Element` placeholder already exists. Goal: add read/write support for DMX binary and DMX text (keyvalues2) formats while reusing existing infrastructure.

### Binary encoding versions

Versions 6-8 do not exist — Valve skipped from v5 to v9. Valid versions: `{1, 2, 3, 4, 5, 9}`.

| Version | Key change |
|---------|-----------|
| v1 | No string table (inline null-terminated strings everywhere) |
| v2 | Per-element string table, `short` string count, `short` string indices |
| v3 | `AT_OBJECTID` removed, replaced by `AT_TIME` at same slot |
| v4 | Global string table, `int` string count, `short` string indices |
| v5 | `int` string indices (supports >65535 unique strings) |
| v9 | Prefix attribute containers, `AT_UINT8`/`AT_UINT64` types, **new type ID encoding** |

Text encoding versions 1-4 exist (CS2 vmaps use v4). No version-specific branching needed for text — the syntax is the same across versions.

### Type ID encoding across versions

The byte written for each attribute type changed between versions. Three distinct encodings exist:

**IDv1 (v1-v2)**: Scalars 1-14, arrays 15-28 (contiguous). Slot 7 = `AT_OBJECTID` (16-byte UUID).

**IDv2 (v3-v5)**: Scalars 1-14, arrays 15-28 (contiguous). Slot 7 = `AT_TIME` (replaces `AT_OBJECTID`).

**IDv3 (v9)**: Scalars 1-16 (adds `UINT64`=15, `UINT8`=16). Arrays start at **33** (scalar ID + 32). Gap between 17-32 is unused.

```
IDv3 mapping: Element=1, Int32=2, Float=3, Bool=4, String=5, BinaryBlob=6,
              Time=7, Color=8, Vector2=9, Vector3=10, Vector4=11,
              QAngle=12, Quaternion=13, Matrix4x4=14, UInt64=15, UInt8=16
              ElementArray=33, Int32Array=34, ... UInt64Array=47, UInt8Array=48
```

The `DmxAttributeType` enum must have `DecodeID(byte raw, IDVersion)` and `EncodeID(IDVersion)` methods.

### Encoding and format names

DMX headers carry independent encoding + format pairs:

```
<!-- dmx encoding binary 5 format dmx 1 -->
<!-- dmx encoding keyvalues2 1 format dmx 1 -->
```

- **Encoding**: how data is physically stored (`binary`, `keyvalues2`, `keyvalues2_flat`)
- **Format**: semantic content type (`dmx`, `sfm_session`, `pcf`, etc.)
- Both have independent version numbers
- `keyvalues2_flat` is a variant that writes all elements at root level — we treat it identically to `keyvalues2` on read

---

## Design Decisions

### 1. Extend KV3ID with a Version integer

`KV3ID(string Name, Guid Id)` becomes `KV3ID(string Name, Guid Id = default, int Version = 0)`.

- KV3: `new KV3ID("text", Encoding.Text)` — unchanged
- DMX: `new KV3ID("binary", Version: 5)` — Id left as default
- **KVHeader stays as-is** (Encoding + Format, both KV3ID)

### 2. KVDocument gets a Root property

Add `KVObject Root` to KVDocument. The constructor already copies `_scalar`/`_ref` from root, but loses subclass identity (KV2Element's ElementId/ClassName/Name). `Root` preserves the original object.

- `Deserialize()` returns KVDocument — Root may be a KV2Element
- Consumer checks `doc.Root is KV2Element`
- No KV2Document class needed

### 3. KV2 readers bypass the visitor pattern

DMX binary is two-pass (element index then attributes) and the data is a graph, not a tree. Readers directly construct KV2Element objects, wire up element references as shared C# object references, and return a KVDocument.

### 4. Element references are shared C# object references

When a DMX attribute is of type "element", its value is the actual KV2Element object. The same instance appears wherever it's referenced. The serializer detects element references via `value is KV2Element`.

- **Null references**: static `KV2Element.Null` sentinel with `ElementId = Guid.Empty`
- **Writer cycle safety**: `HashSet<KV2Element>` visited set during traversal (reference identity)
- **External/stub references** (binary index -2): Even Valve's own dmxloader doesn't support reading these (logs warning, returns null). We do the same — treat as null reference.

### 5. Explicit KVValueType entries for all types (mirror C++ enum)

Follow Valve's C++ pattern: every scalar type AND every array type gets its own KVValueType entry. No runtime type guessing from `List<T>`. The reader maps `DmxAttributeType` directly to `KVValueType`, the writer switches on `KVValueType` directly.

Arrays still store `List<T>` in `_ref` (e.g., `List<int>` for `Int32Array`), but the type is known from `KVValueType`, not inferred.

### 6. Custom value types for QAngle, Color, Time

```csharp
public readonly record struct QAngle(float Pitch, float Yaw, float Roll);
public readonly record struct DmxColor(byte R, byte G, byte B, byte A);
public readonly record struct DmxTime(int Ticks);  // tenths of milliseconds (0.1ms = 0.0001s)
```

Used for both scalar values (boxed in `_ref`) and typed arrays (`List<QAngle>`, etc.).

### 7. KV1/KV3 serializers use a default throw for unknown types

Instead of adding individual guard clauses for every new DMX type, existing KV1/KV3 serializers just use a `default` case in their switch statements that throws. This keeps the existing code minimal — no per-type guards needed.

### 8. StringTable is 100% reusable

No changes needed. DMX binary uses the same pattern as KV1 binary.

### 9. Text format "name" attribute handling

In C++, `CDmElement` has a built-in `m_Name` member (`CDmaString`). When the text reader encounters `"name" "string" "value"`, normal attribute creation sets this built-in — no special-case code in C++.

In our design, `KV2Element.Name` is a C# property, not a child attribute. So our text reader must recognize two pseudo-attributes and route them to properties:

- `"id"` with type `"elementid"` → sets `KV2Element.ElementId`
- `"name"` with type `"string"` → sets `KV2Element.Name`

In binary format, both name and className are stored in the element index (not as attributes), so the binary reader sets them directly from the element dictionary.

### 10. AT_OBJECTID deprecation

Binary v3+ replaced `AT_OBJECTID` with `AT_TIME` at the same enum slot. If we encounter an attribute at this slot in pre-v3 files, read past the 16-byte object ID and skip the attribute. No need to preserve it.

### 11. Binary attribute write order

The C++ datamodel serializer writes attributes in **reverse** order (`for (i = nAttributes - 1; i >= 0; --i)`). Our reader handles whatever order appears. Our writer should match this for compatibility.

---

## KVValueType Additions

Mirrors Valve's `DmAttributeType_t`. Scalar types:

```csharp
// DMX scalar types
Byte,           // _scalar
Color,          // _ref DmxColor
TimeSpan,       // _ref DmxTime
Vector2,        // _ref Numerics.Vector2
Vector3,        // _ref Numerics.Vector3
Vector4,        // _ref Numerics.Vector4
QAngle,         // _ref QAngle
Quaternion,     // _ref Numerics.Quaternion
Matrix4x4,      // _ref Numerics.Matrix4x4
```

Array types (each maps 1:1 to the C++ `AT_*_ARRAY` enum):

```csharp
// DMX array types
ElementArray,    // _ref List<KV2Element>
Int32Array,      // _ref List<int>
FloatArray,      // _ref List<float>
BooleanArray,    // _ref List<bool>
StringArray,     // _ref List<string>
BinaryBlobArray, // _ref List<byte[]>
TimeSpanArray,   // _ref List<DmxTime>
ColorArray,      // _ref List<DmxColor>
Vector2Array,    // _ref List<Vector2>
Vector3Array,    // _ref List<Vector3>
Vector4Array,    // _ref List<Vector4>
QAngleArray,     // _ref List<QAngle>
QuaternionArray, // _ref List<Quaternion>
Matrix4x4Array,  // _ref List<Matrix4x4>
ByteArray,       // _ref List<byte>     (v9)
UInt64Array,     // _ref List<ulong>    (v9)
```

Total: 9 scalar + 16 array = 25 new entries.

---

## Type System Mapping

| DMX Type | KVValueType (scalar) | KVValueType (array) | Scalar storage | Array storage |
|----------|---------------------|---------------------|---------------|---------------|
| element | Collection (is KV2Element) | `ElementArray` | children dict | `List<KV2Element>` |
| int | `Int32` | `Int32Array` | `_scalar` | `List<int>` |
| float | `FloatingPoint` | `FloatArray` | `_scalar` (bits) | `List<float>` |
| bool | `Boolean` | `BooleanArray` | `_scalar` | `List<bool>` |
| string | `String` | `StringArray` | `_ref` string | `List<string>` |
| binary | `BinaryBlob` | `BinaryBlobArray` | `_ref` byte[] | `List<byte[]>` |
| time | `TimeSpan` | `TimeSpanArray` | `_ref` DmxTime | `List<DmxTime>` |
| color | `Color` | `ColorArray` | `_ref` DmxColor | `List<DmxColor>` |
| vector2 | `Vector2` | `Vector2Array` | `_ref` Numerics.Vector2 | `List<Vector2>` |
| vector3 | `Vector3` | `Vector3Array` | `_ref` Numerics.Vector3 | `List<Vector3>` |
| vector4 | `Vector4` | `Vector4Array` | `_ref` Numerics.Vector4 | `List<Vector4>` |
| qangle | `QAngle` | `QAngleArray` | `_ref` QAngle | `List<QAngle>` |
| quaternion | `Quaternion` | `QuaternionArray` | `_ref` Numerics.Quaternion | `List<Quaternion>` |
| matrix | `Matrix4x4` | `Matrix4x4Array` | `_ref` Numerics.Matrix4x4 | `List<Matrix4x4>` |
| byte (v9) | `Byte` | `ByteArray` | `_scalar` | `List<byte>` |
| uint64 (v9) | `UInt64` | `UInt64Array` | `_scalar` | `List<ulong>` |

---

## New Files

### `ValveKeyValue/QAngle.cs`
`public readonly record struct QAngle(float Pitch, float Yaw, float Roll)`

### `ValveKeyValue/DmxColor.cs`
`public readonly record struct DmxColor(byte R, byte G, byte B, byte A)`

### `ValveKeyValue/DmxTime.cs`
`public readonly record struct DmxTime(int Ticks)` — tenths of milliseconds (matches C++ `DmeTime_t.m_tms`)

### `ValveKeyValue/KeyValues2/DmxAttributeType.cs`
Internal enum mirroring Valve's `DmAttributeType_t` (unified, version-independent values). Three static decode/encode methods for the three on-disk ID versions:

```csharp
// Unified enum (our internal representation, matches IDv3 scalar values)
enum DmxAttributeType : byte { Element=1, Int32=2, ..., UInt64=15, UInt8=16,
                                ElementArray=33, Int32Array=34, ..., UInt8Array=48 }

static DmxAttributeType DecodeID(byte raw, IDVersion version);  // disk byte → enum
static byte EncodeID(DmxAttributeType type, IDVersion version); // enum → disk byte
static KVValueType ToKVValueType(DmxAttributeType type);        // → KVValueType
```

IDVersion is `{V1, V2, V3}` — selected based on encoding version (v1-2 → V1, v3-5 → V2, v9 → V3).

### `ValveKeyValue/Deserialization/KeyValues2/KV2BinaryReader.cs`
Binary DMX reader. Directly builds KV2Elements (no visitor).

Pipeline:
1. Parse text header `<!-- dmx encoding binary N format F V -->\0` (read chars until null byte)
2. If v9+: read prefix attribute containers (inline strings, before string table)
3. Read string dictionary (null-terminated strings, count depends on version; skip for v1)
4. Read element count (`int`), then element index (className/name string indices + 16-byte GUID per element) → create KV2Element stubs
5. Read attribute count per element (`int`), then for each: name string index, type byte (decode via `DmxAttributeType.DecodeID`), value data → populate children, element refs wired as shared objects
6. Return KVDocument with Root = elements[0]

Compute version flags upfront (sourcepp pattern):
```csharp
var idVersion = version < 3 ? IDVersion.V1 : version < 9 ? IDVersion.V2 : IDVersion.V3;
var hasStringTable = version > 1;
var namesInStringTable = version > 3;
var stringCountIsInt = version >= 4;
var stringIndicesAreInt = version >= 5;
var hasPrefixAttributes = version > 5;
```

Version-specific behavior:
- **v1**: No string table — all strings inline null-terminated
- **v2-v3**: `short` string count, `short` string indices
- **v4**: `int` string count, `short` string indices
- **v5**: `int` string count, `int` string indices
- **v9**: New type ID encoding (IDv3), prefix attribute containers, `AT_UINT8`/`AT_UINT64`
- **Pre-v3**: `AT_TIME` slot contains `AT_OBJECTID` — skip 16 bytes and discard

v9 prefix attribute containers (read BEFORE string dictionary):
```
int32 prefixContainerCount
for each container:
    int32 attributeCount
    for each attribute:
        string name (inline, NOT from string table — table not read yet)
        byte typeID (IDv3 encoding)
        value data
```
Only the first container (index 0) is typically meaningful. Store as `List<KeyValuePair<string, KVObject>>` on the KVDocument or KV2Element root.

Element reference encoding in attribute values:
- `int >= 0`: index into element list
- `int == -1` (`ELEMENT_INDEX_NULL`): null reference
- `int == -2` (`ELEMENT_INDEX_EXTERNAL`): external reference — treat as null (unsupported, matches Valve behavior)

Note: all binary data is native byte order (little-endian on x86).

### `ValveKeyValue/Deserialization/KeyValues2/KV2TextReader.cs`
Text (keyvalues2) reader. Text encoding versions 1-4 exist but syntax is identical — no version branching needed.

Parses header line then element bodies. Supports `//` line comments.

Element syntax:
```
"ClassName"
{
    "id" "elementid" "550e8400-e29b-41d4-a716-446655440000"
    "name" "string" "my_element"
    "attr" "int" "42"
    "child" "ChildType"                         // inline element (type name, not "element")
    {
        "id" "elementid" "..."
    }
    "ref" "element" "550e8400-..."              // GUID reference
    "nullref" "element" ""                       // null reference (empty string)
    "values" "int_array"                         // typed array
    [
        "1",
        "2",
        "3"
    ]
    "elements" "element_array"                     // element array (has explicit type name!)
    [
        "InlineType"
        {
            "id" "elementid" "..."
        },
        "element" "550e8400-..."                // GUID ref in array
    ]
}
```

Pseudo-attributes `"id"` (type `"elementid"`) and `"name"` (type `"string"`) route to `KV2Element.ElementId` and `KV2Element.Name` respectively.

Pseudo-attributes must be checked AFTER reading the type name (not before), because the reader needs to consume the type token regardless.

Inline elements created immediately. GUID references resolved at end after all elements parsed.

### Text format issues discovered during implementation

These were not anticipated in the original plan and were found by running against real Valve test files:

**1. `element_array` has an explicit type name** — The original plan assumed element arrays use bare brackets (`"attr" [...]`). In reality, the format uses `"attr" "element_array" [...]` with an explicit type name before the bracket, just like typed arrays. The reader must check for `typeName == "element_array"` and route to the element array reader, not the typed array parser.

**2. `uint64` values use hex notation** — Real files (e.g., CS2 vmaps) write uint64 as `"0xdf41645d6af564a"` with a `0x` prefix. The reader must detect the prefix and parse with `NumberStyles.HexNumber`.

**3. Binary blob hex data contains whitespace** — The `"binary"` type in text format stores hex strings that span multiple lines with leading whitespace (tabs, newlines). The reader must strip all whitespace from the hex string before parsing.

**4. Text encoding version is NOT always 1** — sourcepp validates keyvalues2 text encoding versions 1-4, and CS2 vmap files use `encoding keyvalues2 4`. The reader should accept any version for text (no version-specific branching needed, but validation should not reject v4).

**5. `$prefix_element$` in text format** — v4+ text files can have a special `"$prefix_element$"` as a top-level element before the real root. This is the text equivalent of binary v9 prefix attribute containers. The reader currently returns `topLevelElements[0]` as root, which may be this prefix element instead of the actual root. Needs consideration for how to expose prefix elements via the API.

### `ValveKeyValue/Serialization/KeyValues2/KV2BinaryWriter.cs`
Binary writer. Walks from root with `HashSet<KV2Element>` visited set to collect all unique elements (including inside element arrays). Builds string dict via StringTable, writes header + dict + element index + attributes.

### `ValveKeyValue/Serialization/KeyValues2/KV2TextWriter.cs`
Text writer. Pre-pass counts references to decide inline vs GUID:

- Element referenced **once**: inlined at usage site (write ClassName + `{...}`)
- Element referenced **multiple times**: write `"element" "GUID"` at usage sites, full definition written separately
- Element that is **null**: write `"element" ""`
- Root elements always written as top-level definitions

GUIDs written as RFC 4122 strings: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`.

---

## Files to Modify

### `ValveKeyValue/KeyValues3/KV3ID.cs`
Add `int Version = 0` parameter.

### `ValveKeyValue/KVValueType.cs`
Add 25 entries: 9 scalar types + 16 array types (listed above).

### `ValveKeyValue/KV2Element.cs`
- Make **public** (remove pragma warning suppression)
- Add `string Name` property (element instance name, distinct from ClassName)
- Add constructor `KV2Element(string className, string name, Guid id)`
- Add static `KV2Element.Null` sentinel for null element references
- Add dict-backed constructor for reader

### `ValveKeyValue/KVDocument.cs`
Add `KVObject Root` property. Store reference to original root in constructor.

### `ValveKeyValue/KVSerializationFormat.cs`
Add `KeyValues2Text` and `KeyValues2Binary`.

### `ValveKeyValue/KVSerializer.cs`
Add cases for KV2Binary/KV2Text in `Deserialize()` and `Serialize()`. No new public methods.

### `ValveKeyValue/KVObject.cs`
- Add constructors for new scalar types
- Add `List<T> GetArray<T>()` method for typed array access
- Fix `Count` for typed arrays: use `ICollection` instead of casting to `List<KVObject>`
- Add typed accessors that unbox from `_ref`
- Update `DebuggerDescription`

### `ValveKeyValue/KVObject_IConvertible.cs`
Add `ToString()` cases for new types (space-separated floats for vectors, etc.).

### `ValveKeyValue/Abstraction/KVObjectVisitor.cs`
Add new KVValueType entries to `VisitObject` switch and `IsSimpleType`. Array types handled like existing `Array` case.

### `ValveKeyValue/Serialization/KeyValues1/KV1TextSerializer.cs`
### `ValveKeyValue/Serialization/KeyValues1/KV1BinarySerializer.cs`
### `ValveKeyValue/Serialization/KeyValues3/KV3TextSerializer.cs`
Use `default` throw in switch statements. No per-type guards needed.

---

## Downsides & Ergonomic Issues

1. **KVValueType enum growth** — 25 new entries. But switch statements only need a `default` throw in KV1/KV3 serializers, and the KV2 reader/writer map directly.

2. **KV2 readers bypass visitor** — Unavoidable due to two-pass binary format. Listener middleware (merging, appending, conditions) doesn't apply to KV2.

3. **Cyclic references** — The in-memory object graph may have cycles. Writer handles this with a visited set. Any other recursive walk (ToString, ObjectCopier) needs the same care.

4. **Boxing overhead for scalars** — System.Numerics and custom structs stored boxed in `_ref`. One allocation per scalar. Arrays use `List<T>` so elements are not boxed.

5. **KV3ID unused fields** — DMX leaves `Id` as default, KV3 leaves `Version` as 0.

6. **Binary v1 support** — v1 has no string table (all inline strings). Supporting it requires a separate code path. Given how old it is, we may want to only support v2+ and throw on v1.

7. **Byte order assumption** — C++ code uses native byte order without explicit endian markers. All known DMX files are little-endian (x86). BinaryReader defaults to little-endian, so this works. Just a note — no big-endian DMX files are known to exist.

8. **Three type ID encodings** — The on-disk byte for attribute types differs across v1-2 / v3-5 / v9. The `DmxAttributeType` enum needs decode/encode methods for each version. Array offset is +14 in IDv1/IDv2 but +32 in IDv3.

9. **`$prefix_element$` in text format** — v4+ text files have a prefix element as the first top-level element. The reader returns `topLevelElements[0]` as root, which may be the prefix instead of the real root. Need to decide: skip it, expose it separately, or let the consumer handle it.

---

## Implementation Phases

### Phase 1: Core types
1. Define QAngle, DmxColor, DmxTime structs
2. Extend KV3ID with Version
3. Extend KVValueType with 25 entries (9 scalar + 16 array)
4. Add constructors, `GetArray<T>()`, and accessors to KVObject; fix Count for typed arrays
5. Update IConvertible, KVObjectVisitor; add default throw to KV1/KV3 serializers
6. Make KV2Element public, add Name property and constructors
7. Add Root property to KVDocument
8. Create DmxAttributeType enum
9. Add KVSerializationFormat entries + KVSerializer dispatch

### Phase 2: Binary reader
1. Implement KV2BinaryReader (version-specific string dict, element index, attributes, reference wiring)
2. Tests with real DMX binary files

### Phase 3: Text reader
1. Implement KV2TextReader (header, element parsing, special id/name handling, reference resolution)
2. Tests with real DMX text files

### Phase 4: Binary writer
1. Implement KV2BinaryWriter (element collection, string dict, binary output)
2. Round-trip tests

### Phase 5: Text writer
1. Implement KV2TextWriter (reference counting, inline vs root-level elements)
2. Round-trip + cross-format tests

---

## Verification

- Read known DMX binary files (v2, v4, v5, v9) → assert element count, root class, attribute values, element references
- Read known DMX text files → same assertions
- Round-trip: binary → binary, text → text, binary ↔ text
- Test null element references (`ELEMENT_INDEX_NULL`), empty typed arrays, shared element references
- Test external element references (index -2) → treated as null
- Test AT_OBJECTID in pre-v3 binary → skipped gracefully
- Test v9 prefix attribute containers → read and preserved
- Test v9 type ID encoding (IDv3: arrays at offset +32)
- Test version validation rejects v6-v8
- Test existing KV1/KV3 serializers throw on DMX-only types
- Test `doc.Root is KV2Element` pattern
- Test text format with `//` comments
- Test text format inline vs GUID referenced elements
- Existing test suite passes unchanged
