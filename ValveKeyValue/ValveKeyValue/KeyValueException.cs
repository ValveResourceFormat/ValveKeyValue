using System;
using System.Runtime.Serialization;

namespace ValveKeyValue
{
    /// <summary>
    /// General KeyValue exception type.
    /// </summary>
    [Serializable]
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

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The System.Runtime.Serialization.SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The System.Runtime.Serialization.StreamingContext that contains contextual information about the source or destination.</param>
        /// <exception cref="ArgumentNullException"> The info parameter is null.</exception>
        /// <exception cref="SerializationException">The class name is null or System.Exception.HResult is zero (0).</exception>
        protected KeyValueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
