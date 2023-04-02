#nullable enable

namespace DatabaseManager;

public class DatabaseSettings
{
    /// <summary>
    /// The directory where the database file will be stored.
    /// If unset or non-existant, throws a DirectoryNotFoundException.
    /// </summary>
    public readonly string DatabaseDirectory;
    
    /// <summary>
    /// This will be the filename of the database file.
    /// If left null if will be generated by the database manager.
    /// </summary>
    public string? Filename { get; init; }

    /// <summary>
    /// Logging action for the database.
    /// </summary>
    public Action<string> LogAction { get; init; } = _ => { };

    /// <summary>
    /// How often the database checks for changes and saves itself in miliseconds.
    /// </summary>
    public double SaveInterval { get; init; } = 5_000_000; // 5 Minutes

    /// <summary>
    /// Settings object for the DatabaseManager
    /// </summary>
    /// <param name="databaseDirectory">Directory of the database file.</param>
    /// <exception cref="DirectoryNotFoundException">Exception that is thrown when the directory doesn't exist.</exception>
    public DatabaseSettings(string databaseDirectory)
    {
        if (!Directory.Exists(databaseDirectory)) 
            throw new DirectoryNotFoundException($"The directory: \'{databaseDirectory}\' wasn't found.");
        DatabaseDirectory = databaseDirectory;
    }
}