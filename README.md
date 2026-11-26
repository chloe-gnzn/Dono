# BloodMatch

A non-commercial blood matching portal. It connects families who need blood with
compatible volunteer donors, with trusted moderators verifying every request.
It is a **secure communication intermediary, not an inventory system**: patient
identities are never shown to donors.

## Stack

| Layer      | Technology                                   |
|------------|----------------------------------------------|
| App        | Blazor Web App (.NET 10), Interactive Server |
| Styling    | Tailwind CSS 3 (standalone CLI, no Node)     |
| Database   | Supabase (Postgres, Auth, Storage)           |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (`dotnet --version` should print `10.x`)
- Git
- A Supabase project (with migrations in `supabase/migrations/` applied)

## First-time setup

### 1. Download the Tailwind CLI

It is git-ignored, so every clone needs this once:

```powershell
# Windows (PowerShell)
.\scripts\setup-tailwind.ps1
```

```bash
# macOS / Linux
chmod +x scripts/*.sh
./scripts/setup-tailwind.sh
```

### 2. Configure Supabase secrets

Never commit Supabase keys. Store them locally using .NET user-secrets:

```bash
cd src/BloodMatch.Web
dotnet user-secrets set "Supabase:Url" "https://YOUR_PROJECT_REF.supabase.co"
dotnet user-secrets set "Supabase:AnonKey" "YOUR_ANON_KEY"
dotnet user-secrets set "Supabase:ServiceRoleKey" "YOUR_SERVICE_ROLE_KEY"
```

## Run

```bash
dotnet run --project src/BloodMatch.Web --launch-profile http
```

Open <http://localhost:5080>. Visit <http://localhost:5080/health> (or `/healthz`) to run the Supabase connectivity test.

### Live development (recommended)

Use two terminals so styling updates as you edit Razor files:

```bash
# terminal 1
dotnet watch --project src/BloodMatch.Web --launch-profile http

# terminal 2 (Windows: .\scripts\tailwind-watch.ps1)
./scripts/tailwind-watch.sh
```

## Repository layout

```
BloodMatch.sln
docs/                     COMMIT_PLAN.md (progress), DESIGN.md (palette, styling rules)
scripts/                  Tailwind setup and watch scripts
supabase/migrations/      SQL migrations (schema, RLS, compatibility & seed data)
src/BloodMatch.Web/
  Components/             Razor components (App, Routes, Layout, Pages)
  Configuration/          Strongly-typed options (SupabaseSettings)
  Styles/app.css          Tailwind source (compiled to wwwroot/css/app.css)
  tailwind.config.js      Palette and theme
  tools/                  Tailwind binary (downloaded, git-ignored)
  Models/ Services/ Data/ Domain models, services, data access
  Program.cs              App startup
```

## Developer pages

- `/health`: Supabase configuration and connectivity check.
- `/dev/components`: every shared component, plus a role switcher that previews the navbar per role.
- `/dev/palette`: the design palette.

Dev pages are removed before release.

## Progress

See [docs/COMMIT_PLAN.md](docs/COMMIT_PLAN.md).
