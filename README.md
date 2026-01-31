# Player Bonus API

A CRUD REST API module for managing **Player Bonuses** in an online casino backend.  
Built as an interview task with a clean layered architecture, business rules, audit logging, and unit tests.

---

## Tech Stack

- **.NET 10** Web API
- **PostgreSQL** (Docker)
- **Entity Framework Core**
- **JWT Authentication** (dev token for local testing)
- **AutoMapper**
- **xUnit + Moq + FluentAssertions** (unit tests)

---

## Prerequisites

### Step 1: Install .NET 10 SDK

Download and install the .NET 10 SDK from Microsoft here: https://dotnet.microsoft.com/en-us/download/dotnet/10.0

Verify installation:
- `dotnet --version`

--- 


### Step 2: Install Docker

Install Docker Desktop from here: https://www.docker.com/products/docker-desktop/

Verify installation:

- `docker --version`
- `docker compose version`

---

### Step 3: Run PostgreSQL Locally (Docker)

Run the following command: 
- `docker run -d --name playerbonus-postgres -e POSTGRES_USER=playerbonus -e POSTGRES_PASSWORD=playerbonus_pw -e POSTGRES_DB=playerbonus_local -p 5432:5432 -v playerbonus_pgdata:/var/lib/postgresql/data postgres:16`

---

### Step 4: Run the API

From the solution root:

- `dotnet restore`
- `dotnet run --project PlayerBonusApi`

On startup, the application will automatically:

- Create the database if it does not exist

- Apply EF Core migrations

- Seed demo data if the database is empty

- No manual dotnet ef database update is required.

- Swagger UI will be available at: http://localhost:5051/swagger


--- 

## Authentication (Local Development – Step by Step)

This project uses JWT authentication.
For local development and testing, a development-only endpoint is provided to generate a token.


### Step 1: Create a dev token

Call the following endpoint: POST /api/auth/dev-token

Example request body:

{
  "userId": "42",
  "userName": "Local Dev",
  "role": "Admin"
}

This returns a JWT access token.

### Step 2: Authorize in Swagger

Open Swagger UI

Click Authorize (top-right corner)

Paste the token

Click Authorize

### Step 3: Use secured endpoints

After authorization: All secured endpoints under /api/bonus/* are accessible now and free to be used and tested.


## Tests

The project includes unit tests covering the service layer business logic.

Tests verify:

Creating a bonus (success and conflict cases)

Updating a bonus (business rules and action logging)

Soft deleting a bonus (idempotency and logging)

Retrieving bonuses (by ID and paged lists)

Tests are written using xUnit, Moq, and FluentAssertions and do not rely on a real database.