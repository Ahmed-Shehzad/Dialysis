# Parameterized Dockerfile for Dialysis PDMS APIs and Gateway.
# Build with: docker build --build-arg PROJECT_PATH=Services/Dialysis.Patient/Dialysis.Patient.Api/Dialysis.Patient.Api.csproj -t dialysis-patient-api .
# docker-compose passes PROJECT_PATH per service.

ARG PROJECT_PATH
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ARG PROJECT_PATH

COPY . .
RUN dotnet restore "${PROJECT_PATH}"
RUN dotnet publish "${PROJECT_PATH}" -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet"]
# Override CMD in docker-compose per service (e.g. Dialysis.Patient.Api.dll)
