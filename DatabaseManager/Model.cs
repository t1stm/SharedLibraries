namespace DatabaseManager;

public abstract class Model<T>
{
    /// <summary>
    /// Action that is set by the DatabaseManager.
    /// Typically it's only meant to be called, so please don't replace it.
    /// </summary>
    public Action? SetModified;
    
    /// <summary>
    /// This is a method that takes the source database and searches the current object in it. 
    /// </summary>
    /// <param name="source">The source database.</param>
    /// <returns>Refrence to the current type.</returns>
    public abstract T? SearchFrom(IEnumerable<T> source);
    
    /// <summary>
    /// Method that's called when the database is loaded.
    /// </summary>
    public virtual void OnLoaded()
    {
        // To be overridden.
    }
}