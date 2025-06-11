FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 7800

ENV ASPNETCORE_URLS=http://+:7800

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["ChatApp_Server.csproj", "./"]
RUN dotnet restore "ChatApp_Server.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ChatApp_Server.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "ChatApp_Server.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatApp_Server.dll"]
