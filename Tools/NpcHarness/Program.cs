using System.Text;

namespace NpcHarness;

internal static class Program
{
    public const int SuccessExitCode = 0;
    public const int CandidateFailureExitCode = 1;
    public const int InfrastructureErrorExitCode = 2;
    public const int UsageErrorExitCode = 64;

    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (!HarnessCommandLine.TryParse(args, out HarnessCommand? command, out string error))
        {
            Console.Error.WriteLine(error);
            HarnessCommandLine.PrintUsage(Console.Error);
            return UsageErrorExitCode;
        }

        try
        {
            string repositoryRoot = RepositoryLocator.FindRoot(Environment.CurrentDirectory);
            return await new HarnessRunner(repositoryRoot).RunAsync(command!);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine($"Usage error: {exception.Message}");
            return UsageErrorExitCode;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Infrastructure error: {exception.Message}");
            return InfrastructureErrorExitCode;
        }
    }
}
