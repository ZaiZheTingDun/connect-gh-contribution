namespace CGC;

public class GithubClient
{
    private readonly HttpClient _httpClient = new();

    public GithubClient()
    {
        _httpClient.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
    }
    
    public async Task<string> FetchGitHubProfilePage(string username)
    {
        try
        {
            var url = $"https://github.com/{username}?tab=contributions";
            return await _httpClient.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching profile page: {ex.Message}");
            throw;
        }
    }

    public async Task<string> FetchGitHubContributionsPage(string href)
    {
        try
        {
            return await _httpClient.GetStringAsync(href);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching contribution page: {ex.Message}");
            throw;
        }
    }
}