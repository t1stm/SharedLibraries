namespace StreamSpreader.UnitTests;

public class Tests
{
    private MemoryStream JunkData = new();
    [SetUp]
    public void Setup()
    {
        JunkData = new MemoryStream();
        const int dummy_length = 0x1000; // 4 KB
        var random = new Random();
        var buffer = new byte[dummy_length];
        random.NextBytes(buffer);

        JunkData.Write(buffer);
        Assert.That(dummy_length, Is.EqualTo(JunkData.Length));
    }

    [Test]
    public void CopyTest_StandardCopy_NormalSituation_Multiple([Values(16, 32, 64, 0x100)] int array_count)
    {
        var stream_spreader = new StreamSpreader();
        var junk_data_buffer = JunkData.ToArray();
        var memory_streams = new MemoryStream[array_count];

        for (var i = 0; i < memory_streams.Length; i++)
        {
            memory_streams[i] = new MemoryStream();
        }

        foreach (var stream in memory_streams)
        {
            stream_spreader.AddDestination(stream);   
        }

        stream_spreader.Write(junk_data_buffer);
        stream_spreader.Flush();

        foreach (var stream in memory_streams)
        {
            var data = stream.ToArray();
            CollectionAssert.AreEqual(junk_data_buffer, data);
        }
    }
    
    [Test]
    public async Task CopyTest_AsyncCopy_NormalSituation_Multiple([Values(16, 32, 64, 0x100)] int array_count)
    {
        var stream_spreader = new StreamSpreader
        {
            IsAsynchronous = true
        };
        var junk_data_buffer = JunkData.ToArray();
        var memory_streams = new MemoryStream[array_count];

        for (var i = 0; i < memory_streams.Length; i++)
        {
            memory_streams[i] = new MemoryStream();
        }

        foreach (var stream in memory_streams)
        {
            stream_spreader.AddDestination(stream);   
        }

        await stream_spreader.WriteAsync(junk_data_buffer);
        await stream_spreader.FlushAsync();

        foreach (var stream in memory_streams)
        {
            var data = stream.ToArray();
            CollectionAssert.AreEqual(junk_data_buffer, data);
        }
    }
    
    [Test]
    public void CopyTest_StandardCopy_FragmentedCopy_Multiple([Values(16, 32, 64, 0x100)] int array_count)
    {
        var stream_spreader = new StreamSpreader();
        var junk_data_buffer = JunkData.ToArray();
        
        var first_slice = junk_data_buffer[..(junk_data_buffer.Length / 2)];
        var second_slice = junk_data_buffer[(junk_data_buffer.Length / 2)..];
        
        var memory_streams = new MemoryStream[array_count];

        for (var i = 0; i < memory_streams.Length; i++)
        {
            memory_streams[i] = new MemoryStream();
        }

        foreach (var stream in memory_streams)
        {
            stream_spreader.AddDestination(stream);   
        }

        stream_spreader.Write(first_slice);
        stream_spreader.Write(second_slice);
        
        stream_spreader.Flush();

        foreach (var stream in memory_streams)
        {
            var data = stream.ToArray();
            CollectionAssert.AreEqual(junk_data_buffer, data);
        }
    }
    
    [Test]
    public void CopyTest_StandardCopy_BufferedCopy_Multiple([Values(16, 32, 64, 0x100)] int array_count)
    {
        var stream_spreader = new StreamSpreader();
        var junk_data_buffer = JunkData.ToArray();
        var junk_data_stream = new MemoryStream(junk_data_buffer);
        
        var memory_streams = new MemoryStream[array_count];

        for (var i = 0; i < memory_streams.Length; i++)
        {
            memory_streams[i] = new MemoryStream();
        }

        foreach (var stream in memory_streams)
        {
            stream_spreader.AddDestination(stream);   
        }

        var buffer = new byte[64];
        int read_bytes;
        while ((read_bytes = junk_data_stream.Read(buffer)) > 0)
        {
            stream_spreader.Write(buffer, 0, read_bytes);
        }
        
        stream_spreader.Flush();

        foreach (var stream in memory_streams)
        {
            var data = stream.ToArray();
            CollectionAssert.AreEqual(junk_data_buffer, data);
        }
    }
    
    [Test]
    public void CopyTestBig_Copy_NormalSituation_Multiple([Values(2, 4)] int array_count)
    {
        const int dummy_length = 0x1000000; // 16 MB
        var stream_spreader = new StreamSpreader();
        var random = new Random();
        
        var buffer = new byte[dummy_length];
        random.NextBytes(buffer);
        
        var memory_streams = new MemoryStream[array_count];

        for (var i = 0; i < memory_streams.Length; i++)
        {
            memory_streams[i] = new MemoryStream();
        }

        foreach (var stream in memory_streams)
        {
            stream_spreader.AddDestination(stream);   
        }

        stream_spreader.Write(buffer);
        stream_spreader.Flush();

        foreach (var stream in memory_streams)
        {
            var data = stream.ToArray();
            CollectionAssert.AreEqual(buffer, data);
        }
    }
}