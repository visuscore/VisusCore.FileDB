using System;
using System.IO;
using VisusCore.FileDB.Structure;

namespace VisusCore.FileDB.Factories;

internal static class FileFactory
{
    private static readonly Guid _fixedMiddleGuid = new(
        new byte[] { 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127 });

    public static void CreateEmptyFile(BinaryWriter writer)
    {
        // Create new header instance
        var header = new Header
        {
            IndexRootPageID = 0,
            FreeIndexPageID = 0,
            FreeDataPageID = uint.MaxValue,
            LastFreeDataPageID = uint.MaxValue,
            LastPageID = 0,
        };

        HeaderFactory.WriteToFile(header, writer);

        // Create a first fixed index page
        var pageIndex = new IndexPage(0)
        {
            NodeIndex = 0,
            NextPageID = uint.MaxValue,
        };

        // Create first fixed index node, with fixed middle guid
        var indexNode = pageIndex.Nodes[0];
        indexNode.ID = _fixedMiddleGuid;
        indexNode.IsDeleted = true;
        indexNode.Right = new IndexLink();
        indexNode.Left = new IndexLink();
        indexNode.DataPageID = uint.MaxValue;
        indexNode.FileName = string.Empty;
        indexNode.FileExtension = string.Empty;

        PageFactory.WriteToFile(pageIndex, writer);
    }
}
