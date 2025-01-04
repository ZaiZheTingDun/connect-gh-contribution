namespace CGC.types;

public class YearContributionData
{
    public int Total { get; init; }
    public string Year { get; init; } = null!;
    public List<ContributionValue> Contributions { get; init; } = null!;
}