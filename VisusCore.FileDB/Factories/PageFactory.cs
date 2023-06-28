using System.IO;
using VisusCore.FileDB.Exceptions;
using VisusCore.FileDB.Extensions;
using VisusCore.FileDB.Structure;

namespace VisusCore.FileDB.Factories;

internal static class PageFactory
{
    public static void ReadFromFile(IndexPage indexPage, BinaryReader reader)
    {
        // Seek the stream to the fist byte on page
        long initPos = reader.Seek(Header.HeaderSize + (indexPage.PageID * BasePage.PageSize));

        if (reader.ReadByte() != (byte)PageType.Index)
            throw new FileDBException("PageID {0} is not a Index Page", indexPage.PageID);

        indexPage.NextPageID = reader.ReadUInt32();
        indexPage.NodeIndex = reader.ReadByte();

        // Seek the stream to end of header data page
        reader.Seek(initPos + IndexPage.HeaderSize);

        for (int i = 0; i <= indexPage.NodeIndex; i++)
        {
            var node = indexPage.Nodes[i];

            node.ID = reader.ReadGuid();

            node.IsDeleted = reader.ReadBoolean();

            node.Right.Index = reader.ReadByte();
            node.Right.PageID = reader.ReadUInt32();
            node.Left.Index = reader.ReadByte();
            node.Left.PageID = reader.ReadUInt32();

            node.DataPageID = reader.ReadUInt32();

            node.FileName = reader.ReadString(IndexNode.FileNameSize);
            node.FileExtension = reader.ReadString(IndexNode.FileExtensionSize);
            node.FileLength = reader.ReadUInt32();
        }
    }

    public static void ReadFromFile(DataPage dataPage, BinaryReader reader, bool onlyHeader)
    {
        // Seek the stream on first byte from data page
        long initPos = reader.Seek(Header.HeaderSize + (dataPage.PageID * BasePage.PageSize));

        if (reader.ReadByte() != (byte)PageType.Data)
            throw new FileDBException("PageID {0} is not a Data Page", dataPage.PageID);

        dataPage.NextPageID = reader.ReadUInt32();
        dataPage.IsEmpty = reader.ReadBoolean();
        dataPage.DataBlockLength = reader.ReadInt16();

        // If page is empty or onlyHeader parameter, I don't read data content
        if (!dataPage.IsEmpty && !onlyHeader)
        {
            // Seek the stream at the end of page header
            reader.Seek(initPos + DataPage.HeaderSize);

            // Read all bytes from page
            dataPage.DataBlock = reader.ReadBytes(dataPage.DataBlockLength);
        }
    }

    public static void WriteToFile(IndexPage indexPage, BinaryWriter writer)
    {
        // Seek the stream to the fist byte on page
        long initPos = writer.Seek(Header.HeaderSize + (indexPage.PageID * BasePage.PageSize));

        // Write page header
        writer.Write((byte)indexPage.Type);
        writer.Write(indexPage.NextPageID);
        writer.Write(indexPage.NodeIndex);

        // Seek the stream to end of header index page
        writer.Seek(initPos + IndexPage.HeaderSize);

        for (int i = 0; i <= indexPage.NodeIndex; i++)
        {
            var node = indexPage.Nodes[i];

            writer.Write(node.ID);

            writer.Write(node.IsDeleted);

            writer.Write(node.Right.Index);
            writer.Write(node.Right.PageID);
            writer.Write(node.Left.Index);
            writer.Write(node.Left.PageID);

            writer.Write(node.DataPageID);

            writer.Write(node.FileName.ToBytes(IndexNode.FileNameSize));
            writer.Write(node.FileExtension.ToBytes(IndexNode.FileExtensionSize));
            writer.Write(node.FileLength);
        }
    }

    public static void WriteToFile(DataPage dataPage, BinaryWriter writer)
    {
        // Seek the stream on first byte from data page
        long initPos = writer.Seek(Header.HeaderSize + (dataPage.PageID * BasePage.PageSize));

        // Write data page header
        writer.Write((byte)dataPage.Type);
        writer.Write(dataPage.NextPageID);
        writer.Write(dataPage.IsEmpty);
        writer.Write(dataPage.DataBlockLength);

        // I will only save data content if the page is not empty
        if (!dataPage.IsEmpty)
        {
            // Seek the stream at the end of page header
            writer.Seek(initPos + DataPage.HeaderSize);

            writer.Write(dataPage.DataBlock, 0, dataPage.DataBlockLength);
        }
    }

    public static IndexPage GetIndexPage(uint pageID, BinaryReader reader)
    {
        var indexPage = new IndexPage(pageID);
        ReadFromFile(indexPage, reader);

        return indexPage;
    }

    public static DataPage GetDataPage(uint pageID, BinaryReader reader, bool onlyHeader)
    {
        var dataPage = new DataPage(pageID);
        ReadFromFile(dataPage, reader, onlyHeader);

        return dataPage;
    }

    public static BasePage GetBasePage(uint pageID, BinaryReader reader)
    {
        // Seek the stream at begin of page
        reader.Seek(Header.HeaderSize + (pageID * BasePage.PageSize));

        if (reader.ReadByte() == (byte)PageType.Index)
            return GetIndexPage(pageID, reader);

        return GetDataPage(pageID, reader, onlyHeader: true);
    }
}
