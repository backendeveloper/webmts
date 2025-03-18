FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["services/auth-service/AuthService.Client/AuthService.Client.csproj", "services/auth-service/AuthService.Client/"]
COPY ["services/auth-service/AuthService.Business/AuthService.Business.csproj", "services/auth-service/AuthService.Business/"]
COPY ["services/auth-service/AuthService.Common/AuthService.Common.csproj", "services/auth-service/AuthService.Common/"]
COPY ["services/auth-service/AuthService.Data/AuthService.Data.csproj", "services/auth-service/AuthService.Data/"]
COPY ["services/auth-service/AuthService.Contract/AuthService.Contract.csproj", "services/auth-service/AuthService.Contract/"]

RUN dotnet restore "services/auth-service/AuthService.Client/AuthService.Client.csproj"

COPY . .

WORKDIR "/src/services/auth-service/AuthService.Client"
RUN dotnet publish "AuthService.Client.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AuthService.Client.dll"]