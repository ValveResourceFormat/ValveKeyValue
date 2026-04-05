<h1 align="center"><img src="./Misc/logo.png" width="64" height="64" align="center"> Valve Key Value for .NET</h1>

<p align="center">
    <a href="https://github.com/ValveResourceFormat/ValveKeyValue/actions" title="Build Status"><img alt="Build Status" src="https://img.shields.io/github/actions/workflow/status/ValveResourceFormat/ValveKeyValue/ci.yml?logo=github&label=Build&logoColor=ffffff&style=for-the-badge&branch=master"></a>
    <a href="https://www.nuget.org/packages/ValveKeyValue/" title="NuGet"><img alt="NuGet" src="https://img.shields.io/nuget/v/ValveKeyValue.svg?logo=nuget&label=NuGet&logoColor=ffffff&color=004880&style=for-the-badge"></a>
    <a href="https://app.codecov.io/gh/ValveResourceFormat/ValveKeyValue" title="Code Coverage"><img alt="Code Coverage" src="https://img.shields.io/codecov/c/github/ValveResourceFormat/ValveKeyValue/master?logo=codecov&label=Coverage&logoColor=ffffff&color=F01F7A&style=for-the-badge"></a>
</p>

KeyValues is a simple key-value pair format used by Valve in Steam and the Source engine for configuration files, game data, and more (`.vdf`, `.res`, `.acf`, etc.). This library aims to be fully compatible with Valve's various implementations of KeyValues format parsing (believe us, it's not consistent).

# Core Type

The library is built around a single type:

- **`KVObject`** (class) -- a value node. Can be a scalar (string, int, float, bool, etc.), a binary blob, an array, or a named collection of children. Keys (names) are stored in the parent container, not on the child -- similar to how JSON works. Implements `IReadOnlyDictionary<string, KVObject>` and `IConvertible`.
- **`KVDocument`** (class) -- a deserialized document containing a `Root` KVObject, a root key `Name`, and an optional `Header`. Has a read-only string indexer that delegates to `Root`, and an implicit conversion to `KVObject`.

All types are shared across KV1 and KV3 -- you can deserialize from one format and serialize to another. However, not all value types are supported by all formats:

| Feature | KV1 Text | KV1 Binary | KV3 Text |
|---------|----------|------------|----------|
| Collections | Yes (list-backed, allows duplicate keys) | Yes (list-backed) | Yes (dict-backed, O(1) lookup) |
| Arrays | Emulated as objects with numeric keys | No (throws) | Yes (native) |
| Binary blobs | No | No (throws) | Yes (native) |
| Scalars | Yes | Yes | Yes |
| Flags | No | No | Yes |

When constructing objects programmatically, use `KVObject.Collection()` (dict-backed) for general use and KV3 output, or `KVObject.ListCollection()` (list-backed) when you need duplicate keys or KV1 compatibility. Deserialization picks the appropriate backing store automatically.

## KVObject

### Constructing

```csharp
// Scalar values (typed constructors)
var obj = new KVObject("hello");    // string
var obj = new KVObject(42);         // int
var obj = new KVObject(3.14f);      // float
var obj = new KVObject(true);       // bool

// Implicit conversion from primitives
KVObject obj = "hello";
KVObject obj = 42;

// Dictionary-backed collection (O(1) lookup, no duplicate keys)
var obj = KVObject.Collection();                     // empty
var obj = new KVObject();                            // same as above

// List-backed collection (preserves insertion order, allows duplicate keys, for KV1)
var obj = KVObject.ListCollection();                 // empty

// Build up children
var obj = new KVObject();
obj["name"] = "Dota 2";                              // implicit string -> KVObject
obj["appid"] = 570;                                   // implicit int -> KVObject

// Array
var arr = KVObject.Array();                           // empty
var arr = KVObject.Array([ new KVObject("a"), new KVObject("b") ]); // from elements

// Binary blob
var blob = KVObject.Blob(new byte[] { 0x01, 0x02, 0x03 });

// Null value
var nul = KVObject.Null();
```

### Reading values

```csharp
KVDocument data = kv.Deserialize(stream);

// Root key name (only on KVDocument)
string? rootName = data.Name;

// String indexer returns KVObject (supports chaining)
string name = (string)data["config"]["name"];
int version = (int)data["version"];
float scale = (float)data["scale"];
bool enabled = (bool)data["settings"]["enabled"];

// Array elements by index
float x = (float)data["position"][0];

// Access the root KVObject for full API (mutations, ContainsKey, etc.)
KVObject root = data.Root;

// Check existence (on the root KVObject)
if (data.Root.ContainsKey("optional")) { ... }
if (data.Root.TryGetValue("optional", out var child)) { ... }

// Indexer throws KeyNotFoundException for missing keys
// Use TryGetValue for safe access

// Direct access to value properties (on KVObject)
KVValueType type = data.Root.ValueType;
KVFlag flag = data["texture"].Flag;
byte[] bytes = data["blob"].AsBlob();
```

