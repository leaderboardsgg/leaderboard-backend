# Contributing to leaderboard-backend

We appreciate your help!

## Before filing an issue

If you are unsure whether you have found a bug, please consider asking in [our discord][discord] first.

Similarly, if you have a question about a potential feature, [the discord][discord] can be a fantastic resource for first comments.

## Filing issues

Filing issues is as simple as going to [the issue tracker](https://github.com/speedrun-website/leaderboard-backend/issues), and adding an issue using one of the below templates.

### Feature Request / Task

```
{Feature Request/Task}: {short description}
---
{Detailed description}

### Affected Functionality
{Any known functionality impacted, or Unknown if further research needs to be done}

### Other Relevant Issues
{Links to Relevant Issues}
```

### Bugs

```
Bug: {short description}
---
{Summary of bug}

### Step(s) to Reproduce
{Numbered list of step(s)}

### Expected Result
{Summary of expected result}

### Actual Outcome
{Description of actual outcome}
```

## Contributing code

### Example code contribution flow

1. Make a fork of this repo.
1. Name a branch on your fork something descriptive for this change (eg. `UpdateReadme`). If possible, reference the issue or task you are working on but this is not necessary.
1. Make your changes.
1. Include unit tests for all changes, and integration tests if it is appropriate (i.e. new endpoints are added).
1. Verify your changes pass all existing and new tests.
1. Commit your changes (Tip! Please read our [Style guide](#style-guide) to help the pull request process go smoothly).
1. Push your branch.
1. Open a pull request to `leaderboardsgg/leaderboard-backend-poc`. If working on a particular issue, please mention that issue in the pull request and describe your changes.
1. Get your pull request approved.
1. Get someone to click `Squash and merge`.
1. [Celebrate][discord] your amazing changes! 🎉

## Style guide

### General

- Be inclusive, this is a project for everyone.
- Be descriptive, it can be hard to understand abbreviations or short-hand.

### C#

- Add tests for any new feature or bug fix, to ensure things continue to work.
- Early returns are great, they help reduce nesting!

### Git

- Do not make pull requests from `main`.
- Do not include slashes in your branch name.
  - Nested paths can act strange when other people start looking at your branch.
- Try to keep commit line length below 80 characters. If more information is needed, include it in the commit body.
- Commits should be as [atomic](https://www.freshconsulting.com/insights/blog/atomic-commits/) as possible.

[discord]: https://discord.gg/TZvfau25Vb
