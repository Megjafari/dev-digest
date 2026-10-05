# dev-digest

![Tokens per run](https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2FMegjafari%2Fdev-digest%2Fmain%2Fdigests%2Fbadge.json)

![ci](https://github.com/Megjafari/dev-digest/actions/workflows/ci.yml/badge.svg)

A .NET console app that runs every morning on GitHub Actions. It collects fresh headlines and job ads, has Claude summarize each topic in a few sentences, and posts the result to Discord. It never sends the same link twice.

## What you get each morning

Four sections, each with a short summary followed by the links it is based on:

* **War and conflicts**: Iran, USA, Israel and Palestine, from sources with different perspectives, filtered by keywords
* **DevOps and software development**: .NET, Azure, Docker, Kubernetes, PostgreSQL, GitHub and general dev news
* **AI worldwide**: research, products and commentary
* **New job ads**: from Arbetsförmedlingen's open JobTech API, searched for junior .NET, cloud and DevOps roles in Gothenburg

Illustrative shape of one section (not real output):

```
### AI worldwide

Four to six sentences summarizing the new items in this section...

**Links**
* [Some headline](https://example.com/article) (Source name)
* [Another headline](https://example.com/other) (Another source)
```

## How it works

```
RSS feeds + JobTech API
        |
   FeedReader / JobSearch      fetch, parse, keyword filter
        |
   SeenStore                   drop links that were already sent
        |
   Summarizer (Claude API)     one summary per section
        |
   Digest                      markdown: summary + links
        |
   digests/YYYY-MM-DD.md  +  Discord webhook
```

| File | Responsibility |
|---|---|
| `Program.cs` | Orchestration, about 40 lines |
| `Config.cs` | Sources, keywords, per section instructions for the summarizer |
| `FeedReader.cs` | RSS and Atom parsing, keyword filtering |
| `JobSearch.cs` | JobTech API client |
| `SeenStore.cs` | Remembers sent links in `digests/seen.json`, forgets them after 120 days |
| `Summarizer.cs` | Claude API call (Haiku 4.5) |
| `Digest.cs` | Renders one section as markdown |
| `Discord.cs` | Splits the digest on line breaks and posts it in several messages |

### About the commits

The workflow commits `digests/YYYY-MM-DD.md` and `digests/seen.json` every day. The markdown files are the archive of what was sent, and `seen.json` is the state that prevents duplicates between runs, so the repository is the app's storage. That is why most of the history consists of `digest:` commits.

## Setup

1. Fork or clone the repo.
2. Create a Discord webhook (channel settings, Integrations, Webhooks).
3. Create an Anthropic API key in the Claude Console. Choose a workspace when you create the key, otherwise the API asks for an `anthropic-workspace-id` header on every request.
4. Add these under Settings, Secrets and variables, Actions:
   * Secret `DISCORD_WEBHOOK`
   * Secret `ANTHROPIC_API_KEY`
   * Variable `COMMIT_EMAIL`, your GitHub noreply address (a commit only counts as yours if the email belongs to your account)
5. Under Settings, Actions, General, set Workflow permissions to "Read and write permissions".
6. Run the `daily-digest` workflow once from the Actions tab. After that it runs on its own.

The first run sends everything that is current. After that you only get what is new.

## Run locally

```bash
export ANTHROPIC_API_KEY="..."
export DISCORD_WEBHOOK="..."        # optional
DRY_RUN=1 dotnet run --project src/DevDigest
```

`DRY_RUN=1` writes to `digests-preview/` and leaves `seen.json` untouched, so local testing never affects what the scheduled run sends. Without `ANTHROPIC_API_KEY` the digest is produced without summaries. Without `DISCORD_WEBHOOK` nothing is posted.

## Tests

```bash
dotnet test tests/DevDigest.Tests
```

The tests cover the logic that is easiest to get wrong: deduplication in `SeenStore`, message splitting in `Discord`, and keyword filtering in `FeedReader`. They run in CI on every push that touches `src/` or `tests/`.

## Cost

Roughly one cent per run on Claude Haiku 4.5, which is a few kronor per month. GitHub Actions is free for public repositories, and the RSS feeds, JobTech and Discord webhooks are free.

## Known limitations

* A source that fails (for example a 403 from a site that blocks bots) is logged and skipped, with no retries.
* Claude only sees headlines and short snippets from the feeds, not the full articles, so summaries are overviews and not deep reads. The prompt tells it to use only what is in the list and to stay neutral on conflict news, but the links are there for a reason.
* Scheduled workflows on GitHub can start 5 to 30 minutes late, and the cron time is in UTC, so it shifts by an hour when daylight saving time changes.
* The summarizer is a concrete class, so the section rendering is not covered by unit tests yet. Extracting an interface would fix that.

## Ideas

* Fetch the full text of the top items before summarizing
* Retry with backoff for flaky sources
* Rank items instead of taking the newest few per source
* Extract an `ISummarizer` interface and test `Digest`