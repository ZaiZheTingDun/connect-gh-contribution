namespace CGC;

public class GitRepository(string repoUrl, string username)
{
    private readonly string _tmpPath = Path.Combine(Path.GetTempPath(), "cgc-tmp");
    private readonly string _branchName = $"cgc-{username}";

    private string _defaultBranch = "";
    private bool _isEmpty;

    private async Task CloneRepository()
    {
        if (Directory.Exists(_tmpPath))
        {
            Directory.Delete(_tmpPath, true);
        }

        Directory.CreateDirectory(_tmpPath);
        await GitCommand.Execute(_tmpPath, $"clone {repoUrl} .");
    }

    public async Task InitializeRepository()
    {
        await CloneRepository();

        _defaultBranch = (await GitCommand.Execute(_tmpPath, "branch --show-current")).Trim();

        var status = await GitCommand.Execute(_tmpPath, "status");
        _isEmpty = status.Contains("No commits yet");

        // Create or checkout to the target branch
        try
        {
            await GitCommand.Execute(_tmpPath, $"checkout -b {_branchName}");
        }
        catch (Exception e)
        {
            if (e.Message.Contains("already exists"))
            {
                await GitCommand.Execute(_tmpPath, $"checkout {_branchName}");
            }
            else
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // Rebase the branch to get latest commits from default branch (if not empty)
        if (!_isEmpty)
        {
            await GitCommand.Execute(_tmpPath, $"rebase {_defaultBranch}");
        }
    }

    public async Task<int> GetExistingCommitCount(string date)
    {
        try
        {
            var existingCommits = await GitCommand.Execute(_tmpPath,
                $"rev-list --count --grep=\"^commit {date}$\" HEAD");
            return int.Parse(existingCommits.Trim());
        }
        catch
        {
            // No commits
            return 0;
        }
    }

    public async Task CreateCommitOnSpecifiedDate(string date, string comment)
    {
        // Create or update the commit file
        var commitFile = Path.Combine(_tmpPath, $"commit-{username}.txt");
        await File.WriteAllTextAsync(commitFile, comment);

        // Stage, commit and push with the specific date
        await GitCommand.Execute(_tmpPath, "add .");

        // Set both author and committer dates (explicit UTC)
        var commitDate = $"{date}T12:00:00+0000";
        var envVars = new Dictionary<string, string>
        {
            ["GIT_COMMITTER_DATE"] = commitDate,
            ["GIT_AUTHOR_DATE"] = commitDate
        };

        await GitCommand.Execute(_tmpPath, $"commit --date=\"{commitDate}\" -m \"commit {date}\"",
            envVars);
    }

    public async Task RebaseAndPushMain()
    {
        await GitCommand.Execute(_tmpPath, $"checkout {(_isEmpty ? " -b" : "")} {_defaultBranch}");
        await GitCommand.Execute(_tmpPath, $"rebase {_branchName}");
        await GitCommand.Execute(_tmpPath, $"push --set-upstream --force origin {_defaultBranch}");
    }

    public async Task PushNewBranch()
    {
        await GitCommand.Execute(_tmpPath, $"push --set-upstream --force origin {_branchName}");
    }
}