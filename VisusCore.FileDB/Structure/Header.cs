namespace VisusCore.FileDB.Structure;

internal sealed class Header
{
    public const long LockerPos = 98;
    public const long HeaderSize = 100;

    public const string FileID = "FileDB";        // 6 bytes
    public const short FileVersion = 1;           // 2 bytes

    /// <summary>
    /// Gets or sets the fist index page (root page). It's fixed on 0 (zero).
    /// </summary>
    public uint IndexRootPageID { get; set; } // 4 bytes

    /// <summary>
    /// Gets or sets the last has free nodes to be used.
    /// </summary>
    public uint FreeIndexPageID { get; set; } // 4 bytes

    /// <summary>
    /// Gets or sets if a deleted data, this variable point to first page emtpy. I will use to insert the next data page.
    /// </summary>
    public uint FreeDataPageID { get; set; } // 4 bytes

    /// <summary>
    /// Gets or sets the last deleted page. It's used to make continuos statments of empty page data.
    /// </summary>
    public uint LastFreeDataPageID { get; set; } // 4 bytes

    /// <summary>
    /// Gets or sets the last used page on FileDB disk (even index or data page). It's used to grow the file db (create
    /// new pages).
    /// </summary>
    public uint LastPageID { get; set; } // 4 bytes

    public Header()
    {
        IndexRootPageID = uint.MaxValue;
        FreeIndexPageID = uint.MaxValue;
        FreeDataPageID = uint.MaxValue;
        LastFreeDataPageID = uint.MaxValue;
        LastPageID = uint.MaxValue;
        IsDirty = false;
    }

    public bool IsDirty { get; set; }
}
