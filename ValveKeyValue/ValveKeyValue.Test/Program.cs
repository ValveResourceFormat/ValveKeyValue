using NUnit.Common;
using NUnitLite;
using System;
using System.Reflection;

namespace ValveKeyValue.Test
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var runner = new AutoRun(typeof(Program).GetTypeInfo().Assembly);
            var result = runner.Execute(args, new ExtendedTextWrapper(Console.Out), Console.In);
            return result;
        }
    }
}
