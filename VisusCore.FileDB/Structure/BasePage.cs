namespace VisusCore.FileDB.Structure;

internal enum PageType
{
    /// <summary>
    /// Data = 1.
    /// </summary>
    Data = 1,

    /// <summary>
    /// Index = 2.
    /// </summary>
    Index = 2,
}

internal abstract class BasePage
{
    public const long PageSize = 4096;

    public uint PageID { get; set; }
    public abstract PageType Type { get; }
    public uint NextPageID { get; set; }
}
