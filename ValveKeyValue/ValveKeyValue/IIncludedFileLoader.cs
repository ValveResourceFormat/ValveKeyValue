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
        /// <returns>If the file was found, a stream to the included KeyValues file. If the file wasn't found, a FileNotFoundException or DirectoryNotFoundException should be thrown. To silently swallow the error, return null.</returns>
        #nullable enable
        Stream? OpenFile(string filePath);
        #nullable disable
    }
}
