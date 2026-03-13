PinoyPantry — Project Plan (Next Moves)

This file is the ordered list of jobs for this project. Use it to know what to do next. For why we do each step, see LEARNING_GUIDE.md.


Phase 1 — Backend foundation (API)

Job 1: DTOs. Add ProductResponseDto, CreateProductDto, and UpdateProductDto in a DTOs folder. You can write these yourself; they're plain C# classes.

Job 2: AutoMapper. Add the AutoMapper package, create ProductMappingProfile, and register it in Program.cs. This handles mapping between the Product model and the DTOs, and we fix the ImageUrll → ImageUrl naming in the profile.

Job 3: Service layer. Add IProductService and ProductService. The controller will call the service, and the service will call the repository. So the flow is: Controller → Service → Repository.

Job 4: Update controller. The controller should use DTOs and the service. It returns and accepts DTOs, not the raw Product entity.

Job 5: FluentValidation. Add validators for CreateProductDto and UpdateProductDto, and register them in Program.cs. Validation lives in one place, not scattered in the controller.

Job 6: Swagger. Add Swagger (or Scalar), register it in Program.cs. Optional: add XML comments for nicer docs. You get API docs at /swagger or /scalar.

Job 7: Fix CORS. Point the CORS origin to your React app URL, e.g. http://localhost:3000, so the frontend can call the API.

Job 8: Fix Product model typo. Rename ImageUrll to ImageUrl in Product.cs and add a migration. We do this after DTOs and AutoMapper so the mapping is already clear.

Job 9: Pagination and filtering. Add something like ProductQueryParams and update the GET-all endpoint to support page, limit, category, and search. We never return unbounded lists.

Job 10: Global exception handling. Add middleware that catches unhandled exceptions and returns a clean JSON response instead of raw stack traces to the client.

Job 11: Seed data. Seed products (e.g. from your existing CSV) so the database has data on first run and your portfolio site shows real products.


Phase 2 — Connect frontend to API

Job 12: API base URL. Set the React app to call your .NET API using an env variable (e.g. VITE_API_URL). Replace any Shopify or mock URLs.

Job 13: Endpoints match. Make sure the frontend calls match the API (e.g. GET /api/products, GET /api/products/:id). Adjust client code if needed.

Job 14: Pagination UI. Use the pagination response in the client (page, totalPages, etc.) if you added pagination in Phase 1.


Phase 3 — Git, repo, and DevOps

Job 15: GitHub repo. Create a repo and push both PinoyPantry.API and PinoyPantry.Client (monorepo or two repos — your choice). Use main and dev branches.

Job 16: Branching. Work on dev; use feature branches (e.g. feature/add-dtos), merge to dev, then to main. See LEARNING_GUIDE section 9 for the full flow.

Job 17: Azure resources. Create free-tier resources: App Service for the API, Static Web App for the React app, and Azure SQL (or keep SQL Server local for now). Store connection strings in Azure App Settings and GitHub Secrets.

Job 18: GitHub Actions for the API. Add a workflow that builds the .NET project, runs tests, and deploys to App Service on push to main. File: .github/workflows/deploy-api.yml.

Job 19: GitHub Actions for the client. Add a workflow that builds the React app and deploys to Static Web Apps. This is often auto-generated when you create the Static Web App from GitHub.

Job 20: Secrets and config. Put production connection strings and API URLs in GitHub Secrets and Azure config only — never in code. See LEARNING_GUIDE section 11.


Phase 4 — Polish (optional)

Job 21: Basic tests. Add a few unit or integration tests for the API (e.g. GET products, validation). This makes CI/CD meaningful.

Job 22: README. Update the README with how to run the API and client, env vars, and a link to the live site so recruiters can run it.

Job 23: UI polish. Final pass on layout and copy for your CV/portfolio. You said the layout is about 90% done.


Suggested order

Start with Phase 1, Job 1 (DTOs). Then do Job 2 (AutoMapper), Job 3 (Service), Job 4 (Update controller). After that, Jobs 5–11 in order. Then Phase 2, then Phase 3, then Phase 4 as you have time.


Pacing: what happens when you say "go"

We do not implement the full list in one go. We do one chunk at a time so you can read the code and learn.

First "go": Job 1 only — DTOs (three files in the DTOs folder). No controller or AutoMapper changes yet.

Second "go": Job 2 only — AutoMapper (profile plus Program.cs registration).

Third "go": Job 3 only — Service layer (IProductService, ProductService, and Program.cs).

Fourth "go": Job 4 only — Update the controller to use DTOs and the service.

Fifth "go": Job 5 only — FluentValidation (validators and Program.cs).

Sixth "go": Job 6 only — Swagger.

Seventh "go": Job 7 only — Fix CORS.

Eighth "go": Job 8 only — Fix the Product model typo and add a migration.

Ninth "go": Job 9 only — Pagination and filtering.

Tenth "go": Job 10 only — Global exception-handling middleware.

Eleventh "go": Job 11 only — Seed data.

After that, Phase 2, 3, and 4 work the same way — one job (or a small group) per "go" when you're ready.

Optional: For some jobs (e.g. Job 1 DTOs, Job 3 Service interface) you can implement first and ask for a review instead of having everything generated.


Where explanations live

Concepts (the "why") are in LEARNING_GUIDE.md. What to do next (the plan) is in this file, PROJECT_PLAN.md.
