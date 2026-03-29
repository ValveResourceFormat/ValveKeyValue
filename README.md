<h1 align="center"><img src="./Misc/logo.png" width="64" height="64" align="center"> Valve Key Value for .NET</h1>

<p align="center">
    <a href="https://github.com/ValveResourceFormat/ValveKeyValue/actions" title="Build Status"><img alt="Build Status" src="https://img.shields.io/github/actions/workflow/status/ValveResourceFormat/ValveKeyValue/ci.yml?logo=github&label=Build&logoColor=ffffff&style=for-the-badge&branch=master"></a>
    <a href="https://www.nuget.org/packages/ValveKeyValue/" title="NuGet"><img alt="NuGet" src="https://img.shields.io/nuget/v/ValveKeyValue.svg?logo=nuget&label=NuGet&logoColor=ffffff&color=004880&style=for-the-badge"></a>
    <a href="https://app.codecov.io/gh/ValveResourceFormat/ValveKeyValue" title="Code Coverage"><img alt="Code Coverage" src="https://img.shields.io/codecov/c/github/ValveResourceFormat/ValveKeyValue/master?logo=codecov&label=Coverage&logoColor=ffffff&color=F01F7A&style=for-the-badge"></a>
</p>

KeyValues is a simple key-value pair format used by Valve in Steam and the Source engine for configuration files, game data, and more (`.vdf`, `.res`, `.acf`, etc.). This library aims to be fully compatible with Valve's various implementations of KeyValues format parsing (believe us, it's not consistent).

# Core Types

The library is built around two types:

- **`KVObject`** (class) -- a named tree node. Holds a `Name` and a `KVValue`. Supports navigation, mutation, and enumeration.
- **`KVValue`** (readonly record struct) -- the data. Stores scalars inline (no boxing), strings, binary blobs, arrays, and collections. Supports implicit/explicit conversions, flags, and `with` expressions.

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
// Scalar value (typed constructors for common types, no cast needed)
var obj = new KVObject("key", "hello");
var obj = new KVObject("key", 42);
var obj = new KVObject("key", 3.14f);
var obj = new KVObject("key", true);

// Dictionary-backed collection (O(1) lookup, no duplicate keys)
var obj = KVObject.Collection("root");                // empty, can Add children
var obj = KVObject.Collection("root", [               // with children
    new KVObject("name", "Dota 2"),
    new KVObject("appid", 570),
]);

// List-backed collection (preserves insertion order, allows duplicate keys, for KV1)
var obj = KVObject.ListCollection("root");            // empty
var obj = KVObject.ListCollection("root", [           // with children
    new KVObject("key", "first"),
    new KVObject("key", "second"),                     // duplicate keys allowed
]);

// Array from values (implicit conversions from primitives)
var arr = KVObject.Array("items");                                     // empty, can Add elements
var arr = KVObject.Array("tags", new KVValue[] { "action", "moba" });  // from KVValue[]

// Array from KVObjects (when elements need flags, nested structure, etc.)
var arr = KVObject.Array("data", [
    new KVObject(null, (KVValue)"element"),
    new KVObject(null, flaggedValue),
]);

// Binary blob
var blob = KVObject.Blob("data", new byte[] { 0x01, 0x02, 0x03 });
```

> `new KVObject("name")` is equivalent to `KVObject.Collection("name")` (empty dict-backed collection).

### Reading values

```csharp
KVObject data = kv.Deserialize(stream);

// String indexer returns KVObject (supports chaining)
string name = (string)data["config"]["name"];
int version = (int)data["version"];
float scale = (float)data["scale"];
bool enabled = (bool)data["settings"]["enabled"];

// Array elements by index
float x = (float)data["position"][0];

// Check existence
if (data.ContainsKey("optional")) { ... }
if (data.TryGetChild("optional", out var child)) { ... }

// Null-safe (indexer returns null for missing keys)
KVObject val = data["missing"]; // null

// Access the underlying KVValue directly
KVValueType type = data.ValueType;           // forwarded from Value
KVFlag flag = data["texture"].Value.Flag;    // flags live on KVValue
ReadOnlySpan<byte> bytes = data["blob"].Value.AsSpan();
```

### Modifying

```csharp
// Set scalar (implicit conversion)
data["name"] = "new name";
data["count"] = 42;

// Chained writes work (reference semantics)
data["config"]["resolution"] = "1920x1080";

// Add/remove children
data.Add(new KVObject("newprop", 42));
data.Add("shorthand", (KVValue)"value");
data.Remove("deprecated");

// Array mutation
arr.Add((KVValue)"new element");
arr.RemoveAt(2);

// Clear
data.Clear();

// Modify flags via with expression
var child = data.GetChild("texture");
child.Value = child.Value with { Flag = KVFlag.Resource };
```

### Enumerating

```csharp
// KVObject implements IEnumerable<KVObject>
foreach (var child in data)
{
    Console.WriteLine($"{child.Name} = {(string)child}");
}

// LINQ works naturally
var names = data.Children.Select(c => c.Name);

// Scalars yield nothing
foreach (var child in scalarObj) { } // empty
```

## KVValue

A `readonly record struct` that stores scalar data inline (no boxing):

```csharp
// Implicit from primitives
KVValue v = "hello";
KVValue v = 42;
KVValue v = 3.14f;
KVValue v = true;

// Explicit to primitives
string s = (string)value;
int n = (int)value;

// Typed accessors
string s = value.AsString();
int n = value.ToInt32(CultureInfo.InvariantCulture);
ReadOnlySpan<byte> data = value.AsSpan();
byte[] blob = value.AsBlob();

// Properties
value.ValueType  // KVValueType enum
value.Flag       // KVFlag enum
value.IsNull     // true if ValueType == Null

// with expressions (readonly record struct)
var flagged = value with { Flag = KVFlag.Resource };

// default is null
default(KVValue).IsNull == true
```

# KeyValues1

Used by Steam and the Source engine.

## Deserializing text

### Basic deserialization
```csharp
var stream = File.OpenRead("file.vdf"); // or any other Stream

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
KVObject data = kv.Deserialize(stream);

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
KVObject data = kv.Deserialize<SimpleObject>(stream);
```

### Options
The `Deserialize` method also accepts a `KVSerializerOptions` object.

By default, operating system specific conditionals are enabled based on the OS the code is running on (`RuntimeInformation`).

`KVSerializerOptions` has the following options:

* `Conditions` - List of conditions to use to match conditional values.
* `HasEscapeSequences` - Whether the parser should translate escape sequences (e.g. `\n`, `\t`).
* `EnableValveNullByteBugBehavior` - Whether invalid escape sequences should truncate strings rather than throwing an `InvalidDataException`.
* `FileLoader` - Provider for referenced files with `#include` or `#base` directives.

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
KVObject data = kv.Deserialize(stream);

Console.WriteLine(data["some key"]);
```

## Serializing to text

```csharp
using var stream = File.OpenWrite("file.kv3");

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
kv.Serialize(stream, data);
```
