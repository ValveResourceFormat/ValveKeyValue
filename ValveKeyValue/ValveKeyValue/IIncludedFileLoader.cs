using System.IO;

namespace ValveKeyValue
{
    /// <summary>
    /// Provides a way to open any file referenced with <c>#include</c> or <c>#base</c>.
    /// </summary>
    public interface IIncludedFileLoader
    {
        /// <summary>
        /// Opens the file referenced by a given <c>#include</c> or <c>#base</c> statement.
        /// </summary>
        /// <param name="filePath">The path as declared by the <c>#include</c> or <c>#base</c> statement.</param>
        /// <returns>A stream to the included KeyValues file.</returns>
        Stream OpenFile(string filePath);
    }
}
