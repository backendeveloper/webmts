FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["services/notification-service/NotificationService.Client/NotificationService.Client.csproj", "services/notification-service/NotificationService.Client/"]
COPY ["services/notification-service/NotificationService.Business/NotificationService.Business.csproj", "services/notification-service/NotificationService.Business/"]
COPY ["services/notification-service/NotificationService.Common/NotificationService.Common.csproj", "services/notification-service/NotificationService.Common/"]
COPY ["services/notification-service/NotificationService.Data/NotificationService.Data.csproj", "services/notification-service/NotificationService.Data/"]
COPY ["services/notification-service/NotificationService.Contract/NotificationService.Contract.csproj", "services/notification-service/NotificationService.Contract/"]

RUN dotnet restore "services/notification-service/NotificationService.Client/NotificationService.Client.csproj"

COPY . .

WORKDIR "/src/services/notification-service/NotificationService.Client"
RUN dotnet publish "NotificationService.Client.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "NotificationService.Client.dll"]