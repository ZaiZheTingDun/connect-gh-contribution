using CGC;

var config = Configuration.Load();
var branchName = $"cgc-{config.Username}";

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Fetching your contributions...");
Console.ResetColor();

var githubClient = new GithubClient(config.GithubToken);
var dataOfYears = await githubClient.FetchContributionDataByYear(config.Username);

var repoUrl = config.RepositoryUrl;
var tmpPath = Path.Combine(Path.GetTempPath(), "cgc-tmp");

if (Directory.Exists(tmpPath))
{
    Directory.Delete(tmpPath, true);
}

Directory.CreateDirectory(tmpPath);
await GitCommand.Execute(tmpPath, $"clone {repoUrl} .");

// Create or checkout to the target branch
try
{
    await GitCommand.Execute(tmpPath, $"checkout -b {branchName}");
}
catch (Exception e)
{
    if (e.Message.Contains("already exists"))
    {
        await GitCommand.Execute(tmpPath, $"checkout {branchName}");
    }
    else
    {
        Console.WriteLine(e);
        throw;
    }
}

// Calculate total commits needed
var totalCommits = dataOfYears.Sum(year =>
    year.Contributions.Sum(contribution => contribution.Count));

Console.WriteLine($"Total commits to create: {totalCommits}");
var progressBar = new ProgressBar(totalCommits);
var currentProgress = 0;

foreach (var yearData in dataOfYears)
{
    foreach (var contribution in yearData.Contributions)
    {
        var date = contribution.Date;
        var targetCommits = contribution.Count;
        int commitsNeeded;

        // Count existing commits for this date
        try
        {
            var existingCommits = await GitCommand.Execute(tmpPath,
                $"rev-list --count --grep=\"^commit {date}$\" HEAD");
            var currentCommitCount = int.Parse(existingCommits.Trim());
            commitsNeeded = Math.Max(0, targetCommits - currentCommitCount);
        }
        catch
        {
            // No commits
            commitsNeeded = targetCommits;
        }

        // Create missing commits
        for (var i = 0; i < commitsNeeded; i++)
        {
            // Create or update the commit file
            var commitFile = Path.Combine(tmpPath, $"commit-{config.Username}.txt");
            await File.WriteAllTextAsync(commitFile, $"commit on {date} {i}");

            // Stage, commit and push with the specific date
            await GitCommand.Execute(tmpPath, "add .");

            // Set both author and committer dates (explicit UTC)
            var commitDate = $"{date}T12:00:00+0000";
            var envVars = new Dictionary<string, string>
            {
                ["GIT_COMMITTER_DATE"] = commitDate,
                ["GIT_AUTHOR_DATE"] = commitDate
            };

            await GitCommand.Execute(tmpPath, $"commit --date=\"{commitDate}\" -m \"commit {date}\"",
                envVars);

            currentProgress++;
            progressBar.Update(currentProgress);
        }
    }
}

// Add a newline after progress bar is complete
Console.ResetColor();
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Green;

// Push current branch to GitHub
Console.WriteLine($"Pushing {branchName} to GitHub...");
await GitCommand.Execute(tmpPath, $"push --set-upstream --force origin cgc-{config.Username}");
Console.WriteLine("Push Successfully!");

// Rebase to main
Console.WriteLine("Rebasing...");
await GitCommand.Execute(tmpPath, "checkout main");
await GitCommand.Execute(tmpPath, $"rebase {branchName}");
Console.WriteLine("Rebase Successfully!");

// Push main to GitHub
Console.WriteLine("Pushing main to Github...");
await GitCommand.Execute(tmpPath, "push");
Console.WriteLine("Push Successfully!");