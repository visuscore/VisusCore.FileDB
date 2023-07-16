using System;
using System.IO;
using VisusCore.FileDB.Exceptions;
using VisusCore.FileDB.Structure;

namespace VisusCore.FileDB.Factories;

internal static class DataFactory
{
    public static uint GetStartDataPageID(Engine engine)
    {
        // I have free page inside the disk file. Use it
        if (engine.Header.FreeDataPageID != uint.MaxValue)
        {
            // Take the first free data page
            var startPage = PageFactory.GetDataPage(engine.Header.FreeDataPageID, engine.Reader, onlyHeader: true);

            engine.Header.FreeDataPageID = startPage.NextPageID; // and point the free page to new free one

            // If the next page is MAX, fix too LastFreeData

            if (engine.Header.FreeDataPageID == uint.MaxValue)
                engine.Header.LastFreeDataPageID = uint.MaxValue;

            return startPage.PageID;
        }

        // Don't have free data pages, create new one.
        engine.Header.LastPageID++;

        return engine.Header.LastPageID;
    }

    // Take a new data page on sequence and update the last
    public static DataPage GetNewDataPage(DataPage basePage, Engine engine)
    {
        if (basePage.NextPageID != uint.MaxValue)
        {
            PageFactory.WriteToFile(basePage, engine.Writer); // Write last page on disk

            var dataPage = PageFactory.GetDataPage(basePage.NextPageID, engine.Reader, onlyHeader: false);

            engine.Header.FreeDataPageID = dataPage.NextPageID;

            if (engine.Header.FreeDataPageID == uint.MaxValue)
                engine.Header.LastFreeDataPageID = uint.MaxValue;

            return dataPage;
        }

        var pageID = ++engine.Header.LastPageID;
        var newPage = new DataPage(pageID);
        basePage.NextPageID = newPage.PageID;
        PageFactory.WriteToFile(basePage, engine.Writer); // Write last page on disk

        return newPage;
    }

    public static void InsertFile(IndexNode node, Stream stream, Engine engine)
    {
        DataPage dataPage = null;
        var buffer = new byte[DataPage.DataPerPage];
        uint totalBytes = 0;
        int read;

        while ((read = stream.Read(buffer, 0, (int)DataPage.DataPerPage)) > 0)
        {
            totalBytes += (uint)read;

            dataPage = dataPage is null
                ? engine.GetPageData(node.DataPageID)
                : GetNewDataPage(dataPage, engine);

            if (!dataPage.IsEmpty) // This is never to happend!!
                throw new BlobDatabaseException("Page {0} is not empty", dataPage.PageID);

            Array.Copy(buffer, dataPage.DataBlock, read);
            dataPage.IsEmpty = false;
            dataPage.DataBlockLength = (short)read;
        }

        if (dataPage is null)
        {
            throw new BlobDatabaseException($"{dataPage} is null.");
        }

        // If the last page point to another one, i need to fix that
        if (dataPage.NextPageID != uint.MaxValue)
        {
            engine.Header.FreeDataPageID = dataPage.NextPageID;
            dataPage.NextPageID = uint.MaxValue;
        }

        // Salve the last page on disk
        PageFactory.WriteToFile(dataPage, engine.Writer);

        // Save on node index that file length
        node.FileLength = totalBytes;
    }

    public static void ReadFile(IndexNode node, Stream stream, Engine engine)
    {
        var dataPage = PageFactory.GetDataPage(node.DataPageID, engine.Reader, onlyHeader: false);

        while (dataPage != null)
        {
            stream.Write(dataPage.DataBlock, 0, dataPage.DataBlockLength);

            dataPage = dataPage.NextPageID == uint.MaxValue
                ? null
                : PageFactory.GetDataPage(dataPage.NextPageID, engine.Reader, onlyHeader: false);
        }
    }

    public static void MarkAsEmpty(uint firstPageID, Engine engine)
    {
        var dataPage = PageFactory.GetDataPage(firstPageID, engine.Reader, onlyHeader: true);
        uint lastPageID = uint.MaxValue;
        var cont = true;

        while (cont)
        {
            dataPage.IsEmpty = true;

            PageFactory.WriteToFile(dataPage, engine.Writer);

            if (dataPage.NextPageID != uint.MaxValue)
            {
                lastPageID = dataPage.NextPageID;
                dataPage = PageFactory.GetDataPage(lastPageID, engine.Reader, onlyHeader: true);
            }
            else
            {
                cont = false;
            }
        }

        // Fix header to correct pointer
        // No free pages
        if (engine.Header.FreeDataPageID == uint.MaxValue)
        {
            engine.Header.FreeDataPageID = firstPageID;
            engine.Header.LastFreeDataPageID = lastPageID == uint.MaxValue ? firstPageID : lastPageID;
        }
        else
        {
            // Take the last statment available
            var lastPage = PageFactory.GetDataPage(engine.Header.LastFreeDataPageID, engine.Reader, onlyHeader: true);

            // Point this last statent to first of next one
            if (lastPage.NextPageID != uint.MaxValue || !lastPage.IsEmpty) // This is never to happend!!
                throw new BlobDatabaseException("The page is not empty");

            // Update this last page to first new empty page
            lastPage.NextPageID = firstPageID;

            // Save on disk this update
            PageFactory.WriteToFile(lastPage, engine.Writer);

            // Point header to the new empty page
            engine.Header.LastFreeDataPageID = lastPageID == uint.MaxValue ? firstPageID : lastPageID;
        }
    }
}
