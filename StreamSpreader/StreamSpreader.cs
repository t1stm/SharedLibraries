#nullable enable

using System.Collections.Concurrent;

namespace StreamSpreader;

public class StreamSpreader : Stream
{
    private readonly ConcurrentQueue<byte[]> Data = new();
    private readonly ConcurrentDictionary<Stream, Task> DestinationDictionary = new();
    private readonly bool IsAsynchronous;
    
    public StreamSpreader(bool isAsynchronous = false)
    {
        IsAsynchronous = isAsynchronous;
    }

    public override void Flush()
    {
        Task.WhenAll(DestinationDictionary.Values).Wait();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(DestinationDictionary.Values).WaitAsync(cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

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
                await pair.Key.WriteAsync(owned_buffer);
            }
            
            void SyncWrite(Task _)
            {
                pair.Key.Write(owned_buffer);
            }

            DestinationDictionary[pair.Key] = pair.Value.ContinueWith(IsAsynchronous ? 
                AsyncWrite : SyncWrite);
        }
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        var array = buffer.ToArray();
        Write(array, 0, array.Length);
    }
    
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return Task.FromResult(() =>
        {
            Write(buffer, offset, count);
        });
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
    {
        Write(buffer.Span);
        return ValueTask.CompletedTask;
    }

    public void AddDestination(Stream stream)
    {
        var factory = Task.Factory.StartNew(() => { });

        foreach (var write_data in Data)
        {
            async void AsyncWrite(Task _)
            {
                await stream.WriteAsync(write_data);
            }
            
            void SyncWrite(Task _)
            {
                stream.Write(write_data);
            }
            
            factory = factory.ContinueWith(IsAsynchronous ? AsyncWrite : SyncWrite);
        }

        if (!DestinationDictionary.TryAdd(stream, factory))
        {
            throw new Exception("Unable to add to destination dictionary.");
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