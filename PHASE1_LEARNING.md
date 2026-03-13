PinoyPantry API - Phase 1 Learning Guide
=========================================

This file explains every job we implemented in Phase 1, what files were touched,
what the code does, and why each decision was made. Read this whenever you want
to review or explain what was built.


WHAT WAS BUILT IN PHASE 1
--------------------------

The goal of Phase 1 was to take the raw starter API (which had a controller talking
directly to a database) and transform it into a professional, layered architecture that
follows real-world .NET patterns.

Before Phase 1, the flow was:
  HTTP Request -> Controller -> Repository -> Database

After Phase 1, the flow is:
  HTTP Request -> Middleware (exception handling)
               -> Controller (accepts/returns DTOs, validates input)
               -> Service (business logic, mapping)
               -> Repository (database queries with filtering/pagination)
               -> Database


JOB 1: DTOs - Data Transfer Objects
-------------------------------------

Files added:
  DTOs/ProductResponseDto.cs
  DTOs/CreateProductDto.cs
  DTOs/UpdateProductDto.cs

Why DTOs exist:
  The Product model (Models/Product.cs) is your database shape. It maps 1:1 to the
  SQL table. If you expose this directly through your API, you have no control over
  what goes in or out. For example, if you add a "SupplierCost" field later, it would
  automatically leak to anyone calling the API.

  DTOs are separate classes that define exactly what shape data takes when crossing
  the API boundary (in or out).

ProductResponseDto - used for GET responses (data going OUT to the frontend):
  Has: Id, Name, Description, Price, ImageUrl, Category
  Missing: StockQuantity (internal inventory info), CreatedAt (internal timestamp)
  The frontend doesn't need to know how many items are in stock or when a record
  was created. We choose exactly what to expose.

CreateProductDto - used for POST requests (data coming IN to create a product):
  Has: Name, Description, Price, ImageUrl, Category, StockQuantity
  Missing: Id (the database generates this automatically)
           CreatedAt (the server sets this, not the user)

UpdateProductDto - used for PUT requests (data coming IN to update a product):
  Same as CreateProductDto.
  Missing: Id (comes from the URL, e.g. PUT /api/products/5)
           CreatedAt (you should never change when something was created)

Key rule: Your database model is for EF Core. Your DTOs are for the outside world.


JOB 2: AutoMapper
------------------

Files added:
  Mappings/ProductMappingProfile.cs

Files changed:
  PinoyPantry.API.csproj (added AutoMapper package)
  Program.cs (registered AutoMapper)

Why AutoMapper:
  Without it, you would write this every time you need to convert between types:
    var dto = new ProductResponseDto { Id = product.Id, Name = product.Name, ... };
  With 10 models and 3 DTOs each, that is 30+ manual conversion blocks. AutoMapper
  reads both classes and maps matching properties automatically. You only configure
  it once in a Profile class.

How the profile works:
  CreateMap<Product, ProductResponseDto>(); means: when I call
  _mapper.Map<ProductResponseDto>(product), AutoMapper reads the Product fields and
  fills the matching fields on ProductResponseDto. All same-named fields are mapped
  automatically.

  The .ForMember() calls were originally needed because the Product model had a typo
  (ImageUrll vs ImageUrl). After Job 8 fixed the typo, those .ForMember() calls were
  removed and the three CreateMap lines became simple one-liners.

Registration in Program.cs:
  builder.Services.AddAutoMapper(typeof(Program).Assembly)
  This tells AutoMapper to scan the entire project for any class that inherits from
  Profile (which is what ProductMappingProfile does) and register it automatically.
  You never have to list profiles manually.


JOB 3: Service Layer
---------------------

Files added:
  Services/IProductService.cs
  Services/ProductService.cs

Files changed:
  Program.cs (registered the service)

Why a service layer:
  Before this job, the controller was calling the repository directly. That works for
  simple CRUD, but in real projects you need a place for business logic - rules like
  "before saving a product, check if the category is valid" or "when a product is
  deleted, also clean up related data". This logic does not belong in the controller
  (too fat) or the repository (wrong responsibility).

  The pattern is: Controller -> Service -> Repository
  Each layer has one job:
    Controller: receive HTTP request, return HTTP response
    Service:    business logic, orchestration, mapping between DTOs and models
    Repository: talk to the database

