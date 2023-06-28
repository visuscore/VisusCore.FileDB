using System;
using System.Collections.Generic;
using System.IO;
using VisusCore.FileDB.Extensions;
using VisusCore.FileDB.Factories;
using VisusCore.FileDB.Helpers;
using VisusCore.FileDB.Structure;

namespace VisusCore.FileDB;

internal sealed class Engine : IDisposable
{
    private bool _isDisposed;

    public BinaryReader Reader { get; private set; }
    public BinaryWriter Writer { get; private set; }
    public CacheIndexPage CacheIndexPage { get; private set; } // Used for cache index pages.
    public Header Header { get; private set; }

    public Engine(FileStream stream)
    {
        Reader = new BinaryReader(stream);

        if (stream.CanWrite)
        {
            Writer = new BinaryWriter(stream);
            Writer.Lock(Header.LockerPos, 1);
        }

        Header = new Header();
        HeaderFactory.ReadFromFile(Header, Reader);

        CacheIndexPage = new CacheIndexPage(Reader, Writer, Header.IndexRootPageID);
    }

    public IndexPage GetFreeIndexPage()
    {
        var freeIndexPage = CacheIndexPage.GetPage(Header.FreeIndexPageID);

        // Check if "free page" has no more index to be used
        if (freeIndexPage.NodeIndex >= IndexPage.NodesPerPage - 1)
        {
            Header.LastPageID++; // Take last page and increase
            Header.IsDirty = true;

            var newIndexPage = new IndexPage(Header.LastPageID); // Create a new index page
            freeIndexPage.NextPageID = newIndexPage.PageID; // Point older page to the new page
            Header.FreeIndexPageID = Header.LastPageID; // Update last index page

            CacheIndexPage.AddPage(freeIndexPage, markAsDirty: true);

            return newIndexPage;
        }

        // Has more free index on same index page? return them
        freeIndexPage.NodeIndex++; // Reserve space
        return freeIndexPage;
    }

    public DataPage GetPageData(uint pageID)
    {
        // Page does not exists in disk
        if (pageID == Header.LastPageID)
        {
            var dataPage = new DataPage(pageID);
            return dataPage;
        }

        return PageFactory.GetDataPage(pageID, Reader, onlyHeader: false);
    }

    // Implement file physic storage
    public void Write(EntryInfo entry, Stream stream)
    {
        // Take the first index page
        var rootIndexNode = IndexFactory.GetRootIndexNode(this);

        // Search and insert the index
        var indexNode = IndexFactory.BinaryInsert(entry, rootIndexNode, this);

        // In this moment, the index are ready and saved. I use to add the file
        DataFactory.InsertFile(indexNode, stream, this);

        // Update entry information with file length (I know file length only after read all)
        entry.FileLength = indexNode.FileLength;

        // Only after insert all stream file I confirm that index node is valid
        indexNode.IsDeleted = false;

        // Mask header as dirty for save on dispose
        Header.IsDirty = true;
    }

    public IndexNode Search(Guid id)
    {
        // Take the root node from inital index page
        var rootIndexNode = IndexFactory.GetRootIndexNode(this);

        var indexNode = IndexFactory.BinarySearch(id, rootIndexNode, this);

        // Returns null with not found the record, return false
        if (indexNode == null || indexNode.IsDeleted)
            return null;

        return indexNode;
    }

    public EntryInfo Read(Guid id, Stream stream)
    {
        // Search from index node
        var indexNode = Search(id);

        // If index node is null, not found the guid
        if (indexNode == null)
            return null;

        // Create a entry based on index node
        var entry = new EntryInfo(indexNode);

        // Read data from the index pointer to stream
        DataFactory.ReadFile(indexNode, stream, this);

        return entry;
    }

    public FileDBStream OpenRead(Guid id)
    {
        // Open a FileDBStream and return to user
        var file = new FileDBStream(this, id);

        // If FileInfo is null, ID was not found
        return file.FileInfo == null ? null : file;
    }

    public bool Delete(Guid id)
    {
        // Search index node from guid
        var indexNode = Search(id);

        // If null, not found (return false)
        if (indexNode == null)
            return false;

        // Delete the index node logicaly
        indexNode.IsDeleted = true;

        // Add page (from index node) to cache and set as dirty
        CacheIndexPage.AddPage(indexNode.IndexPage, markAsDirty: true);

        // Mark all data blocks (from data pages) as IsEmpty = true
        DataFactory.MarkAsEmpty(indexNode.DataPageID, this);

        // Set header as Dirty to be saved on dispose
        Header.IsDirty = true;

        return true; // Confirma a exclusão
    }

    public EntryInfo[] ListAllFiles()
    {
        // Get root index page from cache
        var pageIndex = CacheIndexPage.GetPage(Header.IndexRootPageID);
        bool cont = true;

        var list = new List<EntryInfo>();

        while (cont)
        {
            for (int i = 0; i <= pageIndex.NodeIndex; i++)
            {
                // Convert node (if is not logicaly deleted) to Entry
                var node = pageIndex.Nodes[i];
                if (!node.IsDeleted)
                    list.Add(new EntryInfo(node));
            }

            // Go to the next page
            if (pageIndex.NextPageID != uint.MaxValue)
                pageIndex = CacheIndexPage.GetPage(pageIndex.NextPageID);
            else
                cont = false;
        }

        return list.ToArray();
    }

    public void PersistPages()
    {
        // Check if header is dirty and save to disk
        if (Header.IsDirty)
        {
            HeaderFactory.WriteToFile(Header, Writer);
            Header.IsDirty = false;
        }

        // Persist all index pages that are dirty
        CacheIndexPage.PersistPages();
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                if (Writer is not null)
                {
                    // Unlock the file, prevent concurrence writing
                    Writer.Unlock(Header.LockerPos, 1);
                    Writer.Close();
                }

                Reader.Close();
            }

            _isDisposed = true;
        }
    }

    public void Dispose() =>
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
}
