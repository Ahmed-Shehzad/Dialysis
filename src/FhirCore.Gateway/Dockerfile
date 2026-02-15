FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/", "src/"]
RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore src/FhirCore.Gateway/FhirCore.Gateway.csproj
RUN dotnet publish src/FhirCore.Gateway/FhirCore.Gateway.csproj -c Release -o /app/publish

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FhirCore.Gateway.dll"]
