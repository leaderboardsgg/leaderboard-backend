#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS "80"
EXPOSE 80

# Note: when debugging with Visual Studio, the other stages are not used

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG DISABLE_OPENAPI_FILE_GEN=true

WORKDIR /source
# copy csproj and restore as distinct layer that can be cached
COPY LeaderboardBackend/LeaderboardBackend.csproj LeaderboardBackend/
WORKDIR /source/LeaderboardBackend
RUN dotnet restore LeaderboardBackend.csproj

# copy everything else and build app
COPY LeaderboardBackend/. .
RUN dotnet build LeaderboardBackend.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish LeaderboardBackend.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LeaderboardBackend.dll"]
