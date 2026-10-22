FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/YachtSharing.MockGims/YachtSharing.MockGims.csproj", "src/YachtSharing.MockGims/"]

RUN dotnet restore "src/YachtSharing.MockGims/YachtSharing.MockGims.csproj"

COPY . .
WORKDIR "/src/src/YachtSharing.MockGims"
RUN dotnet publish "YachtSharing.MockGims.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "YachtSharing.MockGims.dll"]
