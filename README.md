# Senior .NET Practice

A hands-on practice project focused on modern Senior .NET backend development.

The repository is being built incrementally to practice and demonstrate production-oriented backend development concepts using .NET, PostgreSQL, Docker, and related technologies.

## Current Tech Stack

* .NET 8
* ASP.NET Core Web API
* Entity Framework Core 8
* PostgreSQL
* Npgsql
* Docker / Docker Compose
* Swagger / OpenAPI

## Implemented

### ASP.NET Core API

* REST API for order management
* Create, read, update, delete, and complete order operations
* Request validation
* Pagination and filtering
* Consistent HTTP conflict handling
* Health check endpoint

### Entity Framework Core

* `DbContext` configuration
* PostgreSQL integration
* EF Core migrations
* One-to-many relationships
* LINQ queries and DTO projections
* `AsNoTracking` for read-only queries
* Unique constraints
* Database field-length constraints
* Enum-to-string persistence
* Data normalization migrations
* Database check constraints
* Optimistic concurrency handling
* PostgreSQL `xmin` concurrency token
* Handling of `DbUpdateConcurrencyException`
* Handling of unique constraint violations

### Data Integrity and Validation

The project validates data both at the API boundary and, where appropriate, at the database level.

Examples include:

* required request fields
* maximum string lengths
* positive item quantities
* non-negative prices
* valid order statuses
* page and page-size limits
* unique order numbers
* constrained status values in PostgreSQL

## Project Structure

```text
SeniorDotNetPractice/
├── src/
│   └── SeniorDotNetPractice.Api/
│       ├── Controllers/
│       ├── Data/
│       │   └── Migrations/
│       ├── Entities/
│       ├── Requests/
│       └── Responses/
├── docs/
├── compose.yaml
└── README.md
```

## Running the Project

### Prerequisites

* .NET 8 SDK
* Docker Desktop

### Start PostgreSQL

From the repository root:

```bash
docker compose up -d
```

### Apply EF Core migrations

```bash
dotnet ef database update --project src/SeniorDotNetPractice.Api
```

### Run the API

```bash
dotnet run --project src/SeniorDotNetPractice.Api
```

The application will print the local API URL when it starts.

Swagger can then be used to explore and execute the API endpoints.

## EF Core Practice Notes

Additional EF Core study and implementation notes are available in:

```text
docs/ef-core-practice-notes.md
```

The EF Core portion of the project includes both practical implementation and exploration of topics relevant to Senior .NET interviews, including querying, tracking, migrations, relationships, concurrency, data integrity, and performance considerations.

## Development Approach

The project is intentionally developed in small, reviewable increments.

Changes are implemented through feature branches and pull requests, with Git history preserving the evolution of the implementation.

AI-assisted development with Codex is also being practiced as part of the workflow:

```text
define task
-> inspect code
-> propose or implement changes
-> review diff
-> build and verify
-> commit
```

AI-generated suggestions are reviewed before being accepted rather than treated as automatically correct.

## Next Steps

Planned areas of further hands-on practice include:

* Redis and distributed caching
* additional Docker usage
* Azure
* CI/CD
* observability and monitoring
* further system-design-oriented improvements
* automated testing

The goal is to keep the project focused and incremental rather than introduce infrastructure or architectural patterns without a concrete reason.
