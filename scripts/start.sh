dotnet tool restore
cd LeaderboardBackend
dotnet ef database update
cd ../
docker compose up -d
dotnet run --project LeaderboardBackend --urls https://localhost:7128
