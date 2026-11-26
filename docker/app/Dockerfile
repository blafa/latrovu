# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /src
COPY . .
RUN dotnet publish "src/Hosts/${PROJECT}/${PROJECT}.csproj" -c Release -o /app -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG PROJECT
ENV APP_DLL=${PROJECT}.dll \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_ENVIRONMENT=Production \
    ASPNETCORE_ENVIRONMENT=Production
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["sh", "-c", "exec dotnet \"$APP_DLL\""]
