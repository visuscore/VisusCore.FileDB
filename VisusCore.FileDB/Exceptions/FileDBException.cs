using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace VisusCore.FileDB.Exceptions;

[Serializable]
public class FileDBException : Exception
{
    public FileDBException()
    {
    }

    public FileDBException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public FileDBException(string message)
        : base(message)
    {
    }

    public FileDBException(string message, params object[] args)
        : this(string.Format(CultureInfo.InvariantCulture, message, args))
    {
    }

    protected FileDBException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
