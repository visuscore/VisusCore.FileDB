using System;
using System.IO;
using System.Text;

namespace VisusCore.FileDB.Extensions;

internal static class BinaryReaderExtensions
{
    public static string ReadString(this BinaryReader reader, int size)
    {
        var bytes = reader.ReadBytes(size);
        string str = Encoding.UTF8.GetString(bytes);
        return str.Replace((char)0, ' ').Trim();
    }

    public static Guid ReadGuid(this BinaryReader reader)
    {
        var bytes = reader.ReadBytes(16);
        return new Guid(bytes);
    }

    public static DateTime ReadDateTime(this BinaryReader reader)
    {
        var ticks = reader.ReadInt64();
        return new DateTime(ticks);
    }

    public static long Seek(this BinaryReader reader, long position) =>
        reader.BaseStream.Seek(position, SeekOrigin.Begin);
}
