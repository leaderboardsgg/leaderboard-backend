dotnet tool restore
docker compose up -d
dotnet ef database update --project LeaderboardBackend
dotnet run --project LeaderboardBackend --urls https://localhost:7128
