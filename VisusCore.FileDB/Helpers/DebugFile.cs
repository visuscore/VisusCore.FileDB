using System.Globalization;
using System.Text;
using VisusCore.FileDB.Factories;
using VisusCore.FileDB.Structure;

namespace VisusCore.FileDB.Helpers;

public class DebugFile
{
    private readonly Engine _engine;

    internal DebugFile(Engine engine) =>
        _engine = engine;

    public string DisplayPages()
    {
        var sb = new StringBuilder();

        sb.AppendLine("Constants:");
        sb.AppendLine("=============");
        sb.AppendFormat(CultureInfo.InvariantCulture, "BasePage.PageSize       : {0}", BasePage.PageSize);
        sb.AppendFormat(CultureInfo.InvariantCulture, "IndexPage.HeaderSize    : {0}", IndexPage.HeaderSize);
        sb.AppendFormat(CultureInfo.InvariantCulture, "IndexPage.NodesPerPage  : {0}", IndexPage.NodesPerPage);
        sb.AppendFormat(CultureInfo.InvariantCulture, "DataPage.HeaderSize     : {0}", DataPage.HeaderSize);
        sb.AppendFormat(CultureInfo.InvariantCulture, "DataPage.DataPerPage    : {0}", DataPage.DataPerPage);

        sb.AppendLine();
        sb.AppendLine("Header:");
        sb.AppendLine("=============");
        sb.AppendLine("IndexRootPageID    : " + _engine.Header.IndexRootPageID.Fmt());
        sb.AppendLine("FreeIndexPageID    : " + _engine.Header.FreeIndexPageID.Fmt());
        sb.AppendLine("FreeDataPageID     : " + _engine.Header.FreeDataPageID.Fmt());
        sb.AppendLine("LastFreeDataPageID : " + _engine.Header.LastFreeDataPageID.Fmt());
        sb.AppendLine("LastPageID         : " + _engine.Header.LastPageID.Fmt());

        sb.AppendLine();
        sb.AppendLine("Pages:");
        sb.AppendLine("=============");

        for (uint i = 0; i <= _engine.Header.LastPageID; i++)
        {
            var page = PageFactory.GetBasePage(i, _engine.Reader);

            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "[{0}] >> [{1}] ({2}) ",
                page.PageID.Fmt(),
                page.NextPageID.Fmt(),
                page.Type == PageType.Data ? "D" : "I");

            if (page.Type == PageType.Data)
            {
                var dataPage = (DataPage)page;

                if (dataPage.IsEmpty)
                    sb.Append("Empty");
                else
                    sb.AppendFormat(CultureInfo.InvariantCulture, "Bytes: {0}", dataPage.DataBlockLength);
            }
            else
            {
                var indexPage = (IndexPage)page;

                sb.AppendFormat(CultureInfo.InvariantCulture, "Keys: {0}", indexPage.NodeIndex + 1);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}

internal static class Display
{
    public static string Fmt(this uint val) =>
        val == uint.MaxValue
        ? "----"
        : val.ToString("0000", CultureInfo.InvariantCulture);
}
