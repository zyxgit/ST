namespace ST.MS.FileUpload.Application.Services;

/// <summary>
/// 顺序拼接读取流。
/// 将多个 Stream 按顺序串联为一个可读 Stream，避免将所有数据加载到内存。
/// 不支持 Seek/Write/Length。
/// </summary>
internal sealed class ConcatenatedReadStream : Stream
{
	private readonly IReadOnlyList<Stream> _streams;
	private int _currentIndex;
	private long _totalBytesRead;
	private bool _disposed;

	public ConcatenatedReadStream(IReadOnlyList<Stream> streams)
	{
		_streams = streams ?? throw new ArgumentNullException(nameof(streams));
		if (streams.Count == 0)
			throw new ArgumentException("至少需要一个流", nameof(streams));
	}

	public override bool CanRead => true;
	public override bool CanSeek => false;
	public override bool CanWrite => false;
	public override long Length => throw new NotSupportedException();
	public override long Position
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		ValidateNotDisposed();

		while (_currentIndex < _streams.Count)
		{
			var stream = _streams[_currentIndex];
			var bytesRead = await stream.ReadAsync(buffer, offset, count, cancellationToken);

			if (bytesRead > 0)
			{
				_totalBytesRead += bytesRead;
				return bytesRead;
			}

			// 当前流读完，切换到下一个
			_currentIndex++;
		}

		return 0; // 所有流读完
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		ValidateNotDisposed();

		while (_currentIndex < _streams.Count)
		{
			var stream = _streams[_currentIndex];
			var bytesRead = stream.Read(buffer, offset, count);

			if (bytesRead > 0)
			{
				_totalBytesRead += bytesRead;
				return bytesRead;
			}

			_currentIndex++;
		}

		return 0;
	}

	protected override void Dispose(bool disposing)
	{
		if (!_disposed && disposing)
		{
			_disposed = true;
			foreach (var stream in _streams)
			{
				stream.Dispose();
			}
		}
		base.Dispose(disposing);
	}

	public override async ValueTask DisposeAsync()
	{
		if (!_disposed)
		{
			_disposed = true;
			foreach (var stream in _streams)
			{
				await stream.DisposeAsync();
			}
		}
		await base.DisposeAsync();
	}

	private void ValidateNotDisposed()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	// 不支持的操作
	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
	public override void SetLength(long value) => throw new NotSupportedException();
	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	public override void Flush() { }
}
