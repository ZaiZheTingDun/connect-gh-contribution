using CGC;
using CGC.types;

var config = Configuration.Load();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Fetching your contributions...");
Console.ResetColor();

var githubClient = new GithubClient();
var profilePage = await githubClient.FetchGitHubProfilePage(config.Username);
var githubYears = GithubParser.GetYears(profilePage);

var dataOfYears = new List<YearContributionData>();
foreach (var githubYear in githubYears)
{
    var contributionsPage = await githubClient.FetchGitHubContributionsPage(githubYear.Href);
    var dataOfYear = GithubParser.GetDataOfYear(contributionsPage, githubYear.Year);
    if (dataOfYear != null)
    {
        dataOfYears.Add(dataOfYear);
    }
}

dataOfYears = dataOfYears.OrderBy(date => date.Year).ToList();

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
    await GitCommand.Execute(tmpPath, $"checkout -b cgc-{config.Username}");
}
catch (Exception e)
{
    if (e.Message.Contains("already exists"))
    {
        await GitCommand.Execute(tmpPath, $"checkout cgc-{config.Username}");
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
        var date = DateTime.Parse(contribution.Date);
        var targetCommits = contribution.Count;
        int commitsNeeded;

        // Count existing commits for this date
        try
        {
            var existingCommits = await GitCommand.Execute(tmpPath,
                $"rev-list --count --after=\"{date:yyyy-MM-dd} 00:00\" --before=\"{date:yyyy-MM-dd} 23:59:59\" HEAD");
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
            var commitFile = Path.Combine(tmpPath, "commit.txt");
            await File.WriteAllTextAsync(commitFile, $"commit on {date:yyyy-MM-dd} {i}");

            // Stage, commit and push with the specific date
            await GitCommand.Execute(tmpPath, "add .");

            // Set both author and committer dates
            var commitDate = $"{date:yyyy-MM-dd} 12:00";
            var envVars = new Dictionary<string, string>
            {
                ["GIT_COMMITTER_DATE"] = commitDate,
                ["GIT_AUTHOR_DATE"] = commitDate
            };

            await GitCommand.Execute(tmpPath, $"commit --date=\"{commitDate}\" -m \"commit {date:yyyy-MM-dd}\"", envVars);

            currentProgress++;
            progressBar.Update(currentProgress);
        }
    }
}

// Add a newline after progress bar is complete
Console.ResetColor();
Console.WriteLine();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Pushing to GitHub...");
Console.ResetColor();
// Push changes
await GitCommand.Execute(tmpPath, $"push --set-upstream --force origin cgc-{config.Username}");
