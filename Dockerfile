FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LanguageLearningApp.API.csproj", "./"]
RUN dotnet restore "LanguageLearningApp.API.csproj"
COPY . .
RUN dotnet build "LanguageLearningApp.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LanguageLearningApp.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LanguageLearningApp.API.dll"]