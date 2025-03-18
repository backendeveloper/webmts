FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["services/transaction-service/TransactionService.Client/TransactionService.Client.csproj", "services/transaction-service/TransactionService.Client/"]
COPY ["services/transaction-service/TransactionService.Business/TransactionService.Business.csproj", "services/transaction-service/TransactionService.Business/"]
COPY ["services/transaction-service/TransactionService.Common/TransactionService.Common.csproj", "services/transaction-service/TransactionService.Common/"]
COPY ["services/transaction-service/TransactionService.Data/TransactionService.Data.csproj", "services/transaction-service/TransactionService.Data/"]
COPY ["services/transaction-service/TransactionService.Contract/TransactionService.Contract.csproj", "services/transaction-service/TransactionService.Contract/"]

RUN dotnet restore "services/transaction-service/TransactionService.Client/TransactionService.Client.csproj"

COPY . .

WORKDIR "/src/services/transaction-service/TransactionService.Client"
RUN dotnet publish "TransactionService.Client.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TransactionService.Client.dll"]