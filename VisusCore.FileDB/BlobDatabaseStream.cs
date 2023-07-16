using System;
using System.IO;
using VisusCore.FileDB.Factories;
using VisusCore.FileDB.Structure;

namespace VisusCore.FileDB;

public sealed class BlobDatabaseStream : Stream
{
    private readonly Engine _engine;
    private readonly long _streamLength;
    private long _streamPosition;
    private DataPage _currentPage;
    private int _positionInPage;

    /// <summary>
    /// Gets file information.
    /// </summary>
    public EntryInfo FileInfo { get; }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _streamLength;

    public override long Position
    {
        get => _streamPosition;
        set => throw new NotSupportedException();
    }

    internal BlobDatabaseStream(Engine engine, Guid id)
    {
        _engine = engine;

        var indexNode = _engine.Search(id);
        if (indexNode != null)
        {
            _streamLength = indexNode.FileLength;
            _currentPage = PageFactory.GetDataPage(indexNode.DataPageID, engine.Reader, onlyHeader: false);
            FileInfo = new EntryInfo(indexNode);
        }
    }

    public override void Flush() =>
        throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesLeft = count;

        while (_currentPage != null && bytesLeft > 0)
        {
            int bytesToCopy = Math.Min(bytesLeft, _currentPage.DataBlockLength - _positionInPage);
            Buffer.BlockCopy(_currentPage.DataBlock, _positionInPage, buffer, offset, bytesToCopy);

            _positionInPage += bytesToCopy;
            bytesLeft -= bytesToCopy;
            offset += bytesToCopy;
            _streamPosition += bytesToCopy;

            if (_positionInPage >= _currentPage.DataBlockLength)
            {
                _positionInPage = 0;

                _currentPage = _currentPage.NextPageID == uint.MaxValue
                    ? null
                    : PageFactory.GetDataPage(_currentPage.NextPageID, _engine.Reader, onlyHeader: false);
            }
        }

        return count - bytesLeft;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
