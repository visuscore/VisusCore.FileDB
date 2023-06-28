using System;
using System.Text;

namespace VisusCore.FileDB.Extensions;

internal static class StringExtensions
{
    public static byte[] ToBytes(this string str, int size)
    {
        var buffer = new byte[size];
        if (string.IsNullOrEmpty(str))
            return buffer;

        var strbytes = Encoding.UTF8.GetBytes(str);

        Array.Copy(strbytes, buffer, size > strbytes.Length ? strbytes.Length : size);

        return buffer;
    }
}
