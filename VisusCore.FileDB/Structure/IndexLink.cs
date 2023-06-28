namespace VisusCore.FileDB.Structure;

internal sealed class IndexLink
{
    public byte Index { get; set; }
    public uint PageID { get; set; }

    public IndexLink()
    {
        Index = 0;
        PageID = uint.MaxValue;
    }

    public bool IsEmpty => PageID == uint.MaxValue;
}
