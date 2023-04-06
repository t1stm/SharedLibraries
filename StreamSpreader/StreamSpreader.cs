#nullable enable

using System.Collections.Concurrent;

namespace StreamSpreader;

public class StreamSpreader : Stream
{
    private readonly ConcurrentQueue<byte[]> Data = new();
    private readonly ConcurrentDictionary<Stream, Task> DestinationDictionary = new();
    public bool IsAsynchronous { get; init; }
    private readonly CancellationToken CancellationToken = CancellationToken.None;
    
    public StreamSpreader(params Stream[] destinations)
    {
        AddDestinations(destinations);
    }
    
    public StreamSpreader(CancellationToken cancellationToken, params Stream[] destinations)
    {
        CancellationToken = cancellationToken;
        AddDestinations(destinations);
    }

    public override void Flush()
    {
        Task.WhenAll(DestinationDictionary.Values).Wait(CancellationToken);
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
        var factory = Task.Factory.StartNew(() => { }, CancellationToken);

        foreach (var write_data in Data)
        {
            async void AsyncWrite(Task _)
            {
                await stream.WriteAsync(write_data, CancellationToken);
            }
            
            void SyncWrite(Task _)
            {
                stream.Write(write_data);
            }
            
            factory = factory.ContinueWith(IsAsynchronous ? AsyncWrite : SyncWrite, CancellationToken);
        }

        if (!DestinationDictionary.TryAdd(stream, factory))
        {
            throw new Exception("Unable to add to destination dictionary.");
        }
    }

    public void AddDestinations(params Stream[] destinations)
    {
        foreach (var stream in destinations)
        {
            AddDestination(stream);
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