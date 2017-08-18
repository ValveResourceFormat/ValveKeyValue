using System;

namespace ValveKeyValue
{
    /// <summary>
    /// General KeyValue exception type.
    /// </summary>
    public class KeyValueException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueException"/> class.
        /// </summary>
        public KeyValueException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public KeyValueException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public KeyValueException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
