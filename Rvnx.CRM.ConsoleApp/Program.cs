using Rvnx.CRM.ConsoleApp;

try
{
    Console.WriteLine($"Rvnx.CRM Console started at {DateTime.Now}");

    if (args.Length > 0)
    {
        int result = await TaskManager.ProcessAsync(args);
        Environment.ExitCode = result;
    }
    else
    {
        Console.WriteLine("No arguments provided.");
        Environment.ExitCode = 1;
    }
}
catch (Exception ex)
{
    Console.WriteLine("An error has occurred:");
    Console.Error.WriteLine($"\t{ex.Message}");
    Environment.ExitCode = 1;
}
finally
{
    Console.WriteLine($"Rvnx.CRM Console ended at {DateTime.Now}");
}