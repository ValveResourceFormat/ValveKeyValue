using System.Text;
using ValveKeyValue;
using ConsoleAppFramework;

Console.OutputEncoding = Encoding.UTF8;

ConsoleApp.Run(args, Execute);

/// <summary>
/// Parse a KeyValues file and print to console.
/// </summary>
/// <param name="file">-f, Input file to be parsed.</param>
/// <param name="escape">Whether the parser should translate escape sequences.</param>
/// <param name="valve_null_bug">Whether invalid escape sequences should truncate strings rather than throwing.</param>
static int Execute(
    string file,
    bool escape = false,
    bool valve_null_bug = false
)
{
    if (!File.Exists(file))
    {
        Console.Error.WriteLine($"File \"{file}\" does not exist.");
        return 1;
    }

    using var stream = File.OpenRead(file);

    var options = new KVSerializerOptions
    {
        HasEscapeSequences = escape,
        EnableValveNullByteBugBehavior = valve_null_bug 
    };
    var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
    var root = serializer.Deserialize(stream, options);

    RecursivePrint(root);

    return 0;
}

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
