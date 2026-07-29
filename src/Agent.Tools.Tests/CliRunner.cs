using System.Diagnostics;

namespace Agent.Tools.Tests;

/// <summary>
/// Runs a tool's compiled DLL as a subprocess so tests exercise the real
/// System.CommandLine parsing and output, not internal methods.
/// </summary>
internal static class CliRunner
{
    public static (int ExitCode, string StdOut, string StdErr) Run(string toolDllName, params string[] args)
    {
        var dllPath = Path.Combine(AppContext.BaseDirectory, toolDllName);
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException($"Tool DLL not found next to test output: {dllPath}");
        }

        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(dllPath);
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)!;
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, stdOut, stdErr);
    }
}
