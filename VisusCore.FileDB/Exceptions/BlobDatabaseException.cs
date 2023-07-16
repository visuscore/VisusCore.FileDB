using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace VisusCore.FileDB.Exceptions;

[Serializable]
public class BlobDatabaseException : Exception
{
    public BlobDatabaseException()
    {
    }

    public BlobDatabaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public BlobDatabaseException(string message)
        : base(message)
    {
    }

    public BlobDatabaseException(string message, params object[] args)
        : this(string.Format(CultureInfo.InvariantCulture, message, args))
    {
    }

    protected BlobDatabaseException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
