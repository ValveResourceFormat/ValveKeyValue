using System;

namespace ValveKeyValue
{
    static class Require
    {
        public static void NotNull<T>(T value, string paramName)
            where T : class // So that this is not accidentally used on a struct/value-type and automatically boxed.
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void NotDisposed(string objectName, bool disposed)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(objectName);
            }
        }
    }
}
