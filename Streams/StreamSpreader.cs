#nullable enable

using System.Collections.Concurrent;

namespace Streams;

public class StreamSpreader : Stream
{
    /// <summary>
    /// The cached written data.
    /// </summary>
    protected readonly ConcurrentQueue<byte[]> Data = new();
    
    /// <summary>
    /// The dictionary that contains the Streams and their write tasks.
    /// </summary>
    protected readonly ConcurrentDictionary<Stream, Task> DestinationDictionary = new();
    
    /// <summary>
    /// The given cancellation token.
    /// </summary>
    protected readonly CancellationToken CancellationToken = CancellationToken.None;

    /// <summary>
    /// Buffer size in bytes.
    /// </summary>
    protected const int BufferSize = 1024;

    /// <summary>
    /// Change whether the copy tasks use asynchronous copying.
    /// </summary>
    public bool IsAsynchronous { get; init; }
    
    /// <summary>
    /// Sets whether the StreamSpreader keeps all data passed through it cached if a source is added after data has been written.
    /// </summary>
    public bool KeepCached { get; init; }
    
    /// <summary>
    /// Creates a wrapper that allows writing to multiple streams at the same time.
    /// </summary>
    /// <param name="destinations">The destination streams.</param>
    public StreamSpreader(params Stream[] destinations)
    {
        AddDestinations(destinations);
    }
    
    /// <summary>
    /// Creates a wrapper that allows writing to multiple streams at the same time.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token, that terminates the copying.</param>
    /// <param name="destinations">The destination streams.</param>
    public StreamSpreader(CancellationToken cancellationToken, params Stream[] destinations)
    {
        CancellationToken = cancellationToken;
        AddDestinations(destinations);
    }
    
    /// <summary>
    /// Waits synchronously for all streams to finish copying.
    /// </summary>
    public override void Flush()
    {
        Task.WhenAll(DestinationDictionary.Values).Wait(CancellationToken);
    }
    
    /// <summary>
    /// Waits asynchronously for all streams to finish copying.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(DestinationDictionary.Values)
            .WaitAsync(CancellationToken).WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Usually is an action that reads the buffer. This isn't supported.
    /// </summary>
    /// <param name="buffer">The buffer you want to read to.</param>
    /// <param name="offset">The offset of the buffer.</param>
    /// <param name="count">The count of bytes you want to read.</param>
    /// <returns>An exception.</returns>
    /// <exception cref="NotSupportedException">Exception that is always thrown. This method isn't supported.</exception>
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Seeks the stream to a new location.
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="origin"></param>
    /// <returns>An exception.</returns>
    /// <exception cref="NotSupportedException"></exception>
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }
    /// <summary>
    /// Usually sets the length of the current stream. This isn't supported.
    /// </summary>
    /// <param name="value">The length of the stream.</param>
    /// <exception cref="NotSupportedException">Exception that is always thrown. This method isn't supported.</exception>
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }
    
    /// <summary>
    /// Writes to all the destination streams.
    /// </summary>
    /// <param name="buffer">The buffer of data you want to write to all the streams.</param>
    /// <param name="offset">Offset of the given data.</param>
    /// <param name="count">The amount of bytes you want to write.</param>
    public override void Write(byte[] buffer, int offset, int count)
    {
        var segment = new ArraySegment<byte>(buffer);
        var array_slice = segment.Slice(offset, count);
        
        var owned_buffer = new byte[array_slice.Count];
        array_slice.CopyTo(owned_buffer);

        Data.Enqueue(owned_buffer);

        foreach (var pair in DestinationDictionary)
        {
            async void AsyncWrite(Task _)
            {
                await pair.Key.WriteAsync(owned_buffer, CancellationToken);
            }
            
            void SyncWrite(Task _)
            {
                pair.Key.Write(owned_buffer);
            }

            DestinationDictionary[pair.Key] = pair.Value.ContinueWith(IsAsynchronous ? 
                AsyncWrite : SyncWrite, CancellationToken);
        }
    }

    /// <summary>
    /// Writes to all the destination streams.
    /// </summary>
    /// <param name="buffer">The data buffer to write.</param>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        var array = buffer.ToArray();
        Write(array, 0, array.Length);
    }
    
    /// <summary>
    /// Writes to all the destination streams.
    /// </summary>
    /// <param name="buffer">The buffer of data you want to write to all the streams.</param>
    /// <param name="offset">Offset of the given data.</param>
    /// <param name="count">The amount of bytes you want to write.</param>
    /// <param name="cancellationToken">A cancellation token, that cancels the copying. This value is ignored.</param>
    /// <returns>A Task representing the write action.</returns>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return Task.FromResult(() =>
        {
            Write(buffer, offset, count);
        });
    }

    /// <summary>
    /// Writes to all the destination streams.
    /// </summary>
    /// <param name="buffer">The buffer of data you want to write.</param>
    /// <param name="cancellationToken">A cancellation token, that cancels the copying. This value is ignored.</param>
    /// <returns>A ValueTask representing the write action.</returns>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
    {
        Write(buffer.Span);
        return ValueTask.CompletedTask;
    }
    
    /// <summary>
    /// Adds a new destination stream to the StreamSpreader.
    /// </summary>
    /// <param name="stream">The destination stream to add.</param>
    /// <exception cref="InvalidOperationException">An exception that is thrown when the stream is unable to be added to the stream dictionary.</exception>
    public void AddDestination(Stream stream)
    {
        var new_task = new Task(() => { }, CancellationToken);
        new_task.Start();

        if (KeepCached) foreach (var write_data in Data)
        {
            async void AsyncWrite(Task _)
            {
                await stream.WriteAsync(write_data, CancellationToken);
            }
            
            void SyncWrite(Task _)
            {
                stream.Write(write_data);
            }
            
            new_task = new_task.ContinueWith(IsAsynchronous ? AsyncWrite : SyncWrite, CancellationToken);
        }

        if (!DestinationDictionary.TryAdd(stream, new_task))
        {
            throw new InvalidOperationException("Unable to add to destination dictionary.");
        }
    }

    /// <summary>
    /// Adds multiple destination streams to the StreamSpreader.
    /// </summary>
    /// <param name="destinations">The destination streams you want to add.</param>
    public void AddDestinations(params Stream[] destinations)
    {
        foreach (var stream in destinations)
        {
            AddDestination(stream);
        }
    }

    /// <summary>
    /// Reads an entire stream and writes it to all the destination streams.
    /// </summary>
    /// <param name="source">The souce stream.</param>
    /// <param name="readCancellationToken">A token that cancels the read action.</param>
    public void ReadStreamToEnd(Stream source, CancellationToken? readCancellationToken = null)
    {
        readCancellationToken ??= CancellationToken.None;

        var buffer = new byte[BufferSize];
        int bytes_read;
        while ((bytes_read = source.Read(buffer)) < 0)
        {
            if (readCancellationToken?.IsCancellationRequested ?? false) 
                break;
            Write(buffer, 0, bytes_read);
        }
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => 0;
    public override long Position
    {
        get => 0;
        set => throw new InvalidOperationException();
    }
}