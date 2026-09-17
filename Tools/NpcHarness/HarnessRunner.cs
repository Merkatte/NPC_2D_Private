namespace NpcHarness;

internal sealed class HarnessRunner
{
    private const int RestartForCompilationExitCode = 10;
    private const int MaximumAdapterAttempts = 3;

    private readonly string _repositoryRoot;
    private readonly ProcessRunner _processRunner;

    public HarnessRunner(string repositoryRoot, ProcessRunner? processRunner = null)
    {
        _repositoryRoot = repositoryRoot;
        _processRunner = processRunner ?? new ProcessRunner();
    }

    public Task<int> RunAsync(HarnessCommand command)
    {
        return command switch
        {
            VerifyCommand verify => VerifyAsync(verify),
            RunAdapterCommand adapter => RunAdapterAsync(adapter),
            SelfTestCommand => Task.FromResult(HarnessSelfTest.Run(_repositoryRoot)),
            _ => throw new InvalidOperationException($"Unsupported command type: {command.GetType().Name}"),
        };
    }

    private async Task<int> VerifyAsync(VerifyCommand command)
    {
        GateProfile profile = GateProfile.Resolve(command.Profile);
        string runId = RunPaths.ValidateOrCreateRunId(command.RunId);
        string runDirectory = RunPaths.GetRunDirectory(_repositoryRoot, runId);
        string resultPath = RunPaths.ResolveOutput(
            _repositoryRoot,
            command.OutputPath,
            Path.Combine(runDirectory, "gate-results", profile.FileName + ".json"));
        Directory.CreateDirectory(Path.GetDirectoryName(resultPath)!);
        File.Delete(resultPath);

        ProcessResult? processResult = null;
        if (IsUnityProjectOpen())
        {
            if (!profile.SupportsInteractiveEditor)
            {
                Console.Error.WriteLine(
                    $"Infrastructure error: profile {profile.Name} cannot run through the open Editor bridge.");
                return Program.InfrastructureErrorExitCode;
            }

            bool receivedResult = await RequestOpenEditorGateAsync(
                profile,
                runId,
                resultPath,
                command.Timeout);
            if (!receivedResult)
            {
                Console.Error.WriteLine(
                    $"Infrastructure error: open Unity Editor did not return a GateResult within " +
                    $"{command.Timeout.TotalSeconds:0} seconds.");
                return Program.InfrastructureErrorExitCode;
            }
        }
        else
        {
            string unityPath = UnityEditorLocator.Find(_repositoryRoot, command.UnityPath);
            processResult = await RunUnityAsync(
                unityPath,
                new[]
                {
                    "-batchmode", "-nographics",
                    "-projectPath", _repositoryRoot,
                    "-executeMethod", profile.ExecuteMethod,
                    "-harnessRunId", runId,
                    "-harnessResultPath", resultPath,
                    "-logFile", Path.Combine(runDirectory, "logs", profile.FileName + ".unity.log"),
                },
                runDirectory,
                profile.FileName,
                command.Timeout);

            if (processResult.Value.TimedOut)
            {
                Console.Error.WriteLine($"Infrastructure error: Unity timed out after {command.Timeout.TotalSeconds:0} seconds.");
                return Program.InfrastructureErrorExitCode;
            }
        }

        if (!File.Exists(resultPath))
        {
            string execution = processResult == null
                ? "open Unity Editor completed without a GateResult"
                : $"Unity exited with code {processResult.Value.ExitCode} without a GateResult";
            Console.Error.WriteLine($"Infrastructure error: {execution}.");
            return Program.InfrastructureErrorExitCode;
        }

        GateResultSummary result;
        try
        {
            result = HarnessResultContracts.ReadGateResult(resultPath);
            ValidateGateResultForRequest(result, runId, profile);
        }
        catch (Exception exception) when (exception is InvalidDataException or System.Text.Json.JsonException)
        {
            Console.Error.WriteLine($"Infrastructure error: invalid GateResult: {exception.Message}");
            return Program.InfrastructureErrorExitCode;
        }

        try
        {
            if (processResult != null)
            {
                ValidateGateProcessExitCode(processResult.Value.ExitCode, result);
            }
        }
        catch (InvalidDataException exception)
        {
            Console.Error.WriteLine($"Infrastructure error: {exception.Message}");
            return Program.InfrastructureErrorExitCode;
        }

        Console.WriteLine($"{result.Outcome}: {result.Message}");
        Console.WriteLine($"Result: {Path.GetRelativePath(_repositoryRoot, resultPath)}");
        return result.Outcome switch
        {
            GateOutcome.Pass => Program.SuccessExitCode,
            GateOutcome.Fail => Program.CandidateFailureExitCode,
            GateOutcome.InfrastructureError => Program.InfrastructureErrorExitCode,
            _ => Program.InfrastructureErrorExitCode,
        };
    }

    private async Task<bool> RequestOpenEditorGateAsync(
        GateProfile profile,
        string runId,
        string resultPath,
        TimeSpan timeout)
    {
        string requestDirectory = Path.Combine(_repositoryRoot, "Library", "NpcHarness");
        string pendingPath = Path.Combine(requestDirectory, "PendingGateRequest.json");
        string claimedPath = Path.Combine(requestDirectory, "ClaimedGateRequest.json");
        string temporaryPath = Path.Combine(requestDirectory, $"PendingGateRequest.{runId}.tmp");
        ValidateInteractiveResultPath(resultPath);

        Directory.CreateDirectory(requestDirectory);
        if (File.Exists(pendingPath) || File.Exists(claimedPath))
        {
            throw new InvalidOperationException("Another interactive gate request is already pending or running.");
        }

        string requestJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            runId,
            profile = profile.Name,
            resultPath = Path.GetFullPath(resultPath),
        });
        File.WriteAllText(temporaryPath, requestJson);
        File.Move(temporaryPath, pendingPath);

        DateTime deadline = DateTime.UtcNow + timeout;
        try
        {
            while (DateTime.UtcNow < deadline)
            {
                if (File.Exists(resultPath))
                {
                    return true;
                }

                await Task.Delay(200);
            }

            return File.Exists(resultPath);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            if (File.Exists(pendingPath))
            {
                File.Delete(pendingPath);
            }
        }
    }

    private void ValidateInteractiveResultPath(string resultPath)
    {
        string allowedRoot = Path.GetFullPath(Path.Combine(_repositoryRoot, ".harness-runs"));
        string fullResultPath = Path.GetFullPath(resultPath);
        string prefix = allowedRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                        Path.DirectorySeparatorChar;
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!fullResultPath.StartsWith(prefix, comparison))
        {
            throw new InvalidOperationException(
                "Open Editor gate result paths must be inside the project .harness-runs directory.");
        }
    }

    internal static void ValidateGateResultForRequest(
        GateResultSummary result,
        string requestedRunId,
        GateProfile requestedProfile)
    {
        if (!string.Equals(result.RunId, requestedRunId, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"GateResult runId '{result.RunId}' does not match requested runId '{requestedRunId}'.");
        }

        if (!string.Equals(result.Profile, requestedProfile.Name, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"GateResult profile '{result.Profile}' does not match requested profile '{requestedProfile.Name}'.");
        }

        if (result.ProfileVersion != requestedProfile.Version)
        {
            throw new InvalidDataException(
                $"GateResult profileVersion {result.ProfileVersion} does not match requested version {requestedProfile.Version}.");
        }

        if (requestedProfile.RequiresNoChangedFiles && result.ChangedFiles.Count != 0)
        {
            throw new InvalidDataException(
                $"Read-only profile {requestedProfile.Name} reported changed files: " +
                string.Join(", ", result.ChangedFiles));
        }
    }

    internal static void ValidateGateProcessExitCode(int processExitCode, GateResultSummary result)
    {
        int expectedProcessExitCode = result.Outcome == GateOutcome.Pass ? 0 : 1;
        if (processExitCode != expectedProcessExitCode)
        {
            throw new InvalidDataException(
                $"Unity exit code {processExitCode} disagrees with GateResult {result.Outcome}.");
        }
    }

    private async Task<int> RunAdapterAsync(RunAdapterCommand command)
    {
        string jobPath = Path.GetFullPath(command.JobPath, _repositoryRoot);
        HarnessResultContracts.ValidateJob(jobPath);
        string runId = RunPaths.ValidateOrCreateRunId(command.RunId);
        string runDirectory = RunPaths.GetRunDirectory(_repositoryRoot, runId);
        string resultPath = RunPaths.ResolveOutput(
            _repositoryRoot,
            command.OutputPath,
            Path.Combine(runDirectory, "adapter-results", "result.json"));
        string unityPath = UnityEditorLocator.Find(_repositoryRoot, command.UnityPath);

        EnsureUnityProjectIsNotOpen();
        Directory.CreateDirectory(Path.GetDirectoryName(resultPath)!);

        for (int attempt = 1; attempt <= MaximumAdapterAttempts; attempt++)
        {
            File.Delete(resultPath);
            List<string> arguments = new List<string>
            {
                "-batchmode", "-nographics",
                "-projectPath", _repositoryRoot,
                "-executeMethod", "HarnessBatchRunner.RunJobFromCommandLine",
                "-harnessJobPath", jobPath,
                "-harnessResultPath", resultPath,
                "-logFile", Path.Combine(runDirectory, "logs", $"adapter-{attempt}.unity.log"),
            };
            if (command.AllowOverwrite)
            {
                arguments.Add("-harnessAllowOverwrite");
            }

            ProcessResult processResult = await RunUnityAsync(
                unityPath,
                arguments,
                runDirectory,
                $"adapter-{attempt}",
                command.Timeout);
            if (processResult.TimedOut)
            {
                Console.Error.WriteLine($"Infrastructure error: Unity timed out after {command.Timeout.TotalSeconds:0} seconds.");
                return Program.InfrastructureErrorExitCode;
            }

            if (processResult.ExitCode == RestartForCompilationExitCode)
            {
                Console.WriteLine($"Adapter checkpoint requested a compilation restart ({attempt}/{MaximumAdapterAttempts}).");
                continue;
            }

            if (!File.Exists(resultPath))
            {
                Console.Error.WriteLine(
                    $"Infrastructure error: Unity exited with code {processResult.ExitCode} without an adapter result.");
                return Program.InfrastructureErrorExitCode;
            }

            AdapterResultSummary result;
            try
            {
                result = HarnessResultContracts.ReadAdapterResult(resultPath);
            }
            catch (Exception exception) when (exception is InvalidDataException or System.Text.Json.JsonException)
            {
                Console.Error.WriteLine($"Infrastructure error: invalid adapter result: {exception.Message}");
                return Program.InfrastructureErrorExitCode;
            }

            int expectedProcessExitCode = result.Success ? 0 : 1;
            if (processResult.ExitCode != expectedProcessExitCode)
            {
                Console.Error.WriteLine(
                    $"Infrastructure error: Unity exit code {processResult.ExitCode} disagrees with adapter state {result.State}.");
                return Program.InfrastructureErrorExitCode;
            }

            Console.WriteLine($"{result.State}: {result.Message}");
            Console.WriteLine($"Result: {Path.GetRelativePath(_repositoryRoot, resultPath)}");
            return result.Success ? Program.SuccessExitCode : Program.CandidateFailureExitCode;
        }

        Console.Error.WriteLine("Infrastructure error: adapter exceeded the compilation restart limit.");
        return Program.InfrastructureErrorExitCode;
    }

    private Task<ProcessResult> RunUnityAsync(
        string unityPath,
        IReadOnlyList<string> arguments,
        string runDirectory,
        string logPrefix,
        TimeSpan timeout)
    {
        return _processRunner.RunAsync(
            unityPath,
            arguments,
            standardInput: null,
            Path.Combine(runDirectory, "logs", logPrefix + ".stdout.log"),
            Path.Combine(runDirectory, "logs", logPrefix + ".stderr.log"),
            timeout);
    }

    private bool IsUnityProjectOpen()
    {
        string lockPath = Path.Combine(_repositoryRoot, "Temp", "UnityLockfile");
        if (!File.Exists(lockPath))
        {
            return false;
        }

        try
        {
            using FileStream stream = File.Open(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    private void EnsureUnityProjectIsNotOpen()
    {
        if (IsUnityProjectOpen())
        {
            throw new InvalidOperationException("Close the Unity Editor for this project before running the batch harness.");
        }
    }
}

internal sealed record GateProfile(
    string Name,
    int Version,
    string ExecuteMethod,
    string FileName,
    bool RequiresNoChangedFiles,
    bool SupportsInteractiveEditor)
{
    public static GateProfile Resolve(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "beacon-structure" or "harnessbeacon.structure" => new GateProfile(
                "HarnessBeacon.Structure",
                1,
                "HarnessBeaconGateRunner.VerifyStructureFromCommandLine",
                "harness-beacon-structure",
                RequiresNoChangedFiles: true,
                SupportsInteractiveEditor: true),
            "beacon-playmode" or "harnessbeacon.playmode" => new GateProfile(
                "HarnessBeacon.PlayMode",
                1,
                "HarnessPlayModeVerifier.VerifyFromCommandLine",
                "harness-beacon-playmode",
                RequiresNoChangedFiles: true,
                SupportsInteractiveEditor: false),
            "square-character-structure" or "harnesstest.squarecharacter.structure" => new GateProfile(
                "HarnessTest.SquareCharacter.Structure",
                1,
                "SquareCharacterGateRunner.VerifyStructureFromCommandLine",
                "square-character-structure",
                RequiresNoChangedFiles: true,
                SupportsInteractiveEditor: true),
            _ => throw new ArgumentException(
                $"Unknown gate profile: {value}. Expected beacon-structure, beacon-playmode, " +
                "or square-character-structure."),
        };
    }
}

internal static class RunPaths
{
    public static string ValidateOrCreateRunId(string? value)
    {
        string runId = string.IsNullOrWhiteSpace(value)
            ? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")
            : value;
        if (runId.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character != '-' && character != '_'))
        {
            throw new ArgumentException("Run ID may contain only ASCII letters, digits, '-' and '_'.");
        }

        return runId;
    }

    public static string GetRunDirectory(string repositoryRoot, string runId)
    {
        return Path.Combine(repositoryRoot, ".harness-runs", runId);
    }

    public static string ResolveOutput(string repositoryRoot, string? requestedPath, string defaultPath)
    {
        return string.IsNullOrWhiteSpace(requestedPath)
            ? defaultPath
            : Path.GetFullPath(requestedPath, repositoryRoot);
    }
}
