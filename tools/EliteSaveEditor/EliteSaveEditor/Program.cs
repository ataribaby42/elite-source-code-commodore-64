using EliteSaveEditor;

try
{
    return new EditorApplication().Run(args);
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Fatal error: {exception.Message}");
    return 1;
}