IProductService vs ProductService:
  IProductService is the interface - the contract. It defines method signatures but
  no implementation.
  ProductService is the implementation - the actual code.

  The controller depends on IProductService, not on ProductService directly. This
  is called programming to an interface. It means if you ever want to swap out the
  implementation (e.g. replace the database with a mock for testing), you only change
  the registration in Program.cs, not the controller code.

What ProductService does with AutoMapper:
  GetAllProductsAsync:  asks repository for Product list, maps to ProductResponseDto list
  GetProductByIdAsync:  asks repository for one Product, maps to ProductResponseDto
  CreateProductAsync:   maps CreateProductDto to Product, saves it, maps result back to dto
  UpdateProductAsync:   fetches existing Product, maps DTO onto it (updating fields in-place),
                        saves, maps result back to dto
  DeleteProductAsync:   delegates directly to repository (no mapping needed)

Registration:
  builder.Services.AddScoped<IProductService, ProductService>()
  AddScoped means: create one instance per HTTP request. Every request gets a fresh
  ProductService. This is the correct lifetime for services that use a DbContext,
  because DbContext itself is also scoped.


JOB 4: Update Controller
-------------------------

Files changed:
  Controllers/ProductsController.cs

What changed:
  Before: the controller injected IProductRepository and returned Product entities
  After:  the controller injects IProductService and returns/accepts DTOs only

  The controller now has no knowledge of the repository, AutoMapper, the database,
  or the Product entity at all. It only knows about DTOs and the service.

  This is the correct pattern. A controller should be thin - its only job is to:
    1. Accept an HTTP request
    2. Call the appropriate service method
    3. Return an HTTP response with the correct status code

  All logic (mapping, validation, database calls) happens in the layers below it.

Why CreatedAtAction on POST:
  return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct)
  This returns a 201 Created response (not 200 OK) with:
  - A Location header pointing to the URL of the newly created resource
    (e.g. /api/products/7)
  - The created product DTO as the response body
  This is the HTTP standard for a successful POST.


JOB 5: FluentValidation
------------------------

Files added:
  Validators/CreateProductDtoValidator.cs
  Validators/UpdateProductDtoValidator.cs

Files changed:
  PinoyPantry.API.csproj (added FluentValidation.AspNetCore package)
  Program.cs (registered FluentValidation)

Why FluentValidation:
  Without it, a user can POST { "name": "", "price": -500 } and your database will
  happily save it. You could write if checks in the controller, but that mixes concerns
  and gets messy.

  FluentValidation lets you define rules in a dedicated validator class. ASP.NET runs
  the validator automatically before your controller action fires. If validation fails,
  the user gets a 400 Bad Request with a detailed list of what is wrong - without you
  writing a single if statement in the controller.

How the validators work:
  Both validators inherit from AbstractValidator<T> where T is the DTO being validated.
  Inside the constructor, you write RuleFor() chains:

  RuleFor(x => x.Name).NotEmpty().WithMessage("Product name is required.")
  This means: for the Name property, it must not be empty. If it is empty, the error
  message "Product name is required." is returned.

  Rules added:
    Name:          required, max 100 characters
    Description:   max 500 characters (not required, can be empty)
    Price:         must be greater than 0 (you cannot have a free or negative price)
    Category:      required
    StockQuantity: must be 0 or more (cannot be negative)

Registration in Program.cs:
  builder.Services.AddFluentValidationAutoValidation()
    This hooks FluentValidation into ASP.NET's model validation pipeline so it runs
    automatically.
  builder.Services.AddValidatorsFromAssemblyContaining<Program>()
    This scans the assembly for all AbstractValidator classes and registers them.
    You never have to list validators manually.


JOB 6: Swagger
---------------

Files changed:
  PinoyPantry.API.csproj (added Swashbuckle.AspNetCore package)
  Program.cs (registered and enabled Swagger UI)

What Swagger does:
  Swagger (via Swashbuckle) reads your controllers, action methods, and DTO classes
  and automatically generates interactive API documentation. When you run the app in
  development, you can visit /swagger in the browser and see all your endpoints, what
  they expect, what they return, and test them directly without Postman.

What was added in Program.cs:
  builder.Services.AddSwaggerGen() registers the Swagger generator with a title,
  version, and description.

  app.UseSwagger() enables the raw JSON endpoint at /swagger/v1/swagger.json
  app.UseSwaggerUI() enables the interactive HTML page at /swagger

  Both are inside if (app.Environment.IsDevelopment()) so they only appear in
  development, not in production. You do not want to expose your API docs publicly
  in a production app.


JOB 7: Fix CORS
----------------

Files changed:
  Program.cs (changed origin URL)

What was wrong:
  The original CORS policy had http://localhost:7136 as the allowed origin. This was
  wrong - the React frontend runs on http://localhost:3000 (Vite's default port).
  CORS blocked every request from the frontend.

What CORS is:
  Browsers enforce a Same-Origin Policy by default. A web page from localhost:3000
  cannot make HTTP requests to localhost:7000 (the API) unless the API explicitly
  says "I allow requests from localhost:3000". This is CORS - Cross-Origin Resource
  Sharing. The server sets the allowed origins, and the browser checks this before
  allowing the request through.

What was changed:
  policy.WithOrigins("http://localhost:3000")


JOB 8: Fix Product Model Typo and Migration
--------------------------------------------

Files changed:
  Models/Product.cs (renamed ImageUrll to ImageUrl, added decimal precision)
  Mappings/ProductMappingProfile.cs (removed .ForMember() calls, simplified to 3 lines)
  Repositories/ProductRepository.cs (updated ImageUrll references to ImageUrl)
  Migrations/ (two new migration files added)

The typo:
  The original model had public string ImageUrll - double L at the end. This typo
  leaked into every layer that touched the model. The DTO had ImageUrl (correct),
  so AutoMapper needed .ForMember() to bridge the naming gap.

  Fixing the model to ImageUrl meant:
    1. All names now match between Model and DTO
    2. The .ForMember() workarounds in the profile were removed
    3. The repository's manual field copy was updated

Decimal precision:
  EF Core warned that the decimal Price field had no explicit SQL Server type. Without
  specifying, SQL Server picks a default precision that may silently truncate values
  like 2.9999 to 3.00. The fix was to add [Column(TypeName = "decimal(18,2)")] to
  the Price property. This means: 18 total digits, 2 after the decimal point.

Migrations:
  Phase1_FixImageUrlAndSeedData - renamed the column in SQL and inserted seed data
  Phase1_FixPriceColumnType     - updated the column type for Price

  To apply migrations to your database, run in the Package Manager Console:
    Update-Database
  Or in the terminal:
    dotnet ef database update


JOB 9: Pagination and Filtering
---------------------------------

Files added:
  DTOs/ProductQueryParams.cs
  DTOs/PagedResult.cs

Files changed:
  Repositories/IProductRepository.cs (updated GetAll signature)
  Repositories/ProductRepository.cs  (implemented filtering and pagination)
  Services/IProductService.cs        (updated GetAll to return PagedResult)
  Services/ProductService.cs         (builds PagedResult from repository data)
  Controllers/ProductsController.cs  (accepts [FromQuery] ProductQueryParams)

Why pagination:
  GET /api/products returning all products works fine with 20 products. With 2000,
  you are sending 2000 rows over the network every time someone loads the page. That
  is slow for the user and wastes server resources. Pagination returns only a "page"
  of results at a time.

ProductQueryParams - what the frontend sends as query string:
  Page:     which page to load (default 1)
  Limit:    how many items per page (default 12)
  Category: optional filter by category (e.g. "Snacks")
  Search:   optional text search across Name and Description

  Example URL: GET /api/products?page=2&limit=12&category=Condiments&search=soy

PagedResult<T> - what the API returns:
  Data:       the list of items for this page
  TotalCount: total number of items matching the filter (before pagination)
  Page:       current page number
  Limit:      items per page
  TotalPages: calculated automatically from TotalCount and Limit

  Example response:
    { "data": [...12 products...], "totalCount": 47, "page": 1, "limit": 12, "totalPages": 4 }

How the repository implements it:
  1. Start with IQueryable<Product> - this is a query that has not run yet
  2. Apply category filter with .Where() if provided
  3. Apply search filter with .Where() if provided
  4. Run CountAsync() to get the total count before pagination
  5. Apply .Skip() and .Take() for pagination, then .ToListAsync()

  Skip and Take:
    Page 1, Limit 12: Skip(0).Take(12)  - items 1-12
    Page 2, Limit 12: Skip(12).Take(12) - items 13-24
    Page 3, Limit 12: Skip(24).Take(12) - items 25-36
    Formula: Skip((page - 1) * limit).Take(limit)

  The key insight is that CountAsync() and ToListAsync() are two separate database
  queries. The Count query does not load any data - it just counts. EF Core translates
  these to:
    SELECT COUNT(*) FROM Products WHERE ...
    SELECT TOP 12 * FROM Products WHERE ... ORDER BY ... OFFSET 0 ROWS


JOB 10: Global Exception Handling Middleware
---------------------------------------------

Files added:
  Middleware/ExceptionHandlingMiddleware.cs

Files changed:
  Program.cs (app.UseMiddleware<ExceptionHandlingMiddleware>() added first in pipeline)

Why global exception handling:
  Without it, if something crashes in your service or repository (e.g. database is
  down, null reference exception), ASP.NET returns a raw 500 error with a full stack
  trace - including file paths, line numbers, and internal code details. That is a
  security risk in production because it reveals your system internals.

  A try/catch in every controller method would work but is repetitive and easy to
  forget. One middleware handles all unhandled exceptions in one place.

How the middleware works:
  Middleware is code that runs on every HTTP request in a pipeline. Each middleware
  can do work before and after the next piece of middleware.

  ExceptionHandlingMiddleware wraps the rest of the pipeline in a try/catch:
    try { await _next(context); }  <- runs all other middleware and the controller
    catch (Exception ex) { ... }  <- catches anything that was not handled

  When something crashes, instead of the raw error, the client gets:
    { "status": 500, "message": "An unexpected error occurred.", "traceId": "abc123" }

  The traceId is useful - a developer can search the server logs for that ID to find
  the full error details, while the client only sees a safe message.

  The middleware is added first in the pipeline with app.UseMiddleware<...>() before
  all other app.Use calls. This ensures it catches exceptions from every other layer.

  The actual error is still logged with _logger.LogError() so developers can see it
  in the console or log file.


JOB 11: Seed Data
------------------

Files changed:
  Data/ApplicationDBContext.cs (added OnModelCreating with HasData)

What seed data is:
  Seed data is a set of records that EF Core inserts into the database automatically
  when the migration runs and the table is empty. It means anyone who clones this
  project and runs Update-Database gets a working database with real products
  immediately, without manually inserting records.

How it works:
  Override OnModelCreating in ApplicationDBContext:
    modelBuilder.Entity<Product>().HasData(...)
  Pass a list of Product objects with hardcoded Ids. The Ids must be hardcoded because
  EF Core needs to know exactly which records to insert (and to detect if they change).

  10 Filipino pantry products were seeded across several categories:
  Noodles, Condiments, Soups & Mixes, Canned Goods, Snacks, Dairy.

  The seed data uses a fixed CreatedAt date (new DateTime(2024, 1, 1, ...)) instead
  of DateTime.UtcNow. This is important - if you used UtcNow, EF Core would detect
  a "change" every time you regenerate migrations because the timestamp would be
  different, causing unnecessary migration churn.

To apply the seed data:
  Run in Package Manager Console: Update-Database
  Or in terminal: dotnet ef database update
  The seed records are part of the Phase1_FixImageUrlAndSeedData migration.


WHAT CHANGED IN PROGRAM.CS ACROSS ALL JOBS
--------------------------------------------

Here is what Program.cs looks like after all Phase 1 jobs, and why each line is there:

  builder.Services.AddControllers()
    Registers all controllers. They are discovered automatically - no need to list them.

  builder.Services.AddEndpointsApiExplorer()
    Enables Swagger to discover your API endpoints.

  builder.Services.AddSwaggerGen(...)
    Registers the Swagger document generator with title, version, description.

  builder.Services.AddDbContext<ApplicationDBContext>(...)
    Registers the EF Core DbContext with the SQL Server connection string.
    Scoped lifetime (one per request) is the default and correct for DbContext.

  builder.Services.AddScoped<IProductRepository, ProductRepository>()
    Registers the repository. When something asks for IProductRepository, it gets
    a new ProductRepository. Scoped = one per request.

  builder.Services.AddScoped<IProductService, ProductService>()
    Same pattern for the service layer.

  builder.Services.AddAutoMapper(typeof(Program).Assembly)
    Scans the assembly for Profile classes and registers them. No manual listing needed.

  builder.Services.AddFluentValidationAutoValidation()
    Hooks FluentValidation into ASP.NET's model validation pipeline.

  builder.Services.AddValidatorsFromAssemblyContaining<Program>()
    Scans the assembly for AbstractValidator classes and registers them.

  builder.Services.AddCors(...)
    Configures the CORS policy allowing the React frontend (localhost:3000) to call the API.

  app.UseMiddleware<ExceptionHandlingMiddleware>()
    Adds global exception handling. Must be FIRST so it catches exceptions from everything.

  if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
    Enables Swagger UI only in development. Not exposed in production.

  app.UseHttpsRedirection()
    Redirects HTTP requests to HTTPS.

  app.UseCors("AllowReactApp")
    Applies the CORS policy to all requests. Must come before UseAuthorization.

  app.UseAuthorization()
    Enables authorization middleware (needed even if not using auth yet).

  app.MapControllers()
    Maps all controller routes. This is what makes GET /api/products work.


DEPENDENCY INJECTION RULES
----------------------------

You register a type in Program.cs when:
  - It has a constructor that needs dependencies injected (e.g. ProductService needs
    IProductRepository and IMapper)
  - It is used by something else via an interface

You do NOT register:
  - Controllers: registered automatically by AddControllers()
  - Models/Entities (Product): just plain C# classes, not injected anywhere
  - DTOs: just plain C# classes, not injected anywhere
  - AutoMapper profiles: registered automatically by AddAutoMapper()
  - FluentValidation validators: registered automatically by AddValidatorsFromAssemblyContaining()
  - DbContext: registered via AddDbContext(), not manually

The three lifetime options:
  AddSingleton:  one instance for the entire application lifetime. Use for stateless
                 services that are expensive to create (e.g. configuration, caching).
  AddScoped:     one instance per HTTP request. Use for services and repositories that
                 use DbContext, because DbContext is also scoped.
  AddTransient:  a new instance every time it is requested. Use for lightweight,
                 stateless utility services.

  In this project, both IProductRepository and IProductService use AddScoped.


COMPLETE FILE STRUCTURE AFTER PHASE 1
---------------------------------------

Controllers/
  ProductsController.cs       - thin controller, uses IProductService, returns DTOs

Data/
  ApplicationDBContext.cs     - EF Core DbContext, DbSet<Product>, seed data

DTOs/
  CreateProductDto.cs         - fields accepted when creating a product
  UpdateProductDto.cs         - fields accepted when updating a product
  ProductResponseDto.cs       - fields returned in API responses
  ProductQueryParams.cs       - query string parameters for filtering and pagination
  PagedResult.cs              - generic paged response wrapper

Mappings/
  ProductMappingProfile.cs    - AutoMapper rules (Product <-> DTOs)

Middleware/
  ExceptionHandlingMiddleware.cs - catches unhandled exceptions, returns clean JSON

Migrations/
  (initial migration files)
  Phase1_FixImageUrlAndSeedData.cs
  Phase1_FixPriceColumnType.cs

Models/
  Product.cs                  - database entity, maps to SQL table

Repositories/
  IProductRepository.cs       - interface (contract)
  ProductRepository.cs        - implementation (EF Core queries with pagination)

Services/
  IProductService.cs          - interface (contract)
  ProductService.cs           - implementation (business logic, AutoMapper usage)

Validators/
  CreateProductDtoValidator.cs - FluentValidation rules for create
  UpdateProductDtoValidator.cs - FluentValidation rules for update

Program.cs                    - app startup, all service registrations
appsettings.json              - connection strings, logging config
