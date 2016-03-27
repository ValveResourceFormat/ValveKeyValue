using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValveKeyValue
{
    class KVTextWriter : IDisposable
    {
        public KVTextWriter(Stream stream)
        {
            writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
        }

        readonly TextWriter writer;
        int indentation = 0;

        public void WriteObject(KVObject data)
        {
            if (data.Value != null)
            {
                WriteKeyValuePair(data.Name, data.Value);
            }
            else
            {
                WriteStartObject(data.Name);

                foreach (var item in data.Items)
                {
                    WriteObject(item);
                }

                WriteEndObject();
            }
        }

        public void Dispose()
        {
            writer.Dispose();
        }

        void WriteStartObject(string name)
        {
            WriteIndentation();
            WriteText(name);
            WriteLine();
            WriteIndentation();
            writer.Write('{');
            indentation++;
            WriteLine();
        }

        void WriteEndObject()
        {
            indentation--;
            WriteIndentation();
            writer.Write('}');
            writer.WriteLine();
        }

        void WriteIndentation()
        {
            if (indentation == 0)
            {
                return;
            }

            var text = new string('\t', indentation);
            writer.Write(text);
        }

        void WriteKeyValuePair(string name, KVValue value)
        {
            WriteIndentation();
            WriteText(name);
            writer.Write('\t');
            WriteText((string)value);
            WriteLine();
        }

        void WriteText(string text)
        {
            writer.Write('"');

            foreach (var @char in text)
            {
                switch (@char)
                {
                    case '\r':
                        writer.Write(@"\r");
                        break;

                    case '\n':
                        writer.Write(@"\n");
                        break;

                    case '"':
                        writer.Write("\\\"");
                        break;

                    default:
                        writer.Write(@char);
                        break;
                }
            }

            writer.Write('"');
        }

        void WriteLine()
        {
            writer.WriteLine();
        }
    }
}
