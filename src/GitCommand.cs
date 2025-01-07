namespace CGC;

public static class GitCommand
{
    public static async Task<string> Execute(string workingDirectory, string arguments, Dictionary<string, string>? envVars = null)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        if (envVars != null)
        {
            foreach (var (key, value) in envVars)
            {
                process.StartInfo.Environment[key] = value;
            }
        }
        
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"Git command {arguments} failed: {error}");
        }
        
        return output;
    }
}