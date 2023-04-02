using DatabaseManager;
using ExampleApplication;
using Result;
using Result.Objects;

var databaseSettings = new DatabaseSettings("./")
{
    Filename = "ExampleDatabase.json",
    LogAction = Console.WriteLine
};
var database = new DatabaseManager<ExampleModel>(databaseSettings);

var example = new ExampleModel
{
    ExampleData = "yes"
};
database.Add(example);
example.ExampleData = "yes2";

// When this is called the database knows that the object is modified and gets saved on the next save interval. 
// In this case it isn't needed because the ExampleModel.ExampleData setter already contains the SetModified invoke.
// example.SetModified?.Invoke();

// Manually call the save method.
database.SaveToFile();

// Result Example
var result = Result<string, Empty>.Success("This is successful.");

if (result == Status.OK)
{
    Console.WriteLine("Result is \'ok\'.");
    Console.WriteLine(result.GetOK());
    return;
}
Console.WriteLine("Result is \'error\'.");

var empty = result.GetError();
Console.WriteLine(empty);

// This will now throw an InvalidResultAccessException
result.GetOK();