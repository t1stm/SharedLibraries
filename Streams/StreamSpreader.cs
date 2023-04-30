#nullable enable

namespace Streams;

public class StreamSpreader : Stream
{
    /// <summary>
    /// The cached written data.
    /// </summary>
    protected readonly Queue<byte[]> Data = new();
    
    /// <summary>
    /// The dictionary that contains the Streams and their write tasks.
    /// </summary>
    protected readonly Dictionary<Stream, Task> DestinationDictionary = new();
    
    /// <summary>
    /// The given cancellation token.
    /// </summary>
    protected readonly CancellationToken CancellationToken = CancellationToken.None;

    /// <summary>
    /// This is the semaphore that is released when the writing source has finished writing to the destination.
    /// Must be called in order to free all threads awaiting the subscribe method.
    /// </summary>
    private readonly SemaphoreSlim ClosingSemaphore = new(0);

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
        FlushAsync(CancellationToken).Wait(CancellationToken);
    }
    
    /// <summary>
    /// Waits asynchronously for all streams to finish copying.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await AwaitFinish(cancellationToken);

        foreach (var task in DestinationDictionary.Values)
        {
            await task.WaitAsync(CancellationToken).WaitAsync(cancellationToken);
        }

        foreach (var stream in DestinationDictionary.Keys)
        {
            await stream.FlushAsync(cancellationToken);
        }
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

        lock (Data) Data.Enqueue(owned_buffer);
        
        lock (DestinationDictionary)
        foreach (var (stream, task) in DestinationDictionary)
        {
            DestinationDictionary[stream] = task.ContinueWith(async _ =>
            {
                if (!IsAsynchronous)
                {
                    stream.Write(owned_buffer);
                    return;
                }
                
                await stream.WriteAsync(owned_buffer, CancellationToken);
            }, CancellationToken).Unwrap();
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

    public override void Close()
    {
        foreach (var stream in DestinationDictionary.Keys)
        {
            stream.Close();
        }
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

        byte[][] data;
        lock (Data)
        {
            data = Data.ToArray();
        }

        if (KeepCached)
        {
            new_task = data.Aggregate(new_task, (current, write_data) => 
                current.ContinueWith(async _ =>
                {
                    await stream.WriteAsync(write_data, CancellationToken);
                }, CancellationToken).Unwrap());
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
    /// This method closes the StreamSpreader, and causes every thread subscribed to the close method to be unblocked.
    /// </summary>
    public void FinishWriting()
    {
        ClosingSemaphore.Release();
    }

    public async Task AwaitFinish(CancellationToken token)
    {
        await ClosingSemaphore.WaitAsync(token).WaitAsync(CancellationToken);
        ClosingSemaphore.Release();
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