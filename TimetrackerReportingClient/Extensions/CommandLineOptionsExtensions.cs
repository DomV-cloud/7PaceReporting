using CommandLine;
using TimetrackerReportingClient.Models.CommandLine;

namespace TimetrackerReportingClient.Extensions;

public static class CommandLineOptionsExtensions
{
    public static CommandLineOptions ParseCmdCommands(
        this CommandLineOptions commandLineOptions,
        string[] args
    )
    {
        Parser
            .Default.ParseArguments<CommandLineOptions>(args)
            .WithParsed(x => commandLineOptions = x)
            .WithNotParsed(_ =>
            {
                Console.WriteLine("See --help for usage.");
                Environment.Exit(1);
            });

        return commandLineOptions;
    }

    public static CommandLineOptions InitCLI(
        this CommandLineOptions commandLineOptions,
        string[] args
    )
    {
        commandLineOptions = commandLineOptions.ParseCmdCommands(args);
        return commandLineOptions;
    }
}
