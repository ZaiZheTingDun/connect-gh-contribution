using CGC.types;
using HtmlAgilityPack;

namespace CGC;

public static class GithubParser
{
    public static List<GithubYear> GetYears(string profilePage)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(profilePage);

        var yearLinks = new List<GithubYear>();

        var nodes = doc.DocumentNode.SelectNodes(
            "//a[contains(@class, 'js-year-link') and contains(@class, 'filter-item')]");
        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                var href = node.GetAttributeValue("href", string.Empty);
                if (!string.IsNullOrEmpty(href))
                {
                    var formattedHref = $"https://github.com{href}";
                    var uriBuilder = new UriBuilder(formattedHref);
                    var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                    query["tab"] = "contributions";
                    uriBuilder.Query = query.ToString();

                    yearLinks.Add(new GithubYear
                    {
                        Href = uriBuilder.Uri.ToString(),
                        Year = node.InnerText.Trim()
                    });
                }
            }
        }

        return yearLinks;
    }

    public static YearContributionData? GetDataOfYear(string contributionPage, string year)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(contributionPage);

        // Get total contributions
        var contribNode = doc.DocumentNode.SelectSingleNode("//div[@class='js-yearly-contributions']//h2");
        var contribMatch = contribNode?.InnerText.Trim().Split(' ').FirstOrDefault();
        if (contribMatch == null)
        {
            return null;
        }

        var contribCount = string.IsNullOrEmpty(contribMatch)
            ? int.Parse(contribMatch.Replace(",", ""))
            : 0;

        // Get contribution days
        var dayNodes = doc.DocumentNode.SelectNodes("//td[contains(@class, 'ContributionCalendar-day')]");
        if (dayNodes == null) return null;

        var contributions = GetFlatContributions(dayNodes, doc.DocumentNode);

        return new YearContributionData
        {
            Year = year,
            Total = contribCount,
            Contributions = contributions
        };
    }

    private static List<ContributionValue> GetFlatContributions(
        HtmlNodeCollection dayNodes,
        HtmlNode pageNode) =>
        dayNodes
            .Select(dayNode => ParseDay(dayNode, pageNode).Value)
            .Where(value => value.Count != 0)
            .ToList();

    private static (int[] Date, ContributionValue Value) ParseDay(HtmlNode dayNode, HtmlNode pageNode)
    {
        var dateParts = dayNode.GetAttributeValue("data-date", "")
            .Split('-')
            .Select(int.Parse)
            .ToArray();

        var tooltipNode = pageNode.SelectSingleNode($"//tool-tip[contains(@for, '{dayNode.Id}')]");
        var contributionCount = tooltipNode.InnerText.Trim().Split(' ').FirstOrDefault();

        var value = new ContributionValue
        {
            Date = dayNode.GetAttributeValue("data-date", ""),
            Count = contributionCount != null
                ? int.TryParse(contributionCount, out var count) ? count : 0
                : 0
        };

        return (dateParts, value);
    }
}