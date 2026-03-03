<h1 align="center"><img src="./Misc/logo.png" width="64" height="64" align="center"> Valve Key Value for .NET</h1>

<p align="center">
    <a href="https://github.com/ValveResourceFormat/ValveKeyValue/actions" title="Build Status"><img alt="Build Status" src="https://img.shields.io/github/actions/workflow/status/ValveResourceFormat/ValveKeyValue/ci.yml?logo=github&label=Build&logoColor=ffffff&style=for-the-badge&branch=master"></a>
    <a href="https://www.nuget.org/packages/ValveKeyValue/" title="NuGet"><img alt="NuGet" src="https://img.shields.io/nuget/v/ValveKeyValue.svg?logo=nuget&label=NuGet&logoColor=ffffff&color=004880&style=for-the-badge"></a>
    <a href="https://app.codecov.io/gh/ValveResourceFormat/ValveKeyValue" title="Code Coverage"><img alt="Code Coverage" src="https://img.shields.io/codecov/c/github/ValveResourceFormat/ValveKeyValue/master?logo=codecov&label=Coverage&logoColor=ffffff&color=F01F7A&style=for-the-badge"></a>
</p>

KeyValues is a simple key-value pair format used by Valve in Steam and the Source engine for configuration files, game data, and more (`.vdf`, `.res`, `.acf`, etc.). This library aims to be fully compatible with Valve's various implementations of KeyValues format parsing (believe us, it's not consistent).

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

# KeyValues3

This library does not currently support KeyValues3. There is an [open pull request](https://github.com/ValveResourceFormat/ValveKeyValue/pull/61) for KV3 support.

If you need KV3 support, use [ValveResourceFormat](https://github.com/ValveResourceFormat/ValveResourceFormat) which supports parsing Source 2 formats including KV3.
