# CurrencyConverter Solution

A robust ASP.NET Core Web API for currency conversion, featuring ASP.NET Identity, JWT-based authentication, and support for pluggable currency providers.

## 🚀 Features

### ✅ Currency Conversion API

- **Latest Exchange Rates**
  - Retrieve real-time exchange rates for a specific base currency using the Frankfurter API.
  
- **Currency Conversion**
  - Convert an amount from one currency to another.
  - Automatically blocks conversion for unsupported currencies (`TRY`, `PLN`, `THB`, `MXN`) with appropriate error handling.

- **Historical Exchange Rates with Pagination**
  - Retrieve historical exchange rates between a specified date range.
  - Supports pagination for large datasets.

### 🔒 Security & Access Control

- **JWT Authentication**
  - Secure endpoints using JSON Web Tokens.
  
- **Role-Based Access Control (RBAC)**
  - Restrict access to sensitive endpoints based on user roles.

- **API Throttling / Rate Limiting**
  - Prevent abuse and excessive API usage with request limiting.

### ⚙️ Architecture & Extensibility

- **Clean Architecture**
  - Modular and layered architecture separating concerns (Domain, Application, Infrastructure, API).

- **Factory Pattern for Provider Selection**
  - Easily extendable to support multiple exchange rate providers in the future.

- **Dependency Injection**
  - All services and repositories are injected using .NET Core’s built-in DI container.

### ⚡ Resilience & Performance

- **Caching**
  - In-memory caching of API responses to reduce external API calls.

- **Retry Policy with Exponential Backoff**
  - Automatic retry on transient failures using Polly.

- **Circuit Breaker**
  - Graceful degradation during external API outages.

### 📊 Observability & Logging

- **Structured Logging with Serilog**
  - Each Request is logged enriched with context including:
    - Client IP
    - Client ID (from JWT)
    - HTTP Method & Endpoint
    - Response Code & Response Time

- **Log Correlation with External API**
  - Helps in debugging downstream call issues.
  
- ** distributed tracing using OpenTelemetry

### ✅ Testing

- **90%+ Unit Test Coverage**
  - Covers core business logic with xUnit.

- **Integration Tests**
  - Validates API behavior under real-world conditions.

- **Test Coverage Reports**
  - Generated using Coverlet & ReportGenerator.

### 📦 Deployment Readiness

- **Environment-Based Configuration**
  - Supports multiple environments: Development, Test, Production.

- **API Versioning**
  - Versioned endpoints to support future enhancements.

- **Horizontal Scalability**
  - Stateless design suitable for containerization and load balancing.


## Prerequisites

Before running the application locally, make sure you have the following installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Docker](https://www.docker.com/) (for running Seq)

> 💡 **Note**: Local SQL Server is expected to be available and accessible via the default connection string.
> The application will **automatically run database migrations and seed initial data** on startup.

### 🐳 Running Seq with Docker

If you want to run Seq for structured log viewing, you can use the following Docker command:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq```


## 🚀 Getting Started

Follow these steps to run the Currency Converter API locally:

### 1. Clone the Repository

```bash
git clone https://github.com/lamiamustafa/CurrencyConverterAPI.git
cd CurrencyConverterAPI```



### 2. **Configure app settings:**
  - Update the `appsettings.Development.json` file (or `appsettings.json`) under the `CurrencyConverter.API` project with your local settings if needed

### 3. (Optional) Start Seq for structured logging
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq
# View logs at: http://localhost:5341

### 4. Run the application
cd src/CurrencyConverter.API
dotnet build
dotnet run

#### ✅ API Availability

The API will be available at:

- `https://localhost:7164`

#### 🗄️ Database Initialization

The database is automatically created and seeded with an initial **Admin user** as configured in `appsettings.json`.

#### 🔐 Authentication

You can retrieve a JWT token by sending a `POST` request to:

- `https://localhost:7164/api/auth/login`

### 5. Run Tests and Generate Coverage Report (Optional)

You can run unit tests and generate a code coverage report using the following commands:

```bash
# Run tests with code coverage collection
dotnet test --collect:"XPlat Code Coverage" --settings coveragerunsettings.runsettings

# Generate HTML coverage report (requires ReportGenerator tool)
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html```


## 🚧 Future Enhancements

- Refactor configuration binding using `IOptions` and `AddOptions` for improved maintainability
- Integrate Redis or PostgreSQL for distributed caching of currency data
- Improve data model structure and apply robust data validation rules
- Implement centralized and consistent error handling across the API
- Secure sensitive configuration values by moving them to environment secrets or a secure vault
- Dockerize the entire app with SQL Server and Seq for local development

## 📌 Assumptions

- Only one currency provider is currently used, but others can be added later
- Admin user is predefined and seeded via `appsettings.json`
- Application is intended to run in a trusted environment (e.g., internal use)
- Currency rates are assumed to be stable enough for real-time use without caching for now
- It is considered safe for the business to assume one exchange rate per day per base currency
- The latest exchange rates are cached and remain valid until the end of the day   