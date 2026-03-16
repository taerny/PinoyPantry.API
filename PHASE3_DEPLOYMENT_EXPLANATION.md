# Phase 3 — Deploying to Azure (Full Explanation)

This document explains everything we did to take the PinoyPantry app from
"works on my PC" to "live on the internet." Read it top to bottom like a
story — every step is in the order we actually did it.

---

## The Big Picture

Before deployment, the app only worked on your local machine:

```
Browser (localhost:3001)  →  .NET API (localhost:7136)  →  SQL Server Express (localhost)
```

After deployment, anyone in the world can use it:

```
Browser
  → Azure Static Web App  (React frontend)
       → Azure App Service (NET API)
            → Azure SQL Database (products data)
```

Three separate Azure services, each doing one job.

---

## What Is Azure?

Azure is Microsoft's cloud platform. Think of it as renting computers
from Microsoft instead of buying your own server. You tell Azure what
you need (a web server, a database, etc.) and they create it for you
in seconds. You access everything through https://portal.azure.com.

### Real-World Equivalent

| Azure                | Traditional Way                         |
|----------------------|-----------------------------------------|
| Azure App Service    | Buying a server, installing IIS/.NET    |
| Azure SQL Database   | Installing SQL Server on that server    |
| Azure Static Web App | Uploading HTML/JS to a web host         |
| Azure Portal         | Walking into the server room            |

---

## Step 1 — Create an Azure Account

1. Go to https://azure.microsoft.com/free
2. Click "Start free"
3. Sign in with a Microsoft account (or create one)
4. Enter your card information

**Why a card?** Microsoft needs it to verify you are a real person.
You will NOT be charged. The free tier is genuinely free. Azure gives
you $200 credit for 30 days, and free-tier services continue forever
after that.

**What you get for free (no time limit):**
- Azure Static Web Apps — for React/frontend apps
- Azure App Service F1 — for .NET API / backend

**What you get free for 12 months:**
- Azure SQL Database — 32 GB, free until March 2027
- After that: ~$5/month for Basic tier (2 GB) — more than enough

---

## Step 2 — Create a Resource Group

**What is it?** A folder that holds all your Azure resources together.
It does not cost money. It is purely organizational.

**Why?** Without it, your resources are scattered across the portal.
A resource group keeps them tidy — like a project folder on your PC.

**How we created it:**
1. Azure Portal → "Resource groups" → "+ Create"
2. Name: `pinoypantry-rg`
3. Region: Australia East
4. Click "Review + create" → "Create"

**Naming convention:** The `-rg` suffix stands for "Resource Group."
In real teams, every resource gets a suffix so you know what it is:

```
pinoypantry-rg       →  Resource Group (container)
pinoypantry-api      →  App Service (API host)
pinoypantry-client   →  Static Web App (frontend)
pinoypantry-db       →  SQL Database
pinoypantry-server   →  SQL Server instance
```

---

## Step 3 — Create Azure SQL Database

**What is it?** Your database in the cloud. Same as your local
SQL Server Express, but hosted by Microsoft so the live API can reach it.

**How we created it:**
1. Azure Portal → search "SQL databases" → "+ Create"
2. Resource group: `pinoypantry-rg`
3. Database name: `pinoypantry-db`
4. Server: "Create new"
   - Server name: `pinoypantry-server`
   - Location: Australia East
   - Authentication: SQL authentication
   - Admin login: `pinoyadmin`
   - Password: `PinoyPantry2026!`
5. Selected "Free offer" (32 GB, free for 12 months)
6. Review + create → Create

**Important credentials (keep these safe):**

```
Server:   pinoypantry-server.database.windows.net
Database: pinoypantry-db
Username: pinoyadmin
Password: PinoyPantry2026!
```

**Connection string (ADO.NET / SQL authentication):**

```
Server=tcp:pinoypantry-server.database.windows.net,1433;
Initial Catalog=pinoypantry-db;
Persist Security Info=False;
User ID=pinoyadmin;
Password=PinoyPantry2026!;
MultipleActiveResultSets=False;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

---

## Step 4 — Create Azure App Service (for the .NET API)

**What is it?** A web server that runs your .NET API. Like IIS on
your local machine, but in the cloud. Anyone can hit the URL and
get API responses.

**How we created it:**
1. Azure Portal → "Create a resource" → search "Web App" → Create
2. Resource group: `pinoypantry-rg`
3. Name: `pinoypantry-api`
4. Publish: Code
5. Runtime stack: .NET 8 (LTS)
6. Operating System: Windows
7. Region: Australia East
8. Pricing plan: F1 (Free)
9. Review + create → Create

**The URL Azure gave us:**

```
https://pinoypantry-api-f0a8hbfwc6fwdfbg.australiaeast-01.azurewebsites.net
```

Note: Azure adds a random suffix to make the URL unique. The resource
name is still `pinoypantry-api`, but the public URL is longer.

**F1 Free Tier limits:**
- 60 CPU minutes/day
- Sleeps after 20 minutes of no traffic (first request after sleep
  takes 5-10 seconds to "wake up")
- No custom domain or SSL on F1 (need B1 tier for that)
- Perfect for a portfolio/CV project

---

## Step 5 — Create Azure Static Web App (for the React frontend)

**What is it?** A free hosting service for static sites (React, Vue,
Angular, plain HTML). It connects directly to your GitHub repo and
auto-deploys when you push code.

**How we created it:**
1. Azure Portal → search "Static Web Apps" → "+ Create"
2. Resource group: `pinoypantry-rg`
3. Name: `pinoypantry-client`
4. Plan type: Free
5. Region: East Asia
6. Deployment source: GitHub
   - Signed in with GitHub (authorized Azure)
   - Organization: `taerny`
   - Repository: `PinoyPantry.Client`
   - Branch: `main`
7. Build presets: React
   - App location: `/`
   - Output location: `build`
8. Review + create → Create

**What Azure did automatically:**
- Added a GitHub Actions workflow file to `PinoyPantry.Client` repo
  at `.github/workflows/azure-static-web-apps-gentle-dune-0c69a8700.yml`
- Triggered the first build and deploy
- Your frontend went live at:
  `https://gentle-dune-0c69a8700.6.azurestaticapps.net`

**This is the magic of Static Web Apps** — Azure sets up CI/CD for
you. Every `git push` to `main` triggers an automatic redeploy.

---

## Step 6 — Deploy the .NET API with GitHub Actions

Unlike Static Web Apps, App Service does NOT auto-generate a workflow.
We had to create it manually.

### 6a. Enable Basic Auth Publishing

Before we could download the publish profile, Azure blocked it:

**Error:** `"Download publish profile: basic auth is disabled"`

**Fix:**
1. pinoypantry-api → Settings → Configuration → General settings
2. Check "SCM Basic Auth Publishing Credentials"
3. Click Apply

**What is SCM?** Source Control Manager — the deployment endpoint
that GitHub Actions pushes code to. "Basic auth" means username/password
authentication. Azure disables it by default for security, but GitHub
Actions needs it.

### 6b. Download the Publish Profile

1. pinoypantry-api → Overview → "Get publish profile" (button at top)
2. Downloads a `.PublishSettings` XML file containing deployment credentials

**What is a publish profile?** An XML file with the Azure deployment
endpoint URL, username, and password. It is the "key" that gives
GitHub Actions permission to push code to your App Service.

### 6c. Add Publish Profile as a GitHub Secret

**Why a secret?** The publish profile contains passwords. You never
want passwords in your code or visible to anyone. GitHub Secrets
encrypts them — even you cannot read it after saving.

1. Go to `github.com/taerny/PinoyPantry.API` → Settings
2. Secrets and variables → Actions
3. "New repository secret"
4. Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
5. Value: entire contents of the .PublishSettings file (Ctrl+A, Ctrl+C
   from Notepad, then paste)
6. "Add secret"

**SSH vs GitHub Secrets:**

| SSH                                  | GitHub Secret                          |
|--------------------------------------|----------------------------------------|
| A way to log INTO a server manually  | A stored credential for automation     |
| You type commands in a terminal      | GitHub Actions uses it behind the scenes|
| Like unlocking your front door       | Like a key in a lockbox for a robot    |

### 6d. Create the Workflow File

We created `.github/workflows/deploy-api.yml`:

```yaml
name: Deploy .NET API to Azure App Service

on:
  push:
    branches:
      - main          # Runs every time you push to main

jobs:
  build-and-deploy:
    runs-on: windows-latest

    steps:
      - name: Checkout code              # 1. Download code from GitHub
        uses: actions/checkout@v4

      - name: Set up .NET 8              # 2. Install .NET 8 on the runner
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies       # 3. Download NuGet packages
        run: dotnet restore

      - name: Build                      # 4. Compile the project
        run: dotnet build --configuration Release --no-restore

      - name: Publish                    # 5. Create deployment-ready files
        run: dotnet publish --configuration Release --output ./publish --no-build

      - name: Deploy to Azure App Service  # 6. Push files to Azure
        uses: azure/webapps-deploy@v3
        with:
          app-name: pinoypantry-api
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```

**What each step does:**

1. `checkout` — downloads your code from GitHub to the runner machine
2. `setup-dotnet` — installs .NET 8 SDK (the runner starts empty)
3. `dotnet restore` — downloads all NuGet packages (like `npm install`)
4. `dotnet build` — compiles C# code into DLLs
5. `dotnet publish` — creates a folder with everything needed to run
6. `azure/webapps-deploy` — uploads that folder to Azure App Service
   using the publish profile secret

**Equivalent on other platforms:**

| Platform         | CI/CD Config File                        | Auto-setup? |
|------------------|------------------------------------------|-------------|
| GitHub Actions   | `.github/workflows/deploy-api.yml`       | No — you write it |
| Azure Static Web | `.github/workflows/azure-static-web-...` | Yes — Azure creates it |
| Azure DevOps     | `azure-pipelines.yml`                    | No — you write it |
| Netlify          | No file needed (dashboard config)        | Yes — fully automatic |
| Vercel           | No file needed (dashboard config)        | Yes — fully automatic |
| GitLab CI/CD     | `.gitlab-ci.yml`                         | No — you write it |
| AWS CodePipeline | `buildspec.yml`                          | No — you write it |

They all do the same thing: automatically build and deploy when you push code.

---

## Step 7 — Connect the API to Azure SQL Database

After deploying, the API returned a 500 error:

```json
{
  "status": 500,
  "message": "An unexpected error occurred. Please try again later."
}
```

**Why?** The API was trying to connect to `localhost\SQLEXPRESS01` —
your local SQL Server. That does not exist on Azure's servers.

### 7a. Add Connection String to App Service

We added the Azure SQL connection string as an environment variable:

1. pinoypantry-api → Environment variables → + Add
2. Name: `ConnectionStrings__DefaultConnection`
3. Value: the full Azure SQL connection string (with real password)
4. Apply → Confirm

**Why `ConnectionStrings__DefaultConnection`?**
In .NET, `__` (double underscore) maps to `:` in config. So this
becomes `ConnectionStrings:DefaultConnection` — exactly what
`appsettings.json` uses. Azure environment variables override
`appsettings.json` at runtime, so the live API uses Azure SQL
while your local API still uses localhost.

### 7b. Run Migrations on Azure SQL

The Azure SQL Database was empty — no tables, no products. We ran
migrations from Visual Studio targeting the Azure database:

```
Update-Database -Connection "Server=tcp:pinoypantry-server.database.windows.net,1433;Initial Catalog=pinoypantry-db;Persist Security Info=False;User ID=pinoyadmin;Password=PinoyPantry2026!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

**First attempt failed:**

```
Database 'pinoypantry-db' on server 'pinoypantry-server.database.windows.net'
is not currently available. Please retry the connection later.
```

**Why?** Azure SQL has a firewall that blocks ALL external connections
by default. Your PC's IP was not allowed.

**Fix — Add firewall rule:**
1. Azure Portal → pinoypantry-server (SQL Server) → Networking
2. Click "+ Add your client IPv4 address" (Azure auto-detected our IP)
3. Check "Allow Azure services and resources to access this server"
   (so the App Service can also connect)
4. Save

**Second attempt succeeded.** EF Core created the tables and seeded
10 products into the Azure SQL Database.

### 7c. Verify in SSMS

You can browse the Azure SQL Database from SSMS on your PC:

1. Open SSMS → Connect
2. Server name: `pinoypantry-server.database.windows.net`
3. Authentication: SQL Server Authentication
4. Login: `pinoyadmin`
5. Password: `PinoyPantry2026!`
6. Expand pinoypantry-db → Tables → right-click Products →
   "Select Top 1000 Rows"

You see the same 10 products that the live API returns.

---

## Step 8 — Point the Frontend to the Live API

### 8a. Add VITE_API_URL to the Build Workflow

Vite "bakes" environment variables at BUILD TIME, not runtime. Setting
a var in Azure portal doesn't help — it needs to be available when
`npm run build` runs in GitHub Actions.

We added it directly to the workflow file:

```yaml
      - name: Build And Deploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          # ... other config ...
        env:
          VITE_API_URL: https://pinoypantry-api-f0a8hbfwc6fwdfbg.australiaeast-01.azurewebsites.net
```

**Why not Azure portal env vars?** Azure Static Web Apps passes portal
env vars at runtime, but Vite needs them at build time. The `env:` block
in the workflow file makes the var available during the build step.

### 8b. CORS Error — The Final Bug

After deploying both the API and frontend, the site showed no products.
Browser console showed:

```
Access to fetch at 'https://pinoypantry-api-...azurewebsites.net/api/products'
from origin 'https://gentle-dune-0c69a8700.6.azurestaticapps.net'
has been blocked by CORS policy: No 'Access-Control-Allow-Origin'
header is present on the requested resource.
```

**Why?** The API's CORS policy in `Program.cs` only allowed
`localhost:3000` and `localhost:3001`. The Azure Static Web App
has a completely different URL — the API rejected it.

**Fix — Add the Azure URL to CORS in Program.cs:**

```csharp
// Before (only local):
policy.WithOrigins("http://localhost:3000", "http://localhost:3001")

// After (local + live):
policy.WithOrigins(
    "http://localhost:3000",
    "http://localhost:3001",
    "https://gentle-dune-0c69a8700.6.azurestaticapps.net")
```

Push to GitHub → GitHub Actions auto-deploys → API restarts with new
CORS policy → frontend can now call the API → products appear!

---

## Step 9 — Removing Shopify (Cleanup)

The frontend originally used Shopify as a data source. When deployed
to Azure, it crashed because Shopify credentials were not set.

### Error 1: Shopify validation crash

```
Uncaught Error: VITE_SHOPIFY_STORE_DOMAIN is required in production.
```

**Fix:** Removed the validation check in `shopifyClient.ts`.

### Error 2: Shopify library crash

```
Uncaught Error: [h2:error:createStorefrontClient] `storeDomain`
is required when creating a new Storefront client in production.
```

**Fix:** The `@shopify/hydrogen-react` library itself throws when
`storeDomain` is empty. We guarded the call so it only runs when
credentials exist.

### Final cleanup — complete Shopify removal:

- Deleted `shopifyClient.ts`, `shopifyProductService.ts`,
  `shopifyCustomerService.ts`
- Removed all Shopify imports and fallback code from `productService.ts`
- Updated `LoginPage.tsx` to show "coming soon" instead of calling Shopify
- Uninstalled `@shopify/hydrogen-react` npm package (removed 22 packages)
- Cleaned up Shopify comments across type files

**Data source priority after cleanup:**
1. `VITE_API_URL` is set → use .NET API (live site)
2. Nothing configured → use mock data (fallback)

---

## Summary — All Azure Resources

| Resource              | Type             | URL / Info                           | Cost      |
|-----------------------|------------------|--------------------------------------|-----------|
| `pinoypantry-rg`      | Resource Group   | Container for all resources          | Free      |
| `pinoypantry-api`     | App Service F1   | `pinoypantry-api-f0a8...azurewebsites.net` | Free forever |
| `pinoypantry-client`  | Static Web App   | `gentle-dune-0c69a8700.6.azurestaticapps.net` | Free forever |
| `pinoypantry-db`      | SQL Database     | `pinoypantry-server.database.windows.net` | Free until Mar 2027, then ~$5/mo |
| `pinoypantry-server`  | SQL Server       | Hosts the database                   | Free      |

---

## Summary — The CI/CD Flow

**Frontend (automatic, set up by Azure):**

```
You edit React code
  → git push to PinoyPantry.Client
    → GitHub Actions runs automatically
      → Builds React app (npm run build)
      → Deploys to Azure Static Web App
        → Live site updates in ~2 minutes
```

**API (automatic, set up by us):**

```
You edit C# code
  → git push to PinoyPantry.API
    → GitHub Actions runs automatically
      → Installs .NET 8
      → Builds and publishes the API
      → Deploys to Azure App Service
        → Live API updates in ~3-5 minutes
```

**You never manually deploy again.** Just push to GitHub and
everything happens automatically. That is CI/CD.

---

## Summary — All Errors and Fixes

| # | Error | Cause | Fix |
|---|-------|-------|-----|
| 1 | Shopify `storeDomain` required in production | Old code checking for Shopify credentials | Removed validation, then removed all Shopify code |
| 2 | Site loads but shows no products | `VITE_USE_MOCK_DATA` and `VITE_API_URL` not set | Made mock data the default fallback |
| 3 | Basic auth is disabled (publish profile) | Azure disables SCM auth by default | Enabled SCM Basic Auth in General settings |
| 4 | API returns 500 error | API trying to connect to localhost SQL | Added Azure SQL connection string to App Service env vars |
| 5 | Migration fails — database not available | Azure SQL firewall blocking your PC's IP | Added client IP + allowed Azure services in firewall |
| 6 | Frontend can't reach API — CORS blocked | API only allowed localhost origins | Added Azure Static Web App URL to CORS policy |
| 7 | VITE_API_URL not picked up in build | Azure portal env vars not available at Vite build time | Added env var directly in GitHub Actions workflow file |

---

## Key Concepts — One Sentence Each

**Resource Group** — A folder in Azure that keeps related resources together.

**App Service** — A managed web server that runs your .NET API in the cloud.

**App Service Plan (F1)** — The size/tier of the server. F1 = free, limited.

**Static Web App** — Free hosting for frontend apps (React/Vue/Angular) with
built-in GitHub integration.

**Azure SQL Database** — SQL Server in the cloud. Same T-SQL you know locally.

**SQL Server firewall** — By default blocks ALL external connections. You must
whitelist IP addresses.

**Publish Profile** — An XML file with deployment credentials for App Service.

**GitHub Secret** — Encrypted storage for sensitive values (passwords, keys)
that GitHub Actions can use but humans cannot read.

**GitHub Actions** — Automation that runs on every push. Builds and deploys code.

**Workflow file (.yml)** — The recipe that tells GitHub Actions what to do.

**CI/CD** — Continuous Integration / Continuous Deployment. Code goes from
your editor to the live site automatically.

**CORS** — Cross-Origin Resource Sharing. The API must explicitly allow
which URLs can call it. Without it, browsers block the request.

**Environment variable** — A setting stored outside your code. Used for
connection strings, API URLs, and secrets. Never hardcode secrets in code.

**`ConnectionStrings__DefaultConnection`** — Double underscore maps to
`:` in .NET config. This overrides `appsettings.json` at runtime.

**Vite build-time variables** — `VITE_*` vars are baked into the JavaScript
bundle during `npm run build`. They cannot be changed after the build.

---

## Live URLs

- **Frontend:** https://gentle-dune-0c69a8700.6.azurestaticapps.net
- **API:** https://pinoypantry-api-f0a8hbfwc6fwdfbg.australiaeast-01.azurewebsites.net
- **API Products endpoint:** https://pinoypantry-api-f0a8hbfwc6fwdfbg.australiaeast-01.azurewebsites.net/api/products
- **GitHub (Frontend):** https://github.com/taerny/PinoyPantry.Client
- **GitHub (API):** https://github.com/taerny/PinoyPantry.API
