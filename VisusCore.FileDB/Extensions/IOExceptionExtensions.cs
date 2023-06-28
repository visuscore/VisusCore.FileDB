using System.IO;
using System.Runtime.InteropServices;

namespace VisusCore.FileDB.Extensions;

internal static class IOExceptionExtensions
{
    public static bool IsLockException(this IOException exception)
    {
        int errorCode = Marshal.GetHRForException(exception) & ((1 << 16) - 1);

        return errorCode is 32 or 33;
    }
}
