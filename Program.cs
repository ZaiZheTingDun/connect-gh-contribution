using CGC;
using CGC.types;

var githubClient = new GithubClient();
var profilePage = await githubClient.FetchGitHubProfilePage("ZaiZheTingDun");
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

Console.WriteLine(dataOfYears.Count);
