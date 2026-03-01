using CGC;

var config = Configuration.Load();

var gitRepository = new GitRepository(config.RepositoryUrl, config.Username);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Fetching your contributions...");
Console.ResetColor();

var githubClient = new GithubClient(config.GithubToken);
var dataOfYears = await githubClient.FetchContributionDataByYear(config.Username);

await gitRepository.InitializeRepository();

// // Calculate total commits needed
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

        // Count existing commits for this date
        var currentCommitCount = await gitRepository.GetExistingCommitCount(date);
        var commitsNeeded = Math.Max(0, targetCommits - currentCommitCount);

        currentProgress += currentCommitCount;

        // Create missing commits
        for (var i = 0; i < commitsNeeded; i++)
        {
            // Create or update the commit file
            await gitRepository.CreateCommitOnSpecifiedDate(date, $"commit on {date} {i}");

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
Console.WriteLine("Rebasing and Pushing...");
await gitRepository.RebaseAndPushMain();
await gitRepository.PushNewBranch();
Console.WriteLine("Finish!");