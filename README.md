# BloodMatch

A non-commercial blood matching portal. It connects families who need blood with
compatible volunteer donors, with trusted moderators verifying every request.
It is a **secure communication intermediary, not an inventory system**: patient
identities are never shown to donors.

## Stack

| Layer      | Technology                                   |
|------------|----------------------------------------------|
| App        | Blazor Web App (.NET 10), Interactive Server |
| Styling    | Tailwind CSS (standalone CLI)                |
| Database   | Supabase (Postgres, Auth, Storage)           |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (`dotnet --version` should print `10.x`)
- Git

## Run

```bash
dotnet run --project src/BloodMatch.Web --launch-profile http
```

Then open <http://localhost:5080>.

## Repository layout

```
BloodMatch.sln
docs/                     Planning notes (see COMMIT_PLAN.md)
supabase/migrations/      SQL migrations (added from commit 4)
src/BloodMatch.Web/
  Components/             Razor components (App, Routes, Layout, Pages)
  Models/                 Domain/view models
  Services/               Application services
  Data/                   Data access
  Program.cs              App startup
```

## Progress

See [docs/COMMIT_PLAN.md](docs/COMMIT_PLAN.md).
