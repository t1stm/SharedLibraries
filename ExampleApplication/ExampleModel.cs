using System.Text.Json.Serialization;
using DatabaseManager;

namespace ExampleApplication;

public class ExampleModel : Model<ExampleModel>
{
    [JsonInclude]
    public static int RuntimeIndex;

    [JsonIgnore]
    protected string _exampleData = $"This is a testing string \'{RuntimeIndex}\'";
    
    [JsonInclude] 
    public string ExampleData {
        get => _exampleData;
        set
        {
            SetModified?.Invoke();
            _exampleData = value;
        } 
    }
    public ExampleModel()
    {
        RuntimeIndex++;
    }
    public override ExampleModel? SearchFrom(IEnumerable<ExampleModel> source)
    {
        // Here one must implement the searching behavior using LINQ or something else.
        return source.FirstOrDefault(r => 
            r.ExampleData == ExampleData);
    }

    public override void OnLoaded()
    {
        Console.WriteLine("Loaded.");
    }
}