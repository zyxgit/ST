ARG PROJECT
ARG DLL

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /src
COPY . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "${PROJECT}"
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish "${PROJECT}" -c Release -o /app/publish --no-restore \
    /p:ErrorOnDuplicatePublishOutputFiles=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG DLL
ENV DLL=${DLL}
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT dotnet "${DLL}"
