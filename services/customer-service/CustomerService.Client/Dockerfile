FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["services/customer-service/CustomerService.Client/CustomerService.Client.csproj", "services/customer-service/CustomerService.Client/"]
COPY ["services/customer-service/CustomerService.Business/CustomerService.Business.csproj", "services/customer-service/CustomerService.Business/"]
COPY ["services/customer-service/CustomerService.Common/CustomerService.Common.csproj", "services/customer-service/CustomerService.Common/"]
COPY ["services/customer-service/CustomerService.Data/CustomerService.Data.csproj", "services/customer-service/CustomerService.Data/"]
COPY ["services/customer-service/CustomerService.Contract/CustomerService.Contract.csproj", "services/customer-service/CustomerService.Contract/"]

RUN dotnet restore "services/customer-service/CustomerService.Client/CustomerService.Client.csproj"

COPY . .

WORKDIR "/src/services/customer-service/CustomerService.Client"
RUN dotnet publish "CustomerService.Client.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CustomerService.Client.dll"]