# WEBMTS Microservices

This project is a microservice transformation of the WEBMTS (Western Union Money Transfer System) application.

## Project Structure

```
webmts/
├── docker-compose.yml
├── services/
│   ├── auth-service/
│   ├── customer-service/
│   ├── transaction-service/
│   └── notification-service/
└── infrastructure/
    └── traefik/
```

## Services

1. **Auth Service**: Manages user authentication and authorization
2. **Customer Service**: Handles customer registration and management
3. **Transaction Service**: Processes financial transactions
4. **Notification Service**: Sends notifications to users

## Prerequisites

- Docker and Docker Compose
- .NET 8 SDK (for local development)

## Getting Started

### Running the Services

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/webmts.git
   cd webmts
   ```

2. Start all services using Docker Compose:
   ```bash
   docker-compose up -d
   ```

3. Access the services:
    - Traefik Dashboard: http://localhost:8080
    - Auth Service: http://localhost/api/auth/status
    - Customer Service: http://localhost/api/customers/status
    - Transaction Service: http://localhost/api/transactions/status
    - Notification Service: http://localhost/api/notifications/status

4. Check service health:
    - Auth Service: http://localhost/api/auth/health
    - Customer Service: http://localhost/api/customers/health
    - Transaction Service: http://localhost/api/transactions/health
    - Notification Service: http://localhost/api/notifications/health

### Shutting Down

To stop all services:
```bash
docker-compose down
```

To stop and remove volumes (this will delete all data):
```bash
docker-compose down -v
```

## Development

### Prerequisites for Local Development

- .NET 8 SDK
- PostgreSQL
- RabbitMQ

### Setup for Individual Services

Each service can be run individually for development:

```bash
cd services/auth-service
dotnet run
```

## License

[Your License]