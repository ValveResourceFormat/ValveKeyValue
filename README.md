<h1><img src="./Misc/logo.png" width="64" align="center"> Valve's KeyValue for .NET</h1>

[![Build Status (GitHub)](https://img.shields.io/github/actions/workflow/status/ValveResourceFormat/ValveKeyValue/ci.yml?label=Build&style=flat-square&branch=master)](https://github.com/ValveResourceFormat/ValveKeyValue/actions)
[![NuGet](https://img.shields.io/nuget/v/ValveKeyValue.svg?label=NuGet&style=flat-square)](https://www.nuget.org/packages/ValveKeyValue/)
[![Coverage Status](https://img.shields.io/codecov/c/github/ValveResourceFormat/ValveKeyValue/master?label=Coverage&style=flat-square)](https://app.codecov.io/gh/ValveResourceFormat/ValveKeyValue)

This library aims to be fully compatible with Valve's various implementations of
KeyValues format parsing (believe us, it's not consistent).

# KeyValues1

Used by Steam and the Source engine.

## Deserializing text

### Basic deserialization
```cs
var stream = File.OpenRead("file.vdf"); // or any other Stream

var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
KVObject data = kv.Deserialize(stream);

Console.WriteLine(data["some key"]);
```

### Typed deserialization
```cs
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
`Deserialize` method also accepts an `KVSerializerOptions` object.

By default, operating system specific conditionals are enabled based on the OS the code is running on (`RuntimeInformation`).

`KVSerializerOptions` accepts has the following options:

* `Conditions` - List of conditions to use to match conditional values.
* `HasEscapeSequences` - Whether the parser should translate escape sequences (e.g. `\n`, `\t`).
* `EnableValveNullByteBugBehavior` - Whether invalid escape sequences should truncate strings rather than throwing a `InvalidDataException`.
* `FileLoader` - Provider for referenced files with `#include` or `#base` directives.

```cs
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

```cs
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