### Modifying

```csharp
// Mutations require the Root KVObject (KVDocument indexer is read-only)
data.Root["name"] = "new name";
data.Root["count"] = 42;

// Chained writes work (reference semantics, first lookup goes through KVDocument indexer)
data["config"]["resolution"] = "1920x1080";

// Add children to collections
data.Root.Add("newprop", 42);      // implicit int -> KVObject
data.Root.Add("text", "value");    // implicit string -> KVObject

// Add elements to arrays
arr.Add(3.14f);                    // implicit float -> KVObject

// Remove
data.Root.Remove("deprecated");
arr.RemoveAt(2);
data.Root.Clear();

// Set flags directly
data["texture"].Flag = KVFlag.Resource;
```

### Enumerating

```csharp
// KVObject implements IReadOnlyDictionary<string, KVObject>
// Keys are the child names, values are the child KVObjects
foreach (var (key, child) in data.Root)
{
    Console.WriteLine($"{key} = {(string)child}");
}

// Keys and Values properties
var keys = data.Root.Keys;       // IEnumerable<string>
var values = data.Root.Values;   // IEnumerable<KVObject>

// Array elements have null keys
foreach (var (key, element) in arrayObj)
{
    // key is null for array elements
    Console.WriteLine((string)element);
}

// Values on arrays returns elements directly (no KVP wrapper)
foreach (var element in arrayObj.Values)
{
    Console.WriteLine((string)element);
}

// Scalars yield nothing
foreach (var child in scalarObj) { } // empty
```

# KeyValues1

Used by Steam and the Source engine.

## Deserializing text

### Basic deserialization
```csharp
var stream = File.OpenRead("file.vdf"); // or any other Stream

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
KVDocument data = kv.Deserialize(stream);

Console.WriteLine(data["some key"]);
```

### Typed deserialization
```csharp
public class SimpleObject
{
    public string Name { get; set; }
    public string Value { get; set; }
}

var stream = File.OpenRead("file.vdf"); // or any other Stream

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
SimpleObject data = kv.Deserialize<SimpleObject>(stream);
```

### Options
The `Deserialize` method also accepts a `KVSerializerOptions` object.

By default, operating system specific conditionals are enabled based on the OS the code is running on (`RuntimeInformation`).

`KVSerializerOptions` has the following options:

* `Conditions` - List of conditions to use to match conditional values.
* `HasEscapeSequences` - Whether the parser should translate escape sequences (e.g. `\n`, `\t`).
* `EnableValveNullByteBugBehavior` - Whether invalid escape sequences should truncate strings rather than throwing an `InvalidDataException`.
* `FileLoader` - Provider for referenced files with `#include` or `#base` directives.
* `SkipHeader` - Whether to skip writing the KV3 header comment during serialization.

```csharp
var options = new KVSerializerOptions
{
    HasEscapeSequences = true,
};
options.Conditions.Clear(); // Remove default conditionals set by the library
options.Conditions.Add("X360WIDE");

var stream = File.OpenRead("file.vdf");

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
var data = kv.Deserialize(stream, options);
```

## Deserializing binary

Essentially the same as text, just change `KeyValues1Text` to `KeyValues1Binary`.

## Serializing to text

### Dynamic serialization
```csharp
var root = KVObject.ListCollection();
root.Add("Developer", "Valve Software");
root.Add("Name", "Dota 2");
var doc = new KVDocument(null, "root object name", root);

using var stream = File.OpenWrite("file.vdf");

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
kv.Serialize(stream, doc);
```

### Typed serialization
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

var data = new DataObject
{
    Developer = "Valve Software",
    Name = "Dota 2",
    Summary = "Dota 2 is a complex game.",
    ExtraData = "This will not be serialized."
};

using var stream = File.OpenWrite("file.vdf");

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
kv.Serialize(stream, data, "root object name");
```

## Serializing to binary

Essentially the same as text, just change `KeyValues1Text` to `KeyValues1Binary`.

# KeyValues2 (Datamodel)

This library does not currently support KeyValues2 (Datamodel). If you need KV2/Datamodel support, use our fork of [Datamodel.NET](https://github.com/ValveResourceFormat/Datamodel.NET) instead.

# KeyValues3

Used by the Source 2 engine.

## Deserializing text

```csharp
var stream = File.OpenRead("file.kv3"); // or any other Stream

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
KVDocument data = kv.Deserialize(stream);

Console.WriteLine(data["some key"]);
```

## Serializing to text

```csharp
using var stream = File.OpenWrite("file.kv3");

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
kv.Serialize(stream, data);
```
