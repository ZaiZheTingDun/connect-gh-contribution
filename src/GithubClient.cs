using System.Text;
using System.Text.Json;
using CGC.types;

namespace CGC;

public class GithubClient
{
    private readonly HttpClient _httpClient = new();
    private const string GraphqlEndpoint = "https://api.github.com/graphql";

    public GithubClient(string githubToken)
    {
        if (string.IsNullOrWhiteSpace(githubToken))
        {
            throw new ArgumentException("GitHub token is required.", nameof(githubToken));
        }

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {githubToken}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "connect-gh-contribution");
    }

    public async Task<List<YearContributionData>> FetchContributionDataByYear(string username)
    {
        var years = await FetchContributionYears(username);
        if (years.Count == 0)
        {
            return new List<YearContributionData>();
        }

        var query = @"
query($login: String!, $from: DateTime!, $to: DateTime!) {
  user(login: $login) {
    contributionsCollection(from: $from, to: $to) {
      contributionCalendar {
        weeks {
          contributionDays {
            date
            contributionCount
          }
        }
      }
    }
  }
}";

      var currentYear = DateTime.UtcNow.Year;
      var result = new List<YearContributionData>();

      foreach (var year in years.OrderBy(value => value))
      {
        var fromUtc = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = year == currentYear
          ? DateTime.UtcNow
          : new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        using var response = await SendGraphqlRequest(query, new
        {
          login = username,
          from = fromUtc.ToString("O"),
          to = toUtc.ToString("O")
        });

        var contributionDays = response.RootElement
          .GetProperty("data")
          .GetProperty("user")
          .GetProperty("contributionsCollection")
          .GetProperty("contributionCalendar")
          .GetProperty("weeks")
          .EnumerateArray()
          .SelectMany(week => week.GetProperty("contributionDays").EnumerateArray())
          .Select(day => new ContributionValue
          {
            Date = day.GetProperty("date").GetString() ?? string.Empty,
            Count = day.GetProperty("contributionCount").GetInt32()
          })
          .Where(day => day.Count > 0 && day.Date.StartsWith($"{year}-", StringComparison.Ordinal))
          .GroupBy(day => day.Date)
          .Select(group => new ContributionValue
          {
            Date = group.Key,
            Count = group.Max(day => day.Count)
          })
          .OrderBy(day => day.Date)
          .ToList();

        result.Add(new YearContributionData
        {
          Year = year.ToString(),
          Contributions = contributionDays
        });
      }

      return result;
    }

    private async Task<List<int>> FetchContributionYears(string username)
    {
        var query = @"
query($login: String!) {
  user(login: $login) {
    contributionsCollection {
      contributionYears
    }
  }
}";

        using var response = await SendGraphqlRequest(query, new { login = username });

        return response.RootElement
            .GetProperty("data")
            .GetProperty("user")
            .GetProperty("contributionsCollection")
            .GetProperty("contributionYears")
            .EnumerateArray()
            .Select(year => year.GetInt32())
            .ToList();
    }

    private async Task<JsonDocument> SendGraphqlRequest(string query, object variables)
    {
        var payload = JsonSerializer.Serialize(new { query, variables });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(GraphqlEndpoint, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"GraphQL request failed with status {(int)response.StatusCode}: {responseBody}");
        }

        var jsonDoc = JsonDocument.Parse(responseBody);
        if (jsonDoc.RootElement.TryGetProperty("errors", out var errors))
        {
            var messages = errors.EnumerateArray()
                .Select(error => error.GetProperty("message").GetString())
                .Where(message => !string.IsNullOrWhiteSpace(message));
            throw new InvalidOperationException($"GraphQL returned errors: {string.Join("; ", messages!)}");
        }

        return jsonDoc;
    }
}