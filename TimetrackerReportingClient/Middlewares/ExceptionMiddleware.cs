namespace TimetrackerReportingClient.Middleware;

public static class ExceptionMiddleware
{
    public static void Run(Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentException ex)
        {
            WriteError($"Configuration error: {ex.Message}");
            Environment.Exit(1);
        }
        catch (InvalidOperationException ex)
        {
            WriteError($"Operation error: {ex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            WriteError($"Unexpected error ({ex.GetType().Name}): {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"[Error] {message}");
        Console.ResetColor();
    }
}
