using System.Collections.Generic;
using System.IO;
using System.Linq;
using VisusCore.FileDB.Factories;
using VisusCore.FileDB.Structure;

namespace VisusCore.FileDB.Helpers;

internal delegate void ReleasePageIndexFromCache(IndexPage page);

internal sealed class CacheIndexPage
{
    public const int CacheSize = 200;

    private readonly BinaryReader _reader;
    private readonly BinaryWriter _writer;
    private readonly Dictionary<uint, IndexPage> _cache;
    private readonly uint _rootPageID;

    public CacheIndexPage(BinaryReader reader, BinaryWriter writer, uint rootPageID)
    {
        _reader = reader;
        _writer = writer;
        _cache = new Dictionary<uint, IndexPage>();
        _rootPageID = rootPageID;
    }

    public IndexPage GetPage(uint pageID)
    {
        if (_cache.TryGetValue(pageID, out var indexPage))
            return indexPage;

        indexPage = PageFactory.GetIndexPage(pageID, _reader);

        AddPage(indexPage, markAsDirty: false);

        return indexPage;
    }

    public void AddPage(IndexPage indexPage) =>
        AddPage(indexPage, markAsDirty: false);

    public void AddPage(IndexPage indexPage, bool markAsDirty)
    {
        if (!_cache.ContainsKey(indexPage.PageID))
        {
            if (_cache.Count >= CacheSize)
            {
                // Remove fist page that are not the root page (because I use too much)
                var pageToRemove = _cache.First(x => x.Key != _rootPageID);

                if (pageToRemove.Value.IsDirty)
                {
                    PageFactory.WriteToFile(pageToRemove.Value, _writer);
                    pageToRemove.Value.IsDirty = false;
                }

                _cache.Remove(pageToRemove.Key);
            }

            _cache.Add(indexPage.PageID, indexPage);
        }

        if (markAsDirty)
            indexPage.IsDirty = true;
    }

    public void PersistPages()
    {
        // Check which pages is dirty and need to saved on disk
        var pagesToPersist = _cache.Values.Where(x => x.IsDirty).ToArray();

        if (pagesToPersist.Length > 0)
        {
            foreach (var indexPage in pagesToPersist)
            {
                PageFactory.WriteToFile(indexPage, _writer);
                indexPage.IsDirty = false;
            }
        }
    }
}
