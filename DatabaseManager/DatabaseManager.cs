#nullable enable
using System.Text.Json;
using System.Timers;
using Timer = System.Timers.Timer;

namespace DatabaseManager;

public class DatabaseManager<T> where T : Model<T>
{
    private readonly string FileLocation;
    private readonly object ReadWriteLock = new();
    private readonly string ObjectName;
    private readonly Timer SaveTimer = new();
    private Action<string> Log { get; }
    private bool Modified;

    /// <summary>
    /// Creates a new DatabaseManager with a JSON backend.
    /// </summary>
    /// <param name="settings">Settings for the database manager.</param>
    public DatabaseManager(DatabaseSettings settings)
    {
        SaveTimer.Elapsed += ElapsedEvent;
        SaveTimer.Interval = 300000; // ms | 5 Minutes
        var type = typeof(T);
        var name = type.Name;
        ObjectName = name.Length > 5 && name[^5..] is "Model" or "model" ? name[..^5] : name;
        FileLocation = $"{settings.DatabaseDirectory}/{settings.Filename ?? $"{ObjectName}.json"}";
        Log = settings.LogAction;
    }

    private List<T> Data { get; set; } = new();
    private FileStream? FileStream { get; set; }

    /// <summary>
    /// Event that handles the save timer.
    /// </summary>
    /// <param name="sender">Ignored. Usually it's the timer.</param>
    /// <param name="e">Ignored. Usually it's empty.</param>
    private void ElapsedEvent(object? sender, ElapsedEventArgs e)
    {
        if (!Modified) return;
        Log($"Saving \"{ObjectName}\" database.");
        SaveToFile();
    }

    /// <summary>
    /// Read the database into memory.
    /// </summary>
    public void ReadDatabase()
    {
        if (!File.Exists(FileLocation))
            SaveToFile(Enumerable.Empty<T>().ToList());
        ReadFile();
        CallLoadedMethod();
        SaveTimer.Start();
    }
    
    /// <summary>
    /// If current model has a OnLoaded override, calls it on every object in the database.
    /// </summary>
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

    /// <summary>
    /// Searches the database using a template model.
    /// </summary>
    /// <param name="searchData">The search model you want to use.</param>
    /// <returns>The found object in the database.</returns>
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

    /// <summary>
    /// Gets a copy of the backing list.
    /// </summary>
    /// <returns>A copy of the backing list.</returns>
    public List<T> ReadCopy()
    {
        lock (Data)
        {
            return Data.ToList();
        }
    }

    /// <summary>
    /// Adds a new entry to the database.
    /// </summary>
    /// <param name="addModel">The object you want to add.</param>
    /// <returns>The added object.</returns>
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
        lock (ReadWriteLock)
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

    /// <summary>
    /// Saves the database.
    /// </summary>
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
        lock (ReadWriteLock)
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