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

## First-time setup

Download the Tailwind CLI once (it is git-ignored, so every clone needs this):

```powershell
# Windows (PowerShell)
.\scripts\setup-tailwind.ps1
```

```bash
# macOS / Linux
chmod +x scripts/*.sh
./scripts/setup-tailwind.sh
```

## Run

```bash
dotnet run --project src/BloodMatch.Web --launch-profile http
```

Open <http://localhost:5080>. Every `dotnet build` / `dotnet run` recompiles
`Styles/app.css` into `wwwroot/css/app.css` automatically.

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
supabase/migrations/      SQL migrations (added from commit 4)
src/BloodMatch.Web/
  Components/Layout/      MainLayout, Navbar, Footer, BrandLogo
  Components/UI/          Shared UI kit (AppButton, AppCard, FormField, AppAlert,
                          AppBadge, RequestStatusBadge, Spinner, Modal)
  Components/Pages/       Pages (Home, NotFound, Error, Dev/*)
  Styles/app.css          Tailwind source (compiled to wwwroot/css/app.css)
  tailwind.config.js      Palette and theme
  tools/                  Tailwind binary (downloaded, git-ignored)
  Models/ Services/ Data/ Domain models, services, data access
  Program.cs              App startup
```

## Developer pages

- `/dev/components`: every shared component, plus a role switcher that previews the navbar per role.
- `/dev/palette`: the design palette.

Dev pages are removed before release.

## Progress

See [docs/COMMIT_PLAN.md](docs/COMMIT_PLAN.md).
