# Phase 1 & 2 — How Everything Works (From Scratch)

This file explains what actually happens when you run the backend and frontend.
Read this top to bottom. It follows the real execution order.

---

## Part 1 — The .NET API Starts Up

### Step 1: You click Run in Visual Studio

The entry point of the entire .NET application is `Program.cs`.
There is no `Main()` method you can see — .NET 6+ hides it, but it exists behind the scenes.
The very first line that runs is:

```csharp
var builder = WebApplication.CreateBuilder(args);
```

This creates a "builder" object. Think of it as a configuration box.
You are telling .NET: "I am about to describe what this app needs before it starts."
Nothing is running yet. You are just registering things.

---

### Step 2: Registering Services (Dependency Injection)

The next block of code in `Program.cs` is a series of `builder.Services.Add...()` calls.

**What is a Service?**
In .NET, a "service" is any class that another class needs to do its job.
For example, `ProductsController` needs `IProductService`. That's a dependency.
DI (Dependency Injection) means: instead of the controller creating `ProductService` itself,
the framework creates it and hands it over automatically.

You have to tell the framework what exists. That is what these lines do:

```csharp
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
```

Reading this: "Whenever anyone asks for `IProductRepository`, give them a `ProductRepository`."

**What does Scoped mean?**
There are three lifetimes:
- Singleton — one instance for the entire app lifetime. Everyone shares it.
- Scoped — one instance per HTTP request. Created when a request comes in, destroyed when it ends.
- Transient — a new instance every single time it is asked for.

`Scoped` is the right choice for database-related services because each HTTP request
should get its own fresh connection and not share state with other requests.

**The full list of what gets registered and why:**

```csharp
builder.Services.AddControllers();
```
Tells .NET that this app uses controllers (classes with [ApiController] attribute).
Without this, `ProductsController` would never be found.

```csharp
builder.Services.AddDbContext<ApplicationDBContext>(...);
```
Registers the database context. `ApplicationDBContext` is the class that talks to SQL Server.
This also reads the connection string from `appsettings.json`:
  Server=localhost\SQLEXPRESS01;Database=PinoyPantryDb;...

```csharp
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
```
Registers your repository and service. The controller will ask for `IProductService`,
and .NET knows to create `ProductService` to satisfy that.

```csharp
builder.Services.AddAutoMapper(typeof(Program).Assembly);
```
Registers AutoMapper. It scans the entire project for any class that extends `Profile`
(that is `ProductMappingProfile`) and loads all the mapping rules.

```csharp
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```
Registers FluentValidation. It scans the project for any class that extends
`AbstractValidator<T>` (that is `CreateProductDtoValidator` and `UpdateProductDtoValidator`)
and hooks them into the request pipeline automatically.
Now, whenever a POST or PUT request comes in with a body, validation runs before
the controller method even executes.

```csharp
builder.Services.AddCors(...);
```
Registers the CORS policy named "AllowReactApp".
CORS is a browser security rule. Browsers block JavaScript from calling a different
origin (different port = different origin) unless the server explicitly allows it.
Your React app is on port 3001. Your API is on port 7136. Without CORS, the browser
would block every fetch() call from the frontend.
Registering here just defines the rule. It is activated later with app.UseCors().

---

### Step 3: Build the App

```csharp
var app = builder.Build();
```

This line finalises everything above.
The DI container is created. All services are wired up.
After this line, you cannot add more services.

---

### Step 4: The Middleware Pipeline

After `builder.Build()`, you configure how each incoming HTTP request flows through the app.
This is called the middleware pipeline. Each middleware is a layer that wraps the next one.

Think of it like this:

```
Request comes in
  → ExceptionHandlingMiddleware (wraps everything)
    → Swagger (only in Development)
      → HttpsRedirection
        → CORS
          → Authorization
            → Controller (your actual code runs here)
          ← Authorization
        ← CORS
      ← HttpsRedirection
    ← Swagger
  → Response goes back
ExceptionHandlingMiddleware sends response
```

The ORDER of these lines in Program.cs matters. Here is yours:

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();  // First — catches all errors
app.UseSwagger();                                  // Dev only — serves swagger.json
app.UseSwaggerUI(...);                             // Dev only — serves the UI page
app.UseHttpsRedirection();                         // Redirects http:// to https://
app.UseCors("AllowReactApp");                      // Applies the CORS policy
app.UseAuthorization();                            // Checks auth (none yet in this app)
app.MapControllers();                              // Connects routes to controllers
app.Run();                                         // Starts the server, blocks here
```

**ExceptionHandlingMiddleware — why it is first:**
It wraps everything. If any code deeper in the pipeline throws an exception, this
middleware catches it and returns a clean JSON error response instead of a raw crash.
If it were last, it would never catch errors from the other middleware.

**app.Run():**
This is the last line. It starts listening on the port (7136 for HTTPS, 5136 for HTTP).
The program blocks here forever, waiting for incoming requests.

---

## Part 2 — A Request Comes In

### Example: Browser calls GET /api/products?category=Condiments

Let's trace exactly what happens.

---

### Step 1: ExceptionHandlingMiddleware

Every single request hits this first.

```csharp
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);  // Pass to the next middleware
    }
    catch (Exception ex)
    {
        // If anything below crashes, we handle it here
        await HandleExceptionAsync(context, ex, _env.IsDevelopment());
    }
}
```

It calls `_next(context)` — passing the request down the chain.
If nothing crashes, it never does anything visible. It just watches.

---

### Step 2: CORS Middleware

The request has an `Origin: http://localhost:3001` header (added by the browser).
The CORS middleware checks: is `http://localhost:3001` in the allowed origins list?
Yes — you registered both `http://localhost:3000` and `http://localhost:3001`.
It adds `Access-Control-Allow-Origin: http://localhost:3001` to the response header.
The browser sees this header and allows the response through.

---

### Step 3: Routing — MapControllers finds ProductsController

.NET looks at the URL: `GET /api/products`
It checks which controller handles `/api/products`.
`ProductsController` has `[Route("api/[controller]")]` at the top.
`[controller]` is replaced with the class name without the "Controller" suffix → `products`.
So `ProductsController` handles `/api/products`.

The method `GetAllProducts` has `[HttpGet]` — it matches GET with no extra path.
.NET selects this method.

---

### Step 4: FluentValidation (only for POST/PUT)

For a GET request, there is no request body, so FluentValidation does nothing.
For POST/PUT, FluentValidation runs before the controller method.
It takes the request body JSON, deserializes it into `CreateProductDto`,
and runs `CreateProductDtoValidator` rules against it.
If any rule fails (e.g. Name is empty), it returns `400 Bad Request` automatically,
and the controller method never runs at all.

---

### Step 5: DI builds the controller

`ProductsController` needs `IProductService` in its constructor:

```csharp
public ProductsController(IProductService productService)
{
    _productService = productService;
}
```

.NET's DI container sees this and thinks:
- "ProductsController needs IProductService"
- "I have ProductService registered for IProductService"
- "ProductService needs IProductRepository and IMapper"
- "I have ProductRepository registered for IProductRepository"
- "ProductRepository needs ApplicationDBContext"
- "I have ApplicationDBContext registered"

It builds the whole chain automatically, from the bottom up:
ApplicationDBContext → ProductRepository → ProductService → ProductsController

You never called `new ProductService(...)` anywhere in your code.
.NET did it for you because of the registrations in Program.cs.

---

### Step 6: Controller method runs

```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<ProductResponseDto>>> GetAllProducts(
    [FromQuery] ProductQueryParams query)
{
    var result = await _productService.GetAllProductsAsync(query);
    return Ok(result);
}
```

`[FromQuery]` means: read `page`, `limit`, `category`, `search` from the URL query string.
For `GET /api/products?category=Condiments`, the `query` object will have:
- `query.Page = 1` (default)
- `query.Limit = 12` (default)
- `query.Category = "Condiments"`
- `query.Search = null`

It calls `_productService.GetAllProductsAsync(query)`.

---

### Step 7: Service layer

```csharp
public async Task<PagedResult<ProductResponseDto>> GetAllProductsAsync(ProductQueryParams query)
{
    var (products, totalCount) = await _productRepository.GetAllProductsAsync(query);

    return new PagedResult<ProductResponseDto>
    {
        Data = _mapper.Map<IEnumerable<ProductResponseDto>>(products),
        TotalCount = totalCount,
        Page = query.Page,
        Limit = query.Limit
    };
}
```

The service calls the repository to get the raw `Product` model objects from the database.
Then it uses AutoMapper to convert those `Product` objects into `ProductResponseDto` objects.
Why? The `Product` model has `StockQuantity` and `CreatedAt` — internal fields.
The `ProductResponseDto` only has `Id, Name, Description, Price, ImageUrl, Category` — public-safe fields.
AutoMapper copies the matching fields. No sensitive or internal data leaks to the client.

---

### Step 8: Repository and Entity Framework

```csharp
public async Task<(IEnumerable<Product> Products, int TotalCount)> GetAllProductsAsync(
    ProductQueryParams query)
{
    var products = _context.Products.AsQueryable();

    if (!string.IsNullOrWhiteSpace(query.Category))
        products = products.Where(p => p.Category == query.Category);

    var totalCount = await products.CountAsync();

    var paged = await products
        .OrderBy(p => p.Id)
        .Skip((query.Page - 1) * query.Limit)
        .Take(query.Limit)
        .ToListAsync();

    return (paged, totalCount);
}
```

`_context.Products.AsQueryable()` does NOT hit the database yet.
It builds a query description in memory (an IQueryable object).

`.Where(p => p.Category == query.Category)` adds a filter to the query.
Still no database hit.

`CountAsync()` — NOW it hits the database. Runs: SELECT COUNT(*) WHERE Category = 'Condiments'

`Skip().Take().ToListAsync()` — hits the database again. Runs:
SELECT * FROM Products WHERE Category = 'Condiments' ORDER BY Id OFFSET 0 ROWS FETCH NEXT 12 ROWS ONLY

Results come back as a `List<Product>` — real C# objects with data.

---

### Step 9: AutoMapper converts Product → ProductResponseDto

Back in the service:
```csharp
Data = _mapper.Map<IEnumerable<ProductResponseDto>>(products)
```

AutoMapper uses `ProductMappingProfile`:
```csharp
CreateMap<Product, ProductResponseDto>();
```

Because all field names match between `Product` and `ProductResponseDto`,
AutoMapper copies them automatically:
- Product.Id → ProductResponseDto.Id
- Product.Name → ProductResponseDto.Name
- Product.Price → ProductResponseDto.Price
- Product.StockQuantity → NOT copied (not in ProductResponseDto) ← intentional

---

### Step 10: Response sent back

The controller returns:
```csharp
return Ok(result);
```

`Ok()` wraps the result in a 200 HTTP response.
.NET serializes the `PagedResult<ProductResponseDto>` to JSON:

```json
{
  "data": [
    { "id": 2, "name": "Datu Puti Sukang Maasim", "price": 2.99, ... },
    { "id": 3, "name": "Silver Swan Soy Sauce", "price": 3.49, ... }
  ],
  "totalCount": 4,
  "page": 1,
  "limit": 12,
  "totalPages": 1
}
```

This JSON travels back through every middleware layer in reverse order,
and arrives at the browser.

---

## Part 3 — The React Frontend Starts

### Step 1: npm run dev starts Vite

Vite is the development server and build tool for the React project.
When you run `npm run dev`, Vite:
1. Reads `vite.config.ts`
2. Reads `.env` — loads `VITE_API_URL=https://localhost:7136`, `VITE_USE_MOCK_DATA=false`
3. Compiles your TypeScript/TSX files
4. Starts a local HTTP server on port 3000 (or 3001 if 3000 is taken)
5. Opens the browser

The `.env` values are baked into the compiled JavaScript at build time.
They are accessible as `import.meta.env.VITE_API_URL` anywhere in your code.
Important: only variables starting with `VITE_` are exposed. Others are kept private.

---

### Step 2: index.html loads

The browser receives `index.html`:
```html
<div id="root"></div>
<script type="module" src="/src/main.tsx"></script>
```

The browser runs `main.tsx` first.

---

### Step 3: main.tsx — the React entry point

```typescript
import { createRoot } from "react-dom/client";
import App from "./App.tsx";

createRoot(document.getElementById("root")!).render(<App />);
```

This takes the `<div id="root">` from index.html and mounts the `App` component inside it.
The entire React app lives inside that one div.

---

### Step 4: App.tsx renders the layout

`App` renders the router, header, footer, and routes:

```tsx
<BrowserRouter>
  <CartProvider>
    <Header ... />
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/category/:slug" element={<CategoryPage />} />
      ...
    </Routes>
    <Footer />
  </CartProvider>
</BrowserRouter>
```

`BrowserRouter` watches the URL. When the URL is `/`, it renders `<HomePage />`.
When the URL is `/category/condiments`, it renders `<CategoryPage />`.

---

### Step 5: HomePage loads — the first API call

`HomePage` renders and somewhere calls `useFeaturedProducts()`.

The `useFeaturedProducts` hook calls:
```typescript
ProductService.getFeaturedProducts()
```

`productService.ts` checks:
```typescript
const USE_MOCK_DATA = import.meta.env.VITE_USE_MOCK_DATA === 'true'; // false
const USE_API = !USE_MOCK_DATA && !!import.meta.env.VITE_API_URL;    // true
```

Since `USE_API` is true, it calls:
```typescript
ApiProductService.getFeaturedProducts()
```

Which calls:
```typescript
fetch(`https://localhost:7136/api/products?page=1&limit=6`)
```

---

### Step 6: The browser makes the HTTP request

The browser sends:
```
GET https://localhost:7136/api/products?page=1&limit=6
Origin: http://localhost:3001
```

The `Origin` header is added automatically by the browser.
The .NET API receives it, runs through the middleware pipeline (steps above),
and returns the JSON response.

---

### Step 7: The response is mapped

Back in `apiProductService.ts`:
```typescript
const result: ApiPagedResult<ApiProduct> = await response.json();

return {
    products: result.data.map(mapApiProduct),
    ...
};
```

`mapApiProduct` converts the API shape to the frontend shape:
```typescript
function mapApiProduct(p: ApiProduct): Product {
    return {
        id: String(p.id),     // number 1 → string "1"
        name: p.name,
        image: p.imageUrl,    // imageUrl → image (field rename)
        price: p.price,
        category: p.category,
        inStock: true,
    };
}
```

---

### Step 8: React re-renders with the data

The hook (`useFeaturedProducts`) stores the products in React state:
```typescript
setProducts(data);
setLoading(false);
```

React sees the state changed and re-renders the component.
The loading skeleton disappears. Product cards appear with real data from your SQL Server database.

---

## Summary — The Full Chain

```
Visual Studio runs Program.cs
  → DI container built (Controller, Service, Repository, DbContext all wired up)
  → Middleware pipeline configured (Exception → CORS → Controllers)
  → Server listens on port 7136

Browser opens http://localhost:3001
  → Vite serves index.html
  → main.tsx mounts <App />
  → App.tsx renders <HomePage />
  → useProducts hook calls ProductService.getFeaturedProducts()
  → USE_API=true → ApiProductService.getFeaturedProducts()
  → fetch("https://localhost:7136/api/products?page=1&limit=6")
  → Browser adds Origin: http://localhost:3001 header

.NET API receives the request
  → ExceptionHandlingMiddleware wraps it
  → CORS allows http://localhost:3001
  → Router finds ProductsController.GetAllProducts()
  → DI injects ProductService into the controller
  → DI injects ProductRepository into ProductService
  → DI injects ApplicationDBContext into ProductRepository
  → Repository runs SQL query against SQL Server
  → Product rows returned as C# objects
  → Service maps Product → ProductResponseDto via AutoMapper
  → Controller returns 200 OK with JSON

Browser receives the JSON
  → apiProductService maps ApiProduct → Product (imageUrl→image, id number→string)
  → React state updated
  → Component re-renders with real product cards
```

---

## One Concept Per Keyword

**DI / Dependency Injection** — you declare what a class needs in its constructor.
The framework creates and provides those dependencies automatically.
You never call `new ProductService()` manually.

**Scoped** — one instance of a class per HTTP request. Shared within the same request,
thrown away when the request ends. Right for database work.

**Middleware** — a layer that wraps the request/response pipeline.
Each one can inspect, modify, or short-circuit the request before passing it on.

**IProductRepository vs ProductRepository** — the interface defines the contract (what methods exist).
The class implements it (how those methods work).
The controller only knows about the interface — it never cares which concrete class it gets.
This makes it easy to swap the implementation later (e.g. for testing, or switching databases).

**DTO (Data Transfer Object)** — a plain class used only for moving data in or out of the API.
It is not the real database model. You choose exactly which fields to expose.

**AutoMapper** — reads your `CreateMap<A, B>()` rules and copies matching fields automatically.
You don't write `b.Name = a.Name` for every field.

**CORS** — a browser security rule. The browser blocks JavaScript from calling a different origin
unless the server explicitly says it is allowed.
Your API says: "I allow requests from http://localhost:3001."

**VITE_API_URL** — an environment variable injected at build time by Vite.
In code: `import.meta.env.VITE_API_URL`.
In `.env`: `VITE_API_URL=https://localhost:7136`.
Change this one value to point to Azure when you deploy. No code changes needed.

**fetch()** — the browser's built-in function to make HTTP requests.
`await fetch(url)` sends the request and waits for the response.
`await response.json()` reads the response body and parses it from JSON to a JavaScript object.
