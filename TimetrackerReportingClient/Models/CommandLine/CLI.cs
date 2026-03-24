using CommandLine;

namespace TimetrackerReportingClient.Models.CommandLine;

public class CLI
{
    public void SetupCLI(string[] args) {
        CommandLineOptions cmd = null;
        cmd = ParseCmdCommands(args, cmd!);
    }

    private static CommandLineOptions ParseCmdCommands(string[] args, CommandLineOptions cmd)
    {
        Parser
            .Default.ParseArguments<CommandLineOptions>(args)
            .WithParsed(x => cmd = x)
            .WithNotParsed(_ =>
            {
                Console.WriteLine("See --help for usage.");
                Environment.Exit(1);
            });
        return cmd;
    }
}
