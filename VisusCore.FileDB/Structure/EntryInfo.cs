using System;
using System.IO;
using VisusCore.FileDB.Helpers;

namespace VisusCore.FileDB.Structure;

public class EntryInfo
{
    public Guid ID { get; }
    public string FileName { get; }
    public uint FileLength { get; internal set; }
    public string MimeType { get; }

    internal EntryInfo(string fileName)
    {
        ID = Guid.NewGuid();
        FileName = Path.GetFileName(fileName);
        MimeType = MimeTypeConverter.Convert(Path.GetExtension(FileName));
        FileLength = 0;
    }

    internal EntryInfo(IndexNode node)
    {
        ID = node.ID;
        FileName = node.FileName + "." + node.FileExtension;
        MimeType = MimeTypeConverter.Convert(node.FileExtension);
        FileLength = node.FileLength;
    }
}
