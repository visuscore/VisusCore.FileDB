namespace VisusCore.FileDB.Structure;

internal sealed class IndexPage : BasePage
{
    public const long HeaderSize = 46;
    public const int NodesPerPage = 50;

    public override PageType Type => PageType.Index;   // 1 byte
    public byte NodeIndex { get; set; } //  1 byte

    public IndexNode[] Nodes { get; set; }

    public bool IsDirty { get; set; }

    public IndexPage(uint pageID)
    {
        PageID = pageID;
        NextPageID = uint.MaxValue;
        NodeIndex = 0;
        Nodes = new IndexNode[NodesPerPage];
        IsDirty = false;

        for (int i = 0; i < NodesPerPage; i++)
        {
            Nodes[i] = new IndexNode(this);
        }
    }
}
