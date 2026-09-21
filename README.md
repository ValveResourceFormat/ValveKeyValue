<h1 align="center">
  <img src="https://raw.githubusercontent.com/ValveResourceFormat/ValveKeyValue/master/Misc/logo.png" alt="Logo" width="128">
  <br>Valve Key Value for .NET
</h1>

<p align="center">
  Read and write KeyValues, Valve's simple key-value pair format.
  <br />
  Used in Steam and the Source engines for configuration files, game data, and more.
  <br />
  <a href="https://www.nuget.org/packages/ValveKeyValue/">NuGet</a>
  ·
  <a href="#quick-start">Quick start</a>
  ·
  <a href="#keyvalues1">KeyValues1</a>
  ·
  <a href="#keyvalues3">KeyValues3</a>
  ·
  <a href="https://app.codecov.io/gh/ValveResourceFormat/ValveKeyValue">Coverage</a>
</p>

KeyValues files turn up as `.vdf`, `.res`, `.acf`, and more. This library aims to be fully compatible with Valve's various implementations of KeyValues format parsing (believe us, it's not consistent).

| Format | Enum value | Read | Write |
|--------|------------|------|-------|
| KeyValues1 text | `KVSerializationFormat.KeyValues1Text` | Yes | Yes |
| KeyValues1 binary | `KVSerializationFormat.KeyValues1Binary` | Yes | Yes |
| KeyValues2 (Datamodel) | none | No | No |
| KeyValues3 text | `KVSerializationFormat.KeyValues3Text` | Yes | Yes |
| KeyValues3 binary | none | No | No |

