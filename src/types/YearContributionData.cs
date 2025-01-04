namespace CGC.types;

public class YearContributionData
{
    public string Year { get; init; } = null!;
    public List<ContributionValue> Contributions { get; init; } = null!;
}