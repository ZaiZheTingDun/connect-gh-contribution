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

// Clean all commits by deleting .git and reinitializing
Directory.Delete($"{tmpPath}/.git", true);
await GitCommand.Execute(tmpPath, "init");
await GitCommand.Execute(tmpPath, "remote add origin " + repoUrl);
await GitCommand.Execute(tmpPath, "fetch");
await GitCommand.Execute(tmpPath, "checkout -b cgc");

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
        var commitsNeeded = contribution.Count;

        // Create missing commits
        for (var i = 0; i < commitsNeeded; i++)
        {
            // Create or update the commit file
            var commitFile = Path.Combine(tmpPath, "commit.txt");
            await File.WriteAllTextAsync(commitFile, $"commit on {date:yyyy-MM-dd} #{i}");

            // Stage, commit and push with the specific date
            await GitCommand.Execute(tmpPath, "add .");
            await GitCommand.Execute(tmpPath, $"commit --date=\"{date:yyyy-MM-dd} 12:00\" -m \"commit {i}\"");

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
await GitCommand.Execute(tmpPath, "push --set-upstream --force origin cgc");


