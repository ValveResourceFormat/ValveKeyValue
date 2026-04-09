using System.Text;

namespace ValveKeyValue.Serialization
{
    abstract class KVTextSerializerBase : IDisposable
    {
        protected KVTextSerializerBase(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            writer = new StreamWriter(stream, new UTF8Encoding(), bufferSize: 1024, leaveOpen: true)
            {
                NewLine = "\n"
            };
        }

        // Source-map mode writes into the supplied StringBuilder, using sb.Length as the
        // current output offset and appending a span entry to sourceMap on each emitted token.
        protected KVTextSerializerBase(StringBuilder sb, List<KvSourceSpan> sourceMap)
        {
            ArgumentNullException.ThrowIfNull(sb);
            ArgumentNullException.ThrowIfNull(sourceMap);

            this.sb = sb;
            this.sourceMap = sourceMap;

            // StringWriter defaults to Environment.NewLine (\r\n on Windows), which would shift
            // every span offset by one per line. Force \n to match the StreamWriter path.
            writer = new StringWriter(sb) { NewLine = "\n" };
        }

        protected readonly TextWriter writer;
        readonly StringBuilder? sb;
        readonly List<KvSourceSpan>? sourceMap;
        protected int indentation;

        // Returns 0 in stream mode (sb is null), where the matching Record call is a no-op.
        protected int Position => sb?.Length ?? 0;

        protected void Record(int start, KVTokenType tokenType)
        {
            if (sourceMap != null && sb!.Length > start)
            {
                sourceMap.Add(new KvSourceSpan(start, sb.Length, tokenType));
            }
        }

        // Convenience for single-char tokens like braces, brackets, '=', and ','.
        protected void Record(KVTokenType tokenType, char c)
        {
            var s = Position;
            writer.Write(c);
            Record(s, tokenType);
        }

        protected void WriteIndentation()
        {
            for (var i = 0; i < indentation; i++)
            {
                writer.Write('\t');
            }
        }

        public void Dispose()
        {
            writer.Dispose();
        }
    }
}
