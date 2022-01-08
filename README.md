# leaderboard-backend
An open-source community-driven leaderboard backend for the gaming community.

## Links
- Website: https://leaderboards.gg
- Other Repos: https://github.com/leaderboardsgg
- Discord: https://discord.gg/TZvfau25Vb

# Tech-Stack Information
- This repository only contains the backend, and not the UI for the website.
- .NET is used for implementing the backend.
- JSON API with JWT Authentication

# Developing
## Requirements
- [.NET](https://dotnet.microsoft.com/en-us/download) 6.0

## Useful Links
- [Visual Studio](https://visualstudio.microsoft.com/downloads), for obvious reasons.
- [Monodevelop](https://www.monodevelop.com/) is a cross-platform IDE for C#, among other things.
- [VSCode](https://code.visualstudio.com/download) is a pretty good editor (or [Codium](https://vscodium.com/)).
- [.NET docs](https://docs.microsoft.com/en-us/dotnet/core/get-started) are here.
- [Set up git](https://docs.github.com/en/get-started/quickstart/set-up-git) is GitHub's guide on how to set up and begin using git.
- [How to Contribute to an Open Source Project](https://opensource.guide/how-to-contribute/#opening-a-pull-request) is a useful guide showing some of the steps involved in opening a Pull Request.

## Prerequisite - Setting Up Dev Certs
### Win and Mac
Refer to [this page](https://docs.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide#create-a-self-signed-certificate) on the .NET docs for various ways to do so.
- Under [With dotnet dev-certs](https://docs.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide#create-a-self-signed-certificate), you should just need to do the first two steps.

### Linux
- [This chapter](https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-6.0&tabs=visual-studio#trust-https-certificate-on-linux) in the docs covers how to generate and then trust the dev cert for service-to-service (e.g. cURLing) and browser comms.
  - Trusting certs on Fedora and other distros(??) are linked at the bottom of the chapter.
- You can read [Clean up](https://docs.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide#clean-up) if you'd like to start over.

## How to Run
To start:
- `(cd LeaderboardBackend; dotnet run)`
- You should see an HTTP and HTTPS URL in the console. Use either one to call endpoints.

Running tests:
- `(cd LeaderboardBackend.Test; dotnet test)`

<!-- TODO: Update these with .NET equivalents
Running tests with coverage:
- `make test`

Running tests with race detection (requires `gcc`):
- `make test_race`

Running benchmarks:
- `make bench`
-->
