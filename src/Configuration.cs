using System.Text.Json;

namespace CGC;

public class Configuration
{
    public string Username { get; init; } = null!;
    public string RepositoryUrl { get; init; } = null!;

    public static Configuration Load()
    {
        const string configPath = "config.json";

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException(
                "Configuration file not found. Please create a config.json file in the application directory.");
        }

        var jsonString = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<Configuration>(jsonString)
            ?? throw new InvalidOperationException("Failed to parse configuration file");

        return config;
    }
}