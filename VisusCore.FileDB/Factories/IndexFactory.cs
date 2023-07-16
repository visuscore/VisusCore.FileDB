using System;
using VisusCore.FileDB.Exceptions;
using VisusCore.FileDB.Structure;

namespace VisusCore.FileDB.Factories;

internal static class IndexFactory
{
    public static IndexNode GetRootIndexNode(Engine engine)
    {
        var rootIndexPage = engine.CacheIndexPage.GetPage(engine.Header.IndexRootPageID);
        return rootIndexPage.Nodes[0];
    }

    public static IndexNode BinaryInsert(EntryInfo target, IndexNode baseNode, Engine engine)
    {
        var dif = baseNode.ID.CompareTo(target.ID);

        // > Maior (Right)
        if (dif == 1)
        {
            return baseNode.Right.IsEmpty
                ? BinaryInsertNode(baseNode.Right, baseNode, target, engine)
                : BinaryInsert(target, GetChildIndexNode(baseNode.Right, engine), engine);
        }

        // < Menor (Left)
        if (dif == -1)
        {
            return baseNode.Left.IsEmpty
                ? BinaryInsertNode(baseNode.Left, baseNode, target, engine)
                : BinaryInsert(target, GetChildIndexNode(baseNode.Left, engine), engine);
        }

        throw new BlobDatabaseException("Same GUID?!?");
    }

    private static IndexNode GetChildIndexNode(IndexLink link, Engine engine)
    {
        var pageIndex = engine.CacheIndexPage.GetPage(link.PageID);
        return pageIndex.Nodes[link.Index];
    }

    private static IndexNode BinaryInsertNode(IndexLink baseLink, IndexNode baseNode, EntryInfo entry, Engine engine)
    {
        // Must insert my new nodo
        var pageIndex = engine.GetFreeIndexPage();
        var newNode = pageIndex.Nodes[pageIndex.NodeIndex];

        baseLink.PageID = pageIndex.PageID;
        baseLink.Index = pageIndex.NodeIndex;

        newNode.UpdateFromEntry(entry);
        newNode.DataPageID = DataFactory.GetStartDataPageID(engine);

        if (pageIndex.PageID != baseNode.IndexPage.PageID)
            engine.CacheIndexPage.AddPage(baseNode.IndexPage, markAsDirty: true);

        engine.CacheIndexPage.AddPage(pageIndex, markAsDirty: true);

        return newNode;
    }

    public static IndexNode BinarySearch(Guid target, IndexNode baseNode, Engine engine)
    {
        var dif = baseNode.ID.CompareTo(target);

        // > Maior (Right)
        if (dif == 1)
        {
            return baseNode.Right.IsEmpty
                ? null
                // Recursive call on right node
                : BinarySearch(target, GetChildIndexNode(baseNode.Right, engine), engine);
        }

        // < Menor (Left)
        if (dif == -1)
        {
            return baseNode.Left.IsEmpty
                ? null
                // Recursive call on left node
                : BinarySearch(target, GetChildIndexNode(baseNode.Left, engine), engine);
        }

        // Found it
        return baseNode;
    }
}
