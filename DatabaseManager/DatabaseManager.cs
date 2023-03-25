#nullable enable
using System.Text.Json;
using System.Timers;
using Timer = System.Timers.Timer;

namespace DatabaseManager;

public class DatabaseManager<T> where T : Model<T>
{
    private readonly string FileLocation;
    private readonly object LockObject = new();
    private readonly string ObjectName;
    private readonly Timer SaveTimer = new();
    private Action<string> Log { get; }
    private bool Modified;

    public DatabaseManager(DatabaseSettings settings)
    {
        SaveTimer.Elapsed += ElapsedEvent;
        SaveTimer.Interval = 300000; // ms | 5 Minutes
        SaveTimer.Start();
        var type = typeof(T);
        var name = type.Name;
        ObjectName = name.Length > 5 && name[^5..] is "Model" or "model" ? name[..^5] : name;
        FileLocation = $"{settings.DatabaseDirectory}/{settings.Filename ?? $"{ObjectName}.json"}";
        Log = settings.LogAction;
    }

    private List<T> Data { get; set; } = new();
    private FileStream? FileStream { get; set; }

    private void ElapsedEvent(object? sender, ElapsedEventArgs e)
    {
        if (!Modified) return;
        Log($"Saving \"{ObjectName}\" database.");
        SaveToFile();
    }

    public void ReadDatabase()
    {
        if (!File.Exists(FileLocation))
            SaveToFile(Enumerable.Empty<T>().ToList());
        ReadFile();
        CallLoadedMethod();
    }

    private void CallLoadedMethod()
    {
        var copy = Data.ToList();

        foreach (var item in copy) 
            item.OnLoaded();
    }

    protected void ModifiedAction()
    {
        Modified = true;
    }

    public T? Read(T searchData)
    {
        T? data;
        lock (Data)
        {
            data = searchData.SearchFrom(Data); // This makes me go over the rainbow.
        }

        if (data != null) data.SetModified = ModifiedAction;
        return data;
    }

    public List<T> ReadCopy()
    {
        lock (Data)
        {
            return Data.ToList();
        }
    }

    public T Add(T addModel)
    {
        lock (Data)
        {
            if (Data.Count == 0) ReadDatabase();
            Data.Add(addModel);
            Modified = true;
        }

        addModel.SetModified = ModifiedAction;
        return addModel;
    }

    private void ReadFile()
    {
        lock (LockObject)
        {
            try
            {
                FileStream = File.Open(FileLocation, FileMode.Open);
                Data = JsonSerializer.Deserialize<List<T>>(FileStream) ?? Enumerable.Empty<T>().ToList();
                FileStream.Close();
            }
            catch (Exception e)
            {
                Log($"Failed to update database file \"{ObjectName}.json\": \"{e}\"");
            }
        }
    }

    public void SaveToFile()
    {
        lock (Data)
        {
            var copy = Data.ToList();
            SaveToFile(copy);
        }
    }

    private void SaveToFile(List<T> data)
    {
        lock (FileStream ?? new object())
        {
            try
            {
                FileStream = File.Open(FileLocation, FileMode.Create);
                JsonSerializer.Serialize(FileStream, data);
                FileStream.Close();
            }
            catch (Exception e)
            {
                Log($"Failed to update database file \"{ObjectName}\": \"{e}\"");
            }
        }
    }
}