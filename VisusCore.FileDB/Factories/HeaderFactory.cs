using System.IO;
using VisusCore.FileDB.Exceptions;
using VisusCore.FileDB.Extensions;
using VisusCore.FileDB.Structure;

namespace VisusCore.FileDB.Factories;

internal static class HeaderFactory
{
    public static void ReadFromFile(Header header, BinaryReader reader)
    {
        // Seek the stream on 0 position to read header
        reader.BaseStream.Seek(0, SeekOrigin.Begin);

        // Make same validation on header file
        if (reader.ReadString(Header.FileID.Length) != Header.FileID)
            throw new BlobDatabaseException("The file is not a valid storage archive");

        if (reader.ReadInt16() != Header.FileVersion)
            throw new BlobDatabaseException("The archive version is not valid");

        header.IndexRootPageID = reader.ReadUInt32();
        header.FreeIndexPageID = reader.ReadUInt32();
        header.FreeDataPageID = reader.ReadUInt32();
        header.LastFreeDataPageID = reader.ReadUInt32();
        header.LastPageID = reader.ReadUInt32();
        header.IsDirty = false;
    }

    public static void WriteToFile(Header header, BinaryWriter writer)
    {
        // Seek the stream on 0 position to save header
        writer.BaseStream.Seek(0, SeekOrigin.Begin);

        writer.Write(Header.FileID.ToBytes(Header.FileID.Length));
        writer.Write(Header.FileVersion);

        writer.Write(header.IndexRootPageID);
        writer.Write(header.FreeIndexPageID);
        writer.Write(header.FreeDataPageID);
        writer.Write(header.LastFreeDataPageID);
        writer.Write(header.LastPageID);
    }
}
