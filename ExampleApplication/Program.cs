using DatabaseManager;
using ExampleApplication;

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
example.SetModified?.Invoke();

// Manually call the save method.
database.SaveToFile();