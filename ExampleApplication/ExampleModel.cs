using System.Text.Json.Serialization;
using DatabaseManager;

namespace ExampleApplication;

public class ExampleModel : Model<ExampleModel>
{
    [JsonInclude]
    public static int RuntimeIndex;
    
    [JsonInclude] 
    public string ExampleData = $"This is a testing string \'{RuntimeIndex}\'";
    public ExampleModel()
    {
        RuntimeIndex++;
    }
    public override ExampleModel? SearchFrom(IEnumerable<ExampleModel> source)
    {
        // Here one must implement the searching behavior using LINQ or something else.
        return null;
    }
}