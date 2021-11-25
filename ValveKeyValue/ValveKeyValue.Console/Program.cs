using System.Text;
using ValveKeyValue;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length < 1)
{
    Console.Error.WriteLine("Provide path to a keyvalues file as the first argument.");
    return 1;
}

var file = args[0];

if (!File.Exists(file))
{
    Console.Error.WriteLine($"File \"{file}\" does not exist.");
    return 1;
}

using var stream = File.OpenRead(file);

var options = new KVSerializerOptions
{
    HasEscapeSequences = true,
};
var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
var root = serializer.Deserialize(stream, options);

RecursivePrint(root);

return 0;

static void RecursivePrint(KVObject obj, int indent = 0)
{
    Console.Write(new string('\t', indent));

    indent++;

    if (obj.Value is IEnumerable<KVObject> children)
    {
        Console.WriteLine($"Name: {obj.Name}");

        foreach (var value in children)
        {
            RecursivePrint(value, indent);
        }
    }
    else
    {
        Console.WriteLine($"{obj.Name}: {obj.Value}");
    }
}
