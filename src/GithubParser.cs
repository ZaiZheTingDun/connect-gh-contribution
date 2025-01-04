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

        var contribNode = doc.DocumentNode.SelectSingleNode("//div[@class='js-yearly-contributions']//h2");
        var contribMatch = contribNode?.InnerText.Trim().Split(' ').FirstOrDefault();
        if (contribMatch == null)
        {
            return null;
        }

        var dayNodes = doc.DocumentNode.SelectNodes("//td[contains(@class, 'ContributionCalendar-day')]");
        if (dayNodes == null)
        {
            return null;
        }

        var contributions = GetFlatContributions(dayNodes, doc.DocumentNode);

        return new YearContributionData
        {
            Year = year,
            Contributions = contributions
        };
    }

    private static List<ContributionValue> GetFlatContributions(
        HtmlNodeCollection dayNodes,
        HtmlNode pageNode) =>
        dayNodes
            .Select(dayNode => ParseDay(dayNode, pageNode))
            .Where(value => value.Count != 0)
            .OrderBy(value => value.Date)
            .ToList();

    private static ContributionValue ParseDay(HtmlNode dayNode, HtmlNode pageNode)
    {
        var dateString = dayNode.GetAttributeValue("data-date", "");
        
        var tooltipNode = pageNode.SelectSingleNode($"//tool-tip[contains(@for, '{dayNode.Id}')]");
        var countString = tooltipNode.InnerText.Trim().Split(' ').FirstOrDefault();

        var count = countString != null
            ? int.TryParse(countString, out var c) ? c : 0
            : 0;

        return new ContributionValue
        {
            Date = dateString,
            Count = count
        };
    }
}