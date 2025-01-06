# connect-gh-contribution

A tool to aggregate your GitHub contribution graph from multiple accounts into a single GitHub profile. This is useful if you have more than one GitHub account and want to combine all your contributions into one account, displaying a unified contribution graph on your profile page.

## Prerequisites

- .NET 8.0 SDK
- Git installed and configured
- GitHub account
- Write access to the target repository
- Make sure you are [showing your private contributions on your profile](https://docs.github.com/en/account-and-profile/setting-up-and-managing-your-github-profile/managing-contribution-settings-on-your-profile/showing-your-private-contributions-and-achievements-on-your-profile#changing-the-visibility-of-your-private-contributions)

## Configuration

1. Create a `config.json` file in the project root (you can copy from `config.json.example`)
2. Configure the following settings:
```json
{
"Username": "your github username",
"RepositoryUrl": "git@github.com:<username>/<repository>.git"
}
```

- `Username`: Your GitHub username
- `RepositoryUrl`: The SSH URL of the target repository where contributions will be recreated. It’s recommended to create a new repository specifically for hosting the recreated commits to avoid conflicts or clutter in existing repositories.

## Setup

1. Clone the repository:
```bash
git clone <repository-url>
cd connect-gh-contribution
```
2. Create and configure your `config.json` file
3. Build the project:
```bash
dotnet build
```
4. Run the application:
```bash
dotnet run
```

## How It Works

1. The tool fetches your GitHub contribution history from your profile
2. It analyzes the contribution patterns for each day
3. Creates a temporary local repository
4. Recreates commits with the correct dates to match your contribution pattern
5. Pushes the changes to your target repository

## Notes

- The tool will create a new branch called `cgc` in the target repository
- Existing commits in the target repository will be preserved
- The contribution recreation process may take some time depending on your contribution history

## License

MIT
