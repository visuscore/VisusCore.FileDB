using System;
using System.IO;

namespace VisusCore.FileDB.Structure;

internal sealed class IndexNode
{
    public const int FileNameSize = 41;       // Size of file name string
    public const int FileExtensionSize = 5;  // Size of file extension string
    public const int IndexNodeSize = 81;     // Node Index size

    public Guid ID { get; set; } // 16 bytes

    public bool IsDeleted { get; set; } // 1 byte

    public IndexLink Right { get; set; } // 5 bytes
    public IndexLink Left { get; set; } //  5 bytes

    public uint DataPageID { get; set; } // 4 bytes

    // Info
    public string FileName { get; set; } // 41 bytes (file name + extension)
    public string FileExtension { get; set; } //  5 bytes (only extension without dot ".")
    public uint FileLength { get; set; } //  4 bytes

    public IndexPage IndexPage { get; set; }

    public IndexNode(IndexPage indexPage)
    {
        ID = Guid.Empty;
        IsDeleted = true; // Start with index node mark as deleted. Update this after save all stream on disk
        Right = new IndexLink();
        Left = new IndexLink();
        DataPageID = uint.MaxValue;
        IndexPage = indexPage;
    }

    public void UpdateFromEntry(EntryInfo entity)
    {
        ID = entity.ID;
        FileName = Path.GetFileNameWithoutExtension(entity.FileName);
        FileExtension = Path.GetExtension(entity.FileName).Replace(".", string.Empty);
        FileLength = entity.FileLength;
    }
}
