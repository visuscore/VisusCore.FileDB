namespace VisusCore.FileDB.Structure;

internal sealed class DataPage : BasePage
{
    public const long HeaderSize = 8;
    public const long DataPerPage = 4088;

    // 1 byte
    public override PageType Type => PageType.Data;
    // 1 byte
    public bool IsEmpty { get; set; }
    // 2 bytes
    public short DataBlockLength { get; set; }

    public byte[] DataBlock { get; set; }

    public DataPage(uint pageID)
    {
        PageID = pageID;
        IsEmpty = true;
        DataBlockLength = 0;
        NextPageID = uint.MaxValue;
        DataBlock = new byte[DataPerPage];
    }
}
