using System;
using System.IO;
using System.Linq;

namespace VisusCore.FileDB.Samples;

public static class Program
{
    private const string DbFile = "test.dat";

    public static void Main(string[] args)
    {
        var files = new[]
        {
            GetRandomFileEntryArray("file1.dat", 1024),
            GetRandomFileEntryArray("file2.dat", 1_048_576),
            GetRandomFileEntryArray("file3.dat", 10_485_760),
            GetRandomFileEntryArray("file4.dat", 104_857_600),
        };

        foreach (var file in files)
        {
            using var dataStream = new MemoryStream(file.Data);
            FileDB.Store(DbFile, file.Name, dataStream);
        }

        using var db = new FileDB(DbFile, FileAccess.Read);
        var entries = db.ListFiles();

        foreach (var entry in entries)
        {
            var file = files.First(item => item.Name == entry.FileName);
            using var dataStream = new MemoryStream();

            db.Read(entry.ID, dataStream);
            dataStream.Seek(0, SeekOrigin.Begin);

            Console.WriteLine(
                $"Entry: File name: {entry.FileName}, size: {entry.FileLength}, " +
                $"content match: {file.Data.SequenceEqual(dataStream.ToArray())}");
        }
    }

    private static (string Name, byte[] Data) GetRandomFileEntryArray(string fileName, int size)
    {
        var rnd = new Random();
        var bytes = new byte[size]; // convert kb to byte

        // This is only for demo purposes, do not use this in production.
#pragma warning disable CA5394 // Do not use insecure randomness
#pragma warning disable SCS0005 // Weak random number generator.
        rnd.NextBytes(bytes);
#pragma warning restore SCS0005 // Weak random number generator.
#pragma warning restore CA5394 // Do not use insecure randomness

        return (Name: fileName, Data: bytes);
    }
}
