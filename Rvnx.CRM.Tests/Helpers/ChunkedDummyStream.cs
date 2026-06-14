using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Rvnx.CRM.Tests.Helpers;

public class ChunkedDummyStream : Stream
{
    private readonly long _totalBytesToSimulate;
    private readonly int _chunkSize;
    private long _bytesRead;

    public ChunkedDummyStream(long totalBytesToSimulate, int chunkSize = 81920)
    {
        _totalBytesToSimulate = totalBytesToSimulate;
        _chunkSize = chunkSize;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _totalBytesToSimulate;
    public override long Position { get => _bytesRead; set => throw new NotSupportedException(); }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_bytesRead >= _totalBytesToSimulate)
        {
            return 0;
        }

        int toRead = (int)Math.Min(count, _chunkSize);
        toRead = (int)Math.Min(toRead, _totalBytesToSimulate - _bytesRead);

        Array.Fill(buffer, (byte)0, offset, toRead);
        _bytesRead += toRead;
        return toRead;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return Task.FromResult(Read(buffer, offset, count));
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_bytesRead >= _totalBytesToSimulate)
        {
            return new ValueTask<int>(0);
        }

        int toRead = Math.Min(buffer.Length, _chunkSize);
        toRead = (int)Math.Min(toRead, _totalBytesToSimulate - _bytesRead);

        buffer.Span.Slice(0, toRead).Clear();
        _bytesRead += toRead;
        return new ValueTask<int>(toRead);
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
