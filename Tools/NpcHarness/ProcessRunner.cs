using System.Diagnostics;
using System.Text;

namespace NpcHarness;

internal sealed class ProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        string? standardInput,
        string standardOutputPath,
        string standardErrorPath,
        TimeSpan timeout)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            RedirectStandardInput = standardInput != null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Environment.CurrentDirectory,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return await RunAsync(startInfo, standardInput, standardOutputPath, standardErrorPath, timeout);
    }

    public async Task<ProcessResult> RunAsync(
        string executablePath,
        string rawArguments,
        string? standardInput,
        string standardOutputPath,
        string standardErrorPath,
        TimeSpan timeout)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = rawArguments,
            UseShellExecute = false,
            RedirectStandardInput = standardInput != null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Environment.CurrentDirectory,
        };

        return await RunAsync(startInfo, standardInput, standardOutputPath, standardErrorPath, timeout);
    }

    private static async Task<ProcessResult> RunAsync(
        ProcessStartInfo startInfo,
        string? standardInput,
        string standardOutputPath,
        string standardErrorPath,
        TimeSpan timeout)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(standardOutputPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(standardErrorPath)!);

        using Process process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Could not start process: {startInfo.FileName}");
        }

        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

        if (standardInput != null)
        {
            await process.StandardInput.WriteAsync(standardInput);
            process.StandardInput.Close();
        }

        bool timedOut = false;
        using CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = true;
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process completed between the timeout and the kill attempt.
            }

            await process.WaitForExitAsync();
        }

        string standardOutput = await standardOutputTask;
        string standardError = await standardErrorTask;
        await File.WriteAllTextAsync(standardOutputPath, standardOutput, new UTF8Encoding(false));
        await File.WriteAllTextAsync(standardErrorPath, standardError, new UTF8Encoding(false));

        return new ProcessResult(process.ExitCode, timedOut);
    }
}

internal readonly record struct ProcessResult(int ExitCode, bool TimedOut);
