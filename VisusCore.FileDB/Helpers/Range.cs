namespace VisusCore.FileDB.Helpers;

internal sealed class Range<TStart, TEnd>
{
    public TStart Start { get; set; }
    public TEnd End { get; set; }

    public Range()
    {
    }

    public Range(TStart start, TEnd end)
    {
        Start = start;
        End = end;
    }
}
