# KeyValues2 (DMX / Datamodel) support

Implemented. This document covers what is deliberately not supported, the decisions behind the
parts that are subtle, and the work that remains if the format support is ever to replace
Datamodel.NET in ValveResourceFormat.

The format itself is documented by the [Source 2 wiki](https://www.source2.wiki/FileFormats/dmx);
the behaviour here was additionally checked against Valve's own `datamodel` and `dmxloader` sources
in `cstrike15_src`. Where those two disagree, the notes below say which was followed.

---

## What is supported

| | |
|---|---|
| Binary encodings | Versions 1–5 and 9, plus the legacy `<!-- DMXVersion name_vN -->` header (version 0, laid out like version 1). Versions 6–8 never existed |
| Text encodings | `keyvalues2` versions 1–4 |
| Encoding variants | `binary`, `binary_seqids`, `keyvalues2`, `keyvalues2_flat`, `keyvalues2_noids` |
| Value types | Every DMX scalar and its array counterpart, including `uint8`, `uint64` and the prefix element |
| Conversion | Any document can be written in either encoding |

`binary_seqids` is worth noting: Datamodel.NET registers no codec for it, so files Valve wrote in
that encoding can be read here and not there.

## Not supported, by decision

- **Byte-identical re-encoding of Valve-written binaries is not a goal.** Our element index is a
  depth-first walk in stored attribute order; Valve's walks its attribute list in reverse. Round
  trips preserve meaning and attribute order, not bytes.
- **Quaternions are not normalised on load**, unlike both Valve loaders. Keeping the values as
  written is lossless; normalising is not.
- **A document with no elements is rejected.** Valve treats a NULL root as valid; there is no
  natural representation for it here, and every real file has a root.
- **No attribute flags** (`FATTRIB_DONTSAVE`), **no file-id ownership**, **no conflict-resolution
  modes** beyond first-wins on duplicate ids, and **no format upconversion**.
- **No `keyvalues` (headerless KeyValues1) DMX importer.** That is a lossy tool-side path, and the
  existing KeyValues1 support already reads the syntax.
- **No source maps for KeyValues2.** `DeserializeWithSourceMap` and `SerializeWithSourceMap` remain
  KeyValues1 and KeyValues3 only.
- **`KVSerializerOptions` does not apply.** DMX has no includes or conditionals, escape sequences
  are always on, its header is mandatory, and it carries its own string table. Options passed
  alongside a KeyValues2 format are ignored; this is noted on both enum members.
- **Listener middleware does not apply.** Merging, appending, `#base`/`#include` and conditionals
  all live in the visitor, which the KeyValues2 codecs bypass by design.

## Deliberate divergences from Valve

- The reader is **stricter** than Valve on unknown escape sequences (Valve yields a NUL byte) and on
  trailing junk after an integer.
- The reader is **more lenient** on trailing or missing commas in arrays, and accepts `"true"` and
  `"false"` for booleans, which Valve treats as a parse error.
- Text output puts a blank line after each top-level block but not after inline element blocks.
  Source 1 (`taunt05.dmx`) does the former only; Source 2 does both. Both readers accept either.

---

## Decisions worth knowing

### Element references are shared object references

A DMX attribute of type `element` holds the `KV2Element` itself, so the same instance appears at
every reference site and the graph can contain cycles. Writers collect elements with a
reference-identity visited set and emit an id reference for anything already written.

### `Guid.Empty` is the null sentinel

DMX has no separate null: an element without an identifier *is* a null reference. `KV2Element.Null`
carries `Guid.Empty` for that reason. The consequence is a sharp edge for callers — see
[Known rough edges](#known-rough-edges).

### Null and stub elements are immutable

`KV2Element.Null` is one shared instance appearing wherever any document has a null reference, so it
rejects mutation. Stubs — references to elements owned by another file, carrying an id and nothing
else — do too. Both are built with no backing collection at all, so the inherited mutating members
reject them rather than silently sharing state.

### The codecs bypass the visitor

Binary DMX is two-pass (element index, then attributes) and the data is a graph rather than a tree,
so all four codecs read and write directly. `KVObjectVisitor` therefore serves only KeyValues1 and
KeyValues3, which is why it is the single place that rejects DMX-only value types.

### The header describes the output

The encoding name and version written to a header always describe what is being written, never what
was read. Only the *format* name and version carry over, since those describe the content. A
document containing `uint8`, `uint64` or a prefix element automatically selects binary version 9 or
keyvalues2 version 4, which are the first that can encode them.

### Attribute order is preserved

Valve's attribute list prepends on load and is walked head-first on save, so a Valve round trip
preserves file order; ours does too. This is why the dictionary-backed `KVObject` collection uses
`OrderedDictionary` — order has to survive removals, not just insertions. KeyValues3 output became
deterministic as a side effect.

### The prefix container's id decides whether binary writes it twice

Hammer stores the prefix attributes twice in binary version 9: in the container before the string
table, and again as an element right after the root that nothing references. Valve's other tools
write the container alone, so neither shape can be the default. The container's own id is the tie
breaker, because that second copy is the only place an id can be written or read: a container that
has one is written both ways, a container without one is written as a container only. Reading a file
of either shape and writing it back therefore reproduces it.

---

## Known rough edges

Ranked by how likely a consumer is to hit them.

### 1. An element without an id is silently written as null

```csharp
var child = new KV2Element("DmeChild", "child", default);  // no Guid.NewGuid()
root.Add("child", child);
// round-trips back as KV2Element.Null
```

`Guid.Empty` doubles as the null-reference sentinel, so an element the caller forgot to give an id
becomes null on write. The root case throws; children do not. **Decision needed:** assign an id in
the constructor when none is given, or throw on write the way the root does. Assigning is friendlier
and costs nothing, since any element being written needs an id anyway. It would, however, also give
every prefix container an id, which is what decides whether binary version 9 writes the container
twice, so that path needs its own signal first.

### 2. Typed arrays are read in a way that suits small files

`ReadTypedArray` takes a `Func<T>` and calls `BinaryReader` once per component, so a one-million
entry `Vector3Array` performs three million stream reads plus a million delegate invocations for a
single attribute. The writer mirrors this. Since DMX files are multi-megabyte model and map data,
this is the wrong shape for the primary workload.

For the blittable item types — `int`, `float`, `ulong`, `byte`, `Vector2/3/4`, `Quaternion`,
`QAngle`, `Matrix4x4`, `DmxTime`, `DmxColor` — the payload can be read or written in one call over
`MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(list))`. Element, string, blob and bool arrays keep
dedicated loops. **This should be done with a benchmark**, because it assumes the CLR struct layouts
match the wire format exactly.

### 3. Typed saving does not exist

`Serialize<TData>(stream, someObject, name)` cannot produce DMX: a DMX element is identified by a
class name and a GUID, and an arbitrary object carries neither. It throws a message pointing at
`KV2Element`. Typed *loading* works. Closing this needs the mapping layer below.

### 4. Smaller friction

- `KVObject.TypedArray(new List<int> { … })` is more ceremony than an array literal.
- Values come back as `KVObject`, so reading needs `GetValue<Vector3>()` or `GetArray<T>()`.
- `PrefixElement` needs a cast to `KV2Document`.

---

## Replacing Datamodel.NET in ValveResourceFormat

ValveResourceFormat consumes Datamodel.NET as the NuGet package `KeyValues2`, referenced once from
`ValveResourceFormat.csproj`. Everything it does with the DMX *format* is already supported here:

| What it uses | Where |
|---|---|
| `binary 9` | vmap, model meshes, animations, cloth, physics hulls — six write sites |
| `keyvalues2 4` | `ModelExtract.ToDmxSkeleton`, the GUI DMX viewer |
| `keyvalues2_noids 1` | `TextureExtract` vtex output |
| Formats `model 22`, `vmap 29`, `vtex 1` | throughout |
| `$prefix_element$` | `MapExtract.ToValveMap` writes `map_asset_references` |
| Scalars and arrays | element, int, float, bool, string, time, color, vector2/3/4, quaternion, qangle, uint64, and the array forms |

**The blocker is not the format — it is the object model.** Roughly 55 `Element` subclasses across
`DmxModel.cs`, `ValveMap.cs` and `ValveTexture.cs` declare CLR properties that become DMX attributes
via reflection. That is how VRF authors DMX, and there is no equivalent here.

Referenced but inert, so safe to drop: `DeferredMode` (always `Disabled`). Never referenced at all:
stubs, `ImportElement`, format upconversion, cloning, attribute flags, `OverrideType`.

### What a mapping layer would need

The library already has a typed model: `ObjectCopier` backs `Deserialize<TObject>` and
`Serialize<TData>` for KeyValues1 and KeyValues3, with `IObjectReflector`/`IObjectMember` as the
member abstraction. Several pieces carry over directly — `[KVProperty("3dcameras")]` covers
`[DMProperty(name:)]` one-for-one (VRF has 37 uses), `[KVIgnore]` covers opting out, and name
matching is already case-insensitive so camelCase attribute names line up with C# properties without
a convention attribute. `[DMProperty(optional:)]` needs no replacement at all: it is dead code in
Datamodel.NET, set but never read.

What is missing, in rough order of difficulty:

**A. Construction and defaults.** `MakeObject` builds objects with
`RuntimeHelpers.GetUninitializedObject`, so no constructor runs. VRF depends on constructor-time
behaviour throughout: `{ get; } = []` initialisers, non-zero defaults like `Visible = true`, and
`DmeTypedLog<T>` which computes its own `ClassName` and `Name`. Related: `PropertyMember` sets
values with `propertyInfo.SetValue`, which throws on the get-only pre-initialised collections VRF
uses heavily — Datamodel.NET populates those in place instead. And `ConvertObjectToValue` skips null
members, where Datamodel writes property-derived attributes unconditionally. **These three are the
most likely to force a redesign and should be tackled first.**

**B. Graph semantics.** `ConvertObjectToValue` keeps a `HashSet<object>` and throws on a second
visit, which rejects any repeated reference rather than just cycles. DMX needs the opposite in both
directions: same instance → same element on save, same element → same instance on load. On save that
is a cheap swap for a reference-keyed identity map, since the set is already threaded through. On
load there is no context parameter at all, so a map has to be threaded through `MakeObject`,
`CopyObject`, both `ConvertValue` overloads, the enumerable builders and every `InvokeGeneric`
argument array.

**C. Element metadata.** `ClassName`, `Name` and `ElementId` are not attributes and need attributes
or an interface to bind them, settable on load. **Decision needed:** what happens when a POCO
exposes no id — synthesising one per save means load→save changes every id in the file.

**D. Polymorphic loads.** `CMapWorld.Children` is an element array holding a heterogeneous mix
sharing a `MapNode` base. Saving takes the class name from the CLR type; loading cannot, so it needs
a class-name → `Type` registry. Note the project sets `IsAotCompatible`: assembly scanning is
trim-hostile, so explicit registration is the safe form, and closed generics like `DmeLog<float>`
have to be registered by hand. Also, `Deserialize<TObject>` currently drops its `options` before
calling the copier, so the natural home for a registry needs plumbing first.

**E. Naming conventions and ordering.** Datamodel's three class-level conventions plug into
`IObjectMember.Name`. Only `[HungarianProperties]` matters in both directions, since the others are
covered by case-insensitive matching — and note its rule is `"m_" + annotation + PropertyName`, with
the property name keeping its capital when an annotation applies. Separately, `DmeVertexData`
depends on property-derived attributes being emitted before dynamically added ones, so a POCO needs
somewhere to carry dynamic attributes.

**F. The prefix element.** The typed entry points have nowhere to put it, so VRF's vmap path cannot
be migrated until that is designed.

### Open design question

Whether to extend `ObjectCopier` or split it into a shared core with two walkers. Extending reuses
the member walk, collection handling and entry points — but point A above means three existing
behaviours must *change* under a format flag, and those three decide whether output matches what
Valve's tools expect. The strongest argument for one copier is a single attribute vocabulary across
all four formats. **This should be decided once the shape of A is concrete, not before.**

### Cost

Migration is dominated by porting the ~55 content-format classes — attribute renames *and* property
type changes (`TimeSpan` → `DmxTime`, `Datamodel.Color` → `DmxColor`, `Array<T>` → `List<T>`) — plus
roughly 15 imperative call sites, in VRF's most output-sensitive paths. The payoff is one fewer
dependency and a simpler element model, not new capability.