For KeyValues2 (Datamodel), use our fork of [Datamodel.NET](https://github.com/ValveResourceFormat/Datamodel.NET) instead.

## Quick start

```csharp
using ValveKeyValue;

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

// Read
using var input = File.OpenRead("gameinfo.txt");
KVDocument data = kv.Deserialize(input);

string game = (string)data["game"];
int appId = (int)data["FileSystem"]["SteamAppId"];

// Modify
data.Root["game"] = "My Mod";

// Write
using var output = File.OpenWrite("gameinfo_modified.txt");
kv.Serialize(output, data);
```

Reading a KV3 file is the same with `KVSerializationFormat.KeyValues3Text`. If you would rather work with your own classes than with `KVObject`, see [Typed objects](#typed-objects).

## Core types

The library is built around two types:

- **`KVObject`** is a single value node. It can hold a scalar (string, int, float, bool, etc.), a binary blob, an array, or a named collection of children. Keys are stored in the parent container rather than on the child, similar to how JSON works. It implements `IReadOnlyDictionary<string, KVObject>` and `IConvertible`.
- **`KVDocument`** is what you get back from deserializing. It contains the `Root` object, the root key `Name`, and a `Header` that is only set for KV3 documents. It has a read-only string indexer that delegates to `Root` and converts implicitly to `KVObject`.

The same types are used for KV1 and KV3, so you can deserialize from one format and serialize to the other. Not every value type is supported by every format though:

| Feature | KV1 Text | KV1 Binary | KV3 Text |
|---------|----------|------------|----------|
| Collections | Yes (list-backed, allows duplicate keys) | Yes (list-backed) | Yes (dict-backed, O(1) lookup) |
| Arrays | Emulated as objects with numeric keys (`"0"`, `"1"`, ...) | No (throws) | Yes (native) |
| Binary blobs | Written as a hex string (`"01 02 03"`), reads back as a string | Written as a hex string, reads back as a string | Yes (native) |
| Scalars | Yes | Yes | Yes |
| Flags | Ignored | Ignored | Yes |

When building objects in code, use `KVObject.Collection()` for general use and KV3 output. Use `KVObject.ListCollection()` when you need duplicate keys or want to match how KV1 files are read. Deserialization picks the right backing store for you.

## Working with KVObject

### Constructing

```csharp
// Scalar values (typed constructors)
var str = new KVObject("hello");    // string
var num = new KVObject(42);         // int
var flt = new KVObject(3.14f);      // float
var flag = new KVObject(true);      // bool

// Implicit conversion from primitives (string, bool, all integer types, float, double, IntPtr, byte[])
KVObject implicitStr = "hello";
KVObject implicitNum = 42;

// Dictionary-backed collection (O(1) lookup, no duplicate keys)
var dict = KVObject.Collection();                    // empty
var dict2 = new KVObject();                          // same as above
var dict3 = KVObject.Collection(capacity: 16);       // with initial capacity

// List-backed collection (preserves insertion order, allows duplicate keys, for KV1)
var list = KVObject.ListCollection();                // empty
var list2 = KVObject.ListCollection(capacity: 16);   // with initial capacity

// Both collection kinds can also be built from existing pairs
var fromPairs = KVObject.Collection(new Dictionary<string, KVObject>
{
    ["name"] = "Dota 2",
    ["appid"] = 570,
});

// Build up children
var obj = new KVObject();
obj["name"] = "Dota 2";                               // implicit string -> KVObject
obj["appid"] = 570;                                   // implicit int -> KVObject

// Array
var arr = KVObject.Array();                           // empty
var arr2 = KVObject.Array(capacity: 4);               // with initial capacity
var arr3 = KVObject.Array([ new KVObject("a"), new KVObject("b") ]); // from elements

// Binary blob
var blob = KVObject.Blob([ 0x01, 0x02, 0x03 ]);
KVObject implicitBlob = new byte[] { 0x01, 0x02, 0x03 }; // implicit byte[] -> KVObject

// Null value
var nul = KVObject.Null();

// Flagged value (KV3 only)
var resource = new KVObject("models/foo.vmdl") { Flag = KVFlag.Resource };
```

### Reading values

```csharp
var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
using var stream = File.OpenRead("file.vdf");
KVDocument data = kv.Deserialize(stream);

// Root key name (only on KVDocument; null for KV3 documents)
string? rootName = data.Name;

// String indexer returns KVObject (supports chaining)
string name = (string)data["config"]["name"];
int version = (int)data["version"];
float scale = (float)data["scale"];
bool enabled = (bool)data["settings"]["enabled"];

// Named conversion methods (equivalent to the casts above)
int version2 = data["version"].ToInt32();
double scale2 = data["scale"].ToDouble();
string text = data["config"]["name"].ToString();

// Array elements by index (also works on list-backed collections)
float x = (float)data["position"][0];

// Access the root KVObject for full API (mutations, ContainsKey, etc.)
KVObject root = data.Root;

// Check existence (on the root KVObject)
if (root.ContainsKey("optional")) { ... }
if (root.TryGetValue("optional", out var child)) { ... }

// Indexer throws KeyNotFoundException for missing keys
// Use TryGetValue for safe access

// Inspect the value kind
KVValueType type = root.ValueType;
bool isNull = root.IsNull;
bool isArray = root.IsArray;
bool isCollection = root.IsCollection;
int childCount = root.Count;          // 0 for scalars

// Flags and blobs (KV3)
KVFlag flag = data["texture"].Flag;
byte[] bytes = data["blob"].AsBlob();
byte[] bytes2 = (byte[])data["blob"];  // explicit cast does the same

// Direct span access to array elements
Span<KVObject> elements = data["position"].AsArraySpan();
```

### Modifying

```csharp
var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
using var stream = File.OpenRead("file.vdf");
KVDocument data = kv.Deserialize(stream);

// Mutations require the Root KVObject (KVDocument indexer is read-only)
data.Root["name"] = "new name";
data.Root["count"] = 42;

// Chained writes work (reference semantics, first lookup goes through KVDocument indexer)
data["config"]["resolution"] = "1920x1080";

// Add children to collections
data.Root.Add("newprop", 42);      // implicit int -> KVObject, throws if key exists in a dict-backed collection
data.Root.Add("text", "value");    // implicit string -> KVObject
data.Root.TryAdd("text", "other"); // returns false instead of throwing on duplicate keys

// Add elements to arrays
var arr = KVObject.Array();
arr.Add(3.14f);                    // implicit float -> KVObject
arr.Add("text");

// Remove
data.Root.Remove("deprecated");    // list-backed collections remove every entry with that key
arr.RemoveAt(0);
data.Root.Clear();

// Set flags directly
data["texture"].Flag = KVFlag.Resource;
```

### Enumerating

```csharp
var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
using var stream = File.OpenRead("file.kv3");
KVDocument data = kv.Deserialize(stream);

// KVObject implements IReadOnlyDictionary<string, KVObject>
// Keys are the child names, values are the child KVObjects
foreach (var (key, child) in data.Root)
{
    Console.WriteLine($"{key} = {child}");
}

// Children is the same sequence as a property
foreach (var (key, child) in data.Root.Children) { ... }

// Keys and Values properties
IEnumerable<string> keys = data.Root.Keys;
IEnumerable<KVObject> values = data.Root.Values;

// Array elements have null keys
KVObject array = data["position"];
foreach (var (key, element) in array)
{
    // key is null for array elements
    Console.WriteLine((float)element);
}

// Values on arrays returns elements directly (no KVP wrapper)
foreach (var element in array.Values)
{
    Console.WriteLine((float)element);
}

// Scalars yield nothing
KVObject scalar = data["version"];
foreach (var child in scalar) { } // empty
```

## Serializing and deserializing

`KVSerializer.Create(format)` gives you a serializer for one format. It holds no state, so create it once and reuse it for as many files as you like.

```csharp
var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

using var input = File.OpenRead("file.vdf");
KVDocument document = kv.Deserialize(input);

// Writing a KVDocument keeps its root name and, for KV3, its header
using var output = File.OpenWrite("out.vdf");
kv.Serialize(output, document);
```

`Serialize` also accepts a bare `KVObject` plus a root name (see [Root name](#root-name)), and both methods have generic overloads for your own classes (see [Typed objects](#typed-objects)).

### Options

Every `Deserialize` and `Serialize` overload takes an optional `KVSerializerOptions`:

* `Conditions` - List of conditions used to match KV1 conditionals such as `[$WIN32]`. By default `WIN32` is always present, plus one of `WINDOWS`, `LINUX` + `POSIX`, or `OSX` + `POSIX` depending on the OS the code is running on. See [Conditionals](#conditionals).
* `HasEscapeSequences` - Whether the KV1 parser and serializer should translate escape sequences such as `\n` and `\t`. Valve's parser only does this when asked to, so it is off by default.
* `EnableValveNullByteBugBehavior` - Whether invalid KV1 escape sequences should truncate strings rather than throwing an `InvalidDataException`, matching a bug in Valve's parser.
* `FileLoader` - Provider for files referenced by KV1 `#include` and `#base` directives. See [Includes](#includes).
* `StringTable` - String table used by the KV1 binary format. See [Binary](#binary).
* `SkipHeader` - Whether to omit the KV3 header comment when serializing and to not expect one when deserializing.

```csharp
var options = new KVSerializerOptions
{
    HasEscapeSequences = true,
};
options.Conditions.Clear(); // Remove default conditionals set by the library
options.Conditions.Add("X360WIDE");

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
using var stream = File.OpenRead("file.vdf");
var data = kv.Deserialize(stream, options);
```

### Typed objects

Instead of walking a `KVObject` tree you can map KeyValues to and from your own classes. Properties are matched to keys by name. Use `[KVProperty("name")]` to map a property to a differently named key, and `[KVIgnore]` to leave a property out. Arrays and `List<T>` style collections are filled from KV1 objects with numeric keys and from KV3 arrays. This works the same way for every format.

```csharp
class DataObject
{
    public string Name { get; set; }

    public string Developer { get; set; }

    [KVProperty("description")]
    public string Summary { get; set; }

    [KVIgnore]
    public string ExtraData { get; set; }
}

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

// Deserialize
using var input = File.OpenRead("file.vdf");
DataObject loaded = kv.Deserialize<DataObject>(input);

// Serialize
var data = new DataObject
{
    Developer = "Valve Software",
    Name = "Dota 2",
    Summary = "Dota 2 is a complex game.",
    ExtraData = "This will not be serialized."
};

using var output = File.OpenWrite("file.vdf");
kv.Serialize(output, data, "root object name");
```

## KeyValues1

Used by Steam and the Source engine. Text files look like this:

```
"root object name"
{
    "Developer"    "Valve Software"
    "Name"         "Dota 2"
    "Settings"
    {
        "fullscreen"    "1"
    }
}
```

### Root name

Every KV1 file has a single named root object. When reading, the name ends up in `KVDocument.Name`. When writing, either pass a `KVDocument` or a bare `KVObject` plus a name:

```csharp
var root = KVObject.ListCollection();
root.Add("Developer", "Valve Software");
root.Add("Name", "Dota 2");

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
using var stream = File.OpenWrite("file.vdf");

// Either serialize a bare object with a root name...
kv.Serialize(stream, root, "root object name");

// ...or wrap it in a KVDocument (header is null for KV1)
var doc = new KVDocument(null, "root object name", root);
kv.Serialize(stream, doc);
```

### Conditionals

A KV1 key-value pair can be followed by a bracketed condition, and the pair is only kept when the condition matches. Conditions are `$NAME` variables combined with `!`, `&&`, `||` and parentheses:

```
"operating system"    "windows"          [$WIN32]
"operating system"    "something else"   [!$WIN32]
"ui type"             "Widescreen Xbox"  [$X360 && $X360WIDE]
"platform"            "desktop"          [($WIN32 || $OSX) && !$MOBILE]
```

The set of variables that evaluate to true comes from the `Conditions` option. The defaults match the OS you are running on. To target a different platform, clear the list and add your own.

### Includes

KV1 text files can reference other files. `#include` appends the included file's keys into the current block. `#base` loads a base file whose keys are recursively merged into the current file. The library never touches the file system on its own. Instead you provide an `IIncludedFileLoader` that turns the path written in the directive into a `Stream`. If a file contains a directive and no `FileLoader` is set, deserialization throws `KeyValueException`.

```csharp
class DirectoryFileLoader(string directory) : IIncludedFileLoader
{
    public Stream OpenFile(string filePath)
    {
        // filePath is exactly what the directive says, e.g. "shared/base.vdf"
        return File.OpenRead(Path.Combine(directory, filePath));
    }
}

var options = new KVSerializerOptions
{
    FileLoader = new DirectoryFileLoader("C:/game/cfg"),
};

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
using var stream = File.OpenRead("C:/game/cfg/file.vdf");
var data = kv.Deserialize(stream, options);
```

### Binary

Reading and writing binary KV1 works the same as text, just use `KVSerializationFormat.KeyValues1Binary`. Arrays cannot be written to binary and throw `NotImplementedException`. Blobs are written as hex strings.

Some binary files, such as newer versions of Steam's `appinfo.vdf`, keep their keys in a separate string table. Pass that table through the `StringTable` option and keys are read and written as indexes into it instead of inline strings:

```csharp
// The keys the file was written with. Use new StringTable() when writing to build one up.
IList<string> existingKeys = [ "appid", "common", "name" ];

var options = new KVSerializerOptions
{
    StringTable = new StringTable(existingKeys),
};

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary);
using var stream = File.OpenRead("file.bin");
var data = kv.Deserialize(stream, options);

string[] keys = options.StringTable.ToArray(); // table contents after writing
```

## KeyValues3

Used by the Source 2 engine. Only the text encoding is supported. Binary KV3, including the LZ4, Zstd and block-compressed variants, is not.

A KV3 text file starts with a header comment that identifies the encoding and format, followed by a single root value:

```
<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->
{
    name = "Dota 2"
    appid = 570
    enabled = true
    position = [1.0, 2.0, 3.0]
    model = resource:"models/foo.vmdl"
    data = #[01 02 03]
    nested =
    {
        key = "value"
    }
}
```

Reading works exactly like KV1, but a few things come out differently:

- `data.Name` is always `null`, because KV3 has no root key name.
- `data.Root` can be any value, not just a collection. If the file's root is a string, number, array, blob or `null`, that is what `Root` will be.
- Collections are dictionary-backed, so duplicate keys are not preserved.
- `data.Header` holds the encoding and format identifiers from the header comment.

### Header

`KVDocument.Header` is a `KVHeader` with `Encoding` and `Format` properties. Each one is a `KV3ID` record holding a `Name` and a `Guid`. The well-known identifiers are available as static properties on `ValveKeyValue.KeyValues3.Encoding` and `ValveKeyValue.KeyValues3.Format`.

```csharp
var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
using var stream = File.OpenRead("file.kv3");
KVDocument data = kv.Deserialize(stream);

if (data.Header is { } header)
{
    Console.WriteLine(header.Encoding);        // text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d}
    Console.WriteLine(header.Format.Name);     // generic

    bool isGeneric = header.Format.Id == ValveKeyValue.KeyValues3.Format.Generic;
}
```

### Flags

KV3 values can carry a flag prefix: `resource:`, `resource_name:`, `panorama:`, `soundevent:`, `subclass:` or `entity_name:`. These map to the `KVFlag` enum and are exposed through `KVObject.Flag`. A flag can be attached to a scalar, an array or a collection.

```csharp
var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
using var stream = File.OpenRead("file.kv3");
KVDocument data = kv.Deserialize(stream);

KVFlag flag = data["model"].Flag;                    // KVFlag.Resource

var model = new KVObject("models/foo.vmdl") { Flag = KVFlag.Resource };
data.Root["model"] = model;
```

### Writing

```csharp
var root = KVObject.Collection();
root["name"] = "Dota 2";
root["position"] = KVObject.Array([ 1.0f, 2.0f, 3.0f ]);
root["data"] = KVObject.Blob([ 0x01, 0x02, 0x03 ]);

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
using var output = File.OpenWrite("output.kv3");

// A document without a header gets the default text/generic header (KV3 has no root name, so pass null)
kv.Serialize(output, new KVDocument(null, null, root));

// A document read from a file keeps the header it was read with
using var input = File.OpenRead("input.kv3");
KVDocument data = kv.Deserialize(input);
kv.Serialize(output, data);

// Omit the header entirely
kv.Serialize(output, data, new KVSerializerOptions { SkipHeader = true });
```

## Source maps

If you are writing a syntax highlighter or an editor, the text formats can hand you a per-token source map along with the parsed or serialized text. Each `KvSourceSpan` is a `(Start, End, TokenType)` record. `Start` and `End` are character offsets into the text, with `End` being exclusive. `TokenType` is a `KVTokenType` that says what the token is: a key, a string value, a brace, a comment, a KV1 conditional or directive, the KV3 header, a flag, and so on.

```csharp
var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

// Parse a string and get spans into that same string
string text = File.ReadAllText("file.kv3");
var (document, spans) = kv.DeserializeWithSourceMap(text);

foreach (var span in spans)
{
    var token = text.AsSpan(span.Start, span.End - span.Start);
    Console.WriteLine($"{span.TokenType}: {token}");
}

// Serialize and get spans into the produced text
var (output, outputSpans) = kv.SerializeWithSourceMap(document);

// Also available for bare objects and typed objects
var (output2, spans2) = kv.SerializeWithSourceMap(document.Root, "root object name");
var (output3, spans3) = kv.SerializeWithSourceMap(new DataObject { Name = "Dota 2" }, "root object name");
```

Source maps only exist for text formats. Calling these methods on a `KeyValues1Binary` serializer throws `InvalidOperationException`.

## Errors

Malformed input throws `KeyValueException`. Converting a value to a type it cannot represent, such as casting a collection to `int`, throws `NotSupportedException`. Calling a collection or array method on a scalar throws `InvalidOperationException`.
