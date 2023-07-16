using System;
using System.IO;
using System.Threading;
using VisusCore.FileDB.Exceptions;

namespace VisusCore.FileDB.Extensions;

internal static class BinaryWriterExtensions
{
    private const int DelayTryLockFile = 50; // in miliseconds

    public static void Write(this BinaryWriter writer, Guid guid) =>
        writer.Write(guid.ToByteArray());

    public static void Write(this BinaryWriter writer, DateTime dateTime) =>
        writer.Write(dateTime.Ticks);

    public static long Seek(this BinaryWriter writer, long position) =>
        writer.BaseStream.Seek(position, SeekOrigin.Begin);

    public static void Lock(this BinaryWriter writer, long position, long length)
    {
        var fileStream = writer.BaseStream as FileStream;

        TryLockFile(fileStream, position, length, 0);
    }

    private static void TryLockFile(FileStream fileStream, long position, long length, int tryCount)
    {
        try
        {
            fileStream.Lock(position, length);
        }
        catch (IOException ex)
        {
            if (ex.IsLockException())
            {
                if (tryCount >= DelayTryLockFile)
                    throw new BlobDatabaseException("Database file is in lock for a long time");

                Thread.Sleep(tryCount * DelayTryLockFile);

                TryLockFile(fileStream, position, length, ++tryCount);
            }

            throw;
        }
    }

    public static void Unlock(this BinaryWriter writer, long position, long length)
    {
        var fileStream = writer.BaseStream as FileStream;

        fileStream.Unlock(position, length);
    }
}
