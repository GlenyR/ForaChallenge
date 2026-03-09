# Fora Challenge – API Guide

REST API that imports company data from the SEC EDGAR Company Facts API, computes fundable amounts from 10-K net income (2018–2022), and exposes companies with standard and special fundable amounts.

---

## Overview

- **Ingestion on startup:** When the API runs, it applies database migrations and, if enabled in config, **processes all pending CIKs** in the background (calls SEC EDGAR, maps data, and persists companies and annual incomes). So you can just run the API and let it ingest; you can also trigger processing manually via `POST api/ciks/process`.
- **Database:** The project uses **SQLite** so reviewers can run it without installing or configuring SQL Server or any other database—no connection strings or DB setup required beyond the default `fora.db` file. For production, you would typically switch to SQL Server (or another supported provider) via configuration and migrations.
- **Tech:** ASP.NET Core, EF Core, clean layering (Domain, Application, Infrastructure, Persistence).

---

## Requirements

- **.NET 10** (or the TFM in the project files).
- Optional: **dotnet-ef** to generate migrations from the CLI:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

---

## How to Run

1. Open the solution root (where the `.sln` file is).
2. Restore and run the API:
   ```bash
   dotnet restore
   dotnet run --project ForaChallenge.API
   ```
3. The API listens on the URLs in `launchSettings.json` (e.g. `http://localhost:5138` or `https://localhost:7285`). OpenAPI is available in Development (e.g. `/openapi/v1.json`).

---

## Database and Migrations

- **Engine:** SQLite. The database file is `fora.db` in the API’s working directory by default (overridable in `appsettings.json`). No SQL server or extra setup needed—ideal for running the challenge locally.
- **Migrations:** Applied **automatically on startup** (`db.Database.Migrate()` in `Program.cs`). You don’t need to run any migration command.
- **Creating a new migration (optional):**
  ```bash
  dotnet ef migrations add YourMigrationName --project ForaChallenge.Persistence --startup-project ForaChallenge.API
  ```

---

## Configuration (`appsettings.json`)

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | SQLite connection string (default: `Data Source=fora.db`). |
| `SecEdgar:BaseUrl` | SEC EDGAR Company Facts API base URL. |
| `SecEdgar:UserAgent` | User-Agent sent to SEC (required; use a descriptive value). |
| `SecEdgar:MaxConcurrentRequests` | Max concurrent requests when processing CIKs (e.g. 3). |
| `SecEdgar:RateLimitMaxConcurrent` | Upper limit for concurrency (e.g. 10). |
| `SecEdgar:ProcessPendingCiksOnStartup` | If `true`, all Pending CIKs are processed when the API starts. |
| `Ciks:FilePath` | Path to the initial CIK list JSON for seeding (optional). Default: `Data/ciks.json` relative to the API directory. |

---

## Initial CIK Seed

If the CIK table is **empty** on first run, the API seeds it from a JSON file (default: `ForaChallenge.API/Data/ciks.json`, or the path in `Ciks:FilePath`). The file should be an array of CIK numbers, e.g. `[320193, 789019, ...]`.

---

## API Endpoints and How to Consume Them

### 1. Companies (companies with fundable amounts)

**GET** `api/companies`

Returns companies with `id`, `name`, `standardFundableAmount`, and `specialFundableAmount`.

- **Optional query:** `nameStartsWith` – filter by the first letter of the company name (e.g. `A`).

**Example requests:**

```http
GET /api/companies
GET /api/companies?nameStartsWith=A
```

**Example response (200 OK):**

```json
[
  {
    "id": 1,
    "name": "Apple Inc.",
    "standardFundableAmount": 1234567.89,
    "specialFundableAmount": 987654.32
  }
]
```

---

### 2. Add CIKs to the queue

**POST** `api/ciks`

Adds one or more CIKs to the import queue (status: Pending). Duplicates are skipped.

**Request body:**

```json
{
  "ciks": [320193, 789019, 1018724]
}
```

**Example (curl):**

```bash
curl -X POST http://localhost:5138/api/ciks \
  -H "Content-Type: application/json" \
  -d "{\"ciks\": [320193, 789019]}"
```

**Example response (200 OK):**

```json
{
  "addedCount": 2
}
```

---

### 3. Process pending CIKs

**POST** `api/ciks/process`

Processes all CIKs with status Pending: calls SEC EDGAR, maps Company Facts (10-K, 2018–2022, NetIncomeLoss in USD), and saves to `Companies` and `CompanyAnnualIncomes`. You can use this if you added CIKs after startup or if you disabled processing on startup.

**Request:** No body.

**Example (curl):**

```bash
curl -X POST http://localhost:5138/api/ciks/process
```

**Example response (200 OK):**

```json
{
  "processedCount": 5,
  "failedCount": 0,
  "exceptionCount": 1
}
```

- `processedCount`: CIKs imported successfully.
- `failedCount`: CIKs that failed (e.g. transient error); they remain Pending for a later retry.
- `exceptionCount`: CIKs marked as Exception (404 or empty file); not retried.

---

## CIK statuses

| Status    | Value | Description |
|-----------|-------|-------------|
| Pending   | 0     | Not yet processed or failed in a retryable way. |
| Processed | 1     | Successfully imported; data in Companies / CompanyAnnualIncomes. |
| Exception | 2     | Non-retryable: CIK not found (404) or file has no usable data. |

When status is **Exception**, the `Message` field stores the reason, e.g.:

- `"The specified key does not exist."` – SEC returned 404 (CIK does not exist).
- `"Empty File"` – CIK exists but response has no entity name or usable data.

In both cases, nothing is written to `Companies` or `CompanyAnnualIncomes`.

---

## Typical flow for reviewers

1. **Run the API:** `dotnet run --project ForaChallenge.API`  
   - Migrations run; if the CIK table is empty, seed is loaded from `Data/ciks.json` (if present).  
   - If `ProcessPendingCiksOnStartup` is `true`, pending CIKs are processed automatically.

2. **Optional – add more CIKs:**  
   `POST api/ciks` with body `{ "ciks": [320193, ...] }`.

3. **Optional – process pending again:**  
   `POST api/ciks/process`.

4. **Get companies:**  
   `GET api/companies` or `GET api/companies?nameStartsWith=A`.

To **reset** and start over, delete the `fora.db` file and run the API again; the database is recreated and migrations and seed run again.

---

## Solution structure (summary)

| Project | Purpose |
|---------|---------|
| **ForaChallenge.API** | Web API, controllers, configuration, startup. |
| **ForaChallenge.Application** | Use cases, service and repository interfaces, DTOs. |
| **ForaChallenge.Domain** | Entities, value objects, enums. |
| **ForaChallenge.Infrastructure** | SEC EDGAR HTTP client, Company Facts mapping, import service, startup hosted service. |
| **ForaChallenge.Persistence** | DbContext, EF repositories, migrations. |
