# Image Upload Feature — Full Explanation (From Scratch)

This document explains everything about how product images work in PinoyPantry.
Read it top to bottom. It covers: why we need cloud storage, how we set it up,
every line of backend code, every line of frontend code, the errors we hit,
and how the full flow works end to end.

---

## Part 1 — The Problem: Where Do Product Images Go?

### The Old Way: Storing Images in a Folder

If you have worked with PHP or .NET MVC before, you probably stored images like this:

```
wwwroot/
  images/
    products/
      datu-puti.jpg
      lucky-me.jpg
```

Then in your HTML you reference them:

```html
<img src="/images/products/datu-puti.jpg" />
```

This works fine for small projects. The images live on the same server as your code.

### Why That Breaks in the Cloud

When you deploy to Azure App Service (or AWS, or any cloud), your app runs on a
temporary server. Azure can restart it, move it to a different machine, or scale it
to multiple machines. When that happens:

- Files saved to the local disk are **lost**.
- If you have 2 servers running your API, a file saved on server A is
  **invisible** to server B.

So in the real world (especially e-commerce), you store images in a separate
cloud storage service that is:

1. **Permanent** — files survive restarts and redeployments
2. **Fast** — optimized for serving files to browsers
3. **Cheap** — you only pay for the storage you use
4. **Accessible** — a public URL anyone can load

### Cloud Storage Options

| Service              | Provider  | Free Tier        |
|----------------------|-----------|------------------|
| Azure Blob Storage   | Microsoft | 5 GB free        |
| Amazon S3            | AWS       | 5 GB for 12 months |
| Google Cloud Storage | Google    | 5 GB free        |

We chose **Azure Blob Storage** because we already use Azure for everything else.
The concept is the same as Amazon S3 — they are practically identical in how they work.

---

## Part 2 — What Is Azure Blob Storage?

### Key Terms

**Storage Account** — A top-level Azure resource that holds all your storage.
Think of it like a Google Drive account. Ours is called `pinoypantrystorage`.

**Container** — A folder inside the Storage Account. You can have multiple
containers for different purposes. Ours is called `product-images`.

**Blob** — A single file inside a container. "Blob" stands for Binary Large Object.
An uploaded image is a blob.

**Connection String** — A long secret string that lets your code authenticate
with the Storage Account. It looks like:

```
DefaultEndpointsProtocol=https;AccountName=pinoypantrystorage;AccountKey=HU0CzL...==;EndpointSuffix=core.windows.net
```

This is like a password. You never put it in code. You store it as an
environment variable.

### How We Set It Up in Azure Portal

Step by step, here is what we did:

1. **Go to Azure Portal** → Search "Storage accounts" → Click "Create"
2. **Resource group:** `pinoypantry-rg` (same group as everything else)
3. **Storage account name:** `pinoypantrystorage` (must be globally unique, lowercase, no hyphens)
4. **Region:** Australia East (same region as our API and database)
5. **Performance:** Standard (cheaper, fine for images)
6. **Redundancy:** LRS (Locally Redundant Storage — cheapest option, stores 3 copies in the same data center)
7. Click **Review + Create** → **Create**

After the storage account was created:

8. Open `pinoypantrystorage` → **Containers** → Click **+ Container**
9. **Name:** `product-images`
10. **Anonymous access level:** "Blob (anonymous read access for blobs only)"
    - This means anyone with the URL can view the image (like a public image on a website)
    - They CANNOT list all files or see the container itself
    - This is the standard approach for product images on e-commerce sites
11. Click **Create**

Then to get the connection string:

12. Open `pinoypantrystorage` → **Access keys** (under Security + networking)
13. Click **Show** next to key1 → Copy the **Connection string**
14. This is the long string starting with `DefaultEndpointsProtocol=https;...`

### How We Stored the Connection String

Locally, you could put it in `appsettings.json`, but we need it in Azure too.

**In Azure App Service (for the live API):**
1. Go to `pinoypantry-api` App Service → **Environment variables**
2. Click **+ Add**
3. Name: `AzureBlobStorageConnection`
4. Value: (paste the full connection string)
5. Click **Apply** → **Confirm**

**Why the flat name?**
We originally tried `AzureBlobStorage__ConnectionString` (using the .NET
convention of double underscores to represent nested config sections). But
Azure App Service F1 (free tier) does NOT allow double underscores in
environment variable names. It gave this error:

```
AppSetting with name 'AzureBlobStorage__ConnectionString' is not allowed.
```

So we changed it to a flat name: `AzureBlobStorageConnection`. This is a
workaround specific to the free tier. The paid tiers don't have this restriction.

---

## Part 3 — The Backend Code (Step by Step)

The image upload involves 4 files in the .NET API. Let's go through each one.

### 3.1 — The Interface: IBlobStorageService.cs

**File:** `Services/IBlobStorageService.cs`

```csharp
namespace PinoyPantry.API.Services;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteImageAsync(string blobUrl);
}
```

**What this does:**

This is just a contract (interface). It says "any class that implements me must
provide two methods." It does NOT contain any actual logic.

- `UploadImageAsync` — Takes a file stream, the file name, and its content type
  (like "image/jpeg"). Returns a `string` which is the public URL of the uploaded image.
- `DeleteImageAsync` — Takes a blob URL and deletes that file from storage.

**Why an interface?**

Same pattern as `IProductRepository`. By coding against an interface:
- The controller doesn't know or care HOW the upload works
- You could swap Azure Blob Storage for Amazon S3 without changing the controller
- You can mock it in unit tests

### 3.2 — The Implementation: BlobStorageService.cs

**File:** `Services/BlobStorageService.cs`

```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PinoyPantry.API.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureBlobStorageConnection"];
        var containerName = "product-images";
        _containerClient = new BlobContainerClient(connectionString, containerName);
    }

    public async Task<string> UploadImageAsync(Stream fileStream, string fileName, string contentType)
    {
        var blobName = $"products/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });

        return blobClient.Uri.ToString();
    }

    public async Task DeleteImageAsync(string blobUrl)
    {
        var uri = new Uri(blobUrl);
        var blobName = string.Join("/", uri.Segments.Skip(2)).TrimStart('/');
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
    }
}
```

**Let's break this down line by line:**

**The Constructor:**

```csharp
public BlobStorageService(IConfiguration configuration)
{
    var connectionString = configuration["AzureBlobStorageConnection"];
    var containerName = "product-images";
    _containerClient = new BlobContainerClient(connectionString, containerName);
}
```

- `IConfiguration configuration` — .NET automatically injects this. It reads
  from `appsettings.json` locally, or from Azure App Service environment
  variables in production. Same system used for the database connection string.
- `configuration["AzureBlobStorageConnection"]` — Reads the flat connection
  string we added to Azure.
- `BlobContainerClient` — This comes from the `Azure.Storage.Blobs` NuGet
  package. It represents a connection to a specific container in Blob Storage.
  Think of it as: "I'm connected to the `product-images` folder in Azure."

**The Upload Method:**

```csharp
public async Task<string> UploadImageAsync(Stream fileStream, string fileName, string contentType)
{
    var blobName = $"products/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{fileName}";
    var blobClient = _containerClient.GetBlobClient(blobName);

    await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });

    return blobClient.Uri.ToString();
}
```

Line by line:

1. `var blobName = $"products/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{fileName}";`

   This creates a unique path for the file. For example, if you upload `datu-puti.jpg`
   at timestamp 1710345600, the blob name becomes:
   ```
   products/1710345600_datu-puti.jpg
   ```
   Why the timestamp? If you upload two files with the same name, they won't
   overwrite each other. Each upload gets a unique name.

2. `var blobClient = _containerClient.GetBlobClient(blobName);`

   Creates a reference to that specific file location in Azure. The file
   doesn't exist yet — this is just a pointer.

3. `await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });`

   This is the actual upload. It sends the file bytes to Azure.
   The `BlobHttpHeaders` sets the Content-Type header so when a browser
   requests this image, Azure serves it as `image/jpeg` (not as a
   generic download).

4. `return blobClient.Uri.ToString();`

   Returns the public URL. This looks like:
   ```
   https://pinoypantrystorage.blob.core.windows.net/product-images/products/1710345600_datu-puti.jpg
   ```
   This URL is permanent. Anyone can open it in a browser and see the image.
   This is what gets saved in the database as the product's `ImageUrl`.

**The Delete Method:**

```csharp
public async Task DeleteImageAsync(string blobUrl)
{
    var uri = new Uri(blobUrl);
    var blobName = string.Join("/", uri.Segments.Skip(2)).TrimStart('/');
    var blobClient = _containerClient.GetBlobClient(blobName);
    await blobClient.DeleteIfExistsAsync();
}
```

This reverses the process. Given a full URL, it extracts the blob name and deletes it.
`DeleteIfExistsAsync` won't throw an error if the file is already gone.

### 3.3 — The Controller: ImageController.cs

**File:** `Controllers/ImageController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly IBlobStorageService _blobService;
    private readonly IProductService _productService;

    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public ImageController(IBlobStorageService blobService, IProductService productService)
    {
        _blobService = blobService;
        _productService = productService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] int? productId)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        if (file.Length > MaxFileSize)
            return BadRequest(new { message = "File size exceeds 5 MB limit." });

        if (!AllowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Only JPEG, PNG, WebP, and GIF images are allowed." });

        using var stream = file.OpenReadStream();
        var imageUrl = await _blobService.UploadImageAsync(stream, file.FileName, file.ContentType);

        if (productId.HasValue)
        {
            var product = await _productService.GetByIdAsync(productId.Value);
            if (product == null)
                return NotFound(new { message = $"Product {productId} not found." });

            await _productService.UpdateImageUrlAsync(productId.Value, imageUrl);
        }

        return Ok(new { imageUrl, message = "Image uploaded successfully." });
    }
}
```

**Let's break it down:**

**The Route:**

`[Route("api/[controller]")]` — Since the class is `ImageController`, the
`[controller]` token becomes `image`. So the base URL is `/api/image`.

`[HttpPost("upload")]` — This method handles POST requests to `/api/image/upload`.

**The Parameters:**

```csharp
public async Task<IActionResult> Upload(IFormFile file, [FromQuery] int? productId)
```

- `IFormFile file` — This is how .NET receives uploaded files. When a browser
  sends a form with a file input, .NET automatically wraps it as `IFormFile`.
  It contains the file's bytes, name, size, and content type.

- `[FromQuery] int? productId` — This comes from the URL query string:
  `/api/image/upload?productId=2`. The `?` after `int` means it's optional.
  If you just want to upload an image without attaching it to a product, you
  can skip this parameter.

**Validation (lines that return BadRequest):**

```csharp
if (file == null || file.Length == 0)
    return BadRequest(new { message = "No file provided." });
```
No file was sent. This happens if you forget to attach a file in Postman or the frontend.

```csharp
if (file.Length > MaxFileSize)
    return BadRequest(new { message = "File size exceeds 5 MB limit." });
```
The file is too big. `MaxFileSize` is `5 * 1024 * 1024` which equals 5,242,880 bytes (5 MB).
This protects your storage from gigantic uploads.

```csharp
if (!AllowedTypes.Contains(file.ContentType))
    return BadRequest(new { message = "Only JPEG, PNG, WebP, and GIF images are allowed." });
```
Only image files are accepted. If someone tries to upload a `.exe` or `.pdf`,
it gets rejected. `AllowedTypes` is a `HashSet<string>` — a fast lookup data
structure. `StringComparer.OrdinalIgnoreCase` means `IMAGE/JPEG` and `image/jpeg`
are treated the same.

**The Upload:**

```csharp
using var stream = file.OpenReadStream();
var imageUrl = await _blobService.UploadImageAsync(stream, file.FileName, file.ContentType);
```

- `file.OpenReadStream()` — Opens the file for reading. The `using` keyword
  means the stream is automatically closed when this method finishes.
- `_blobService.UploadImageAsync(...)` — Calls our `BlobStorageService` which
  uploads to Azure and returns the public URL.

**Attaching to a Product (optional):**

```csharp
if (productId.HasValue)
{
    var product = await _productService.GetByIdAsync(productId.Value);
    if (product == null)
        return NotFound(new { message = $"Product {productId} not found." });

    await _productService.UpdateImageUrlAsync(productId.Value, imageUrl);
}
```

If a `productId` was provided in the query string:
1. Look up the product in the database
2. If it doesn't exist, return 404
3. If it exists, save the image URL to the product's `ImageUrl` field

**The Response:**

```csharp
return Ok(new { imageUrl, message = "Image uploaded successfully." });
```

Returns HTTP 200 with the image URL. The frontend uses this URL to update
the thumbnail immediately.

### 3.4 — The Database Layer

Two methods were added to support saving the image URL:

**IProductRepository.cs** — Added the contract:
```csharp
Task UpdateImageUrlAsync(int id, string imageUrl);
```

**ProductRepository.cs** — The actual database code:
```csharp
public async Task UpdateImageUrlAsync(int id, string imageUrl)
{
    var product = await _context.Products.FindAsync(id);
    if (product != null)
    {
        product.ImageUrl = imageUrl;
        await _context.SaveChangesAsync();
    }
}
```

This finds the product by ID, sets its `ImageUrl` to the new Azure Blob URL,
and saves the change to SQL Server. After this, the `GET /api/products` endpoint
will return the image URL for that product.

**IProductService.cs** — Added:
```csharp
Task<ProductResponseDto?> GetByIdAsync(int id);
Task UpdateImageUrlAsync(int id, string imageUrl);
```

**ProductService.cs** — Calls the repository:
```csharp
public async Task<ProductResponseDto?> GetByIdAsync(int id)
{
    return await GetProductByIdAsync(id);
}

public async Task UpdateImageUrlAsync(int id, string imageUrl)
{
    await _productRepository.UpdateImageUrlAsync(id, imageUrl);
}
```

### 3.5 — Registering in Program.cs

```csharp
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
```

This tells .NET's Dependency Injection: "When any class asks for
`IBlobStorageService`, give them a `BlobStorageService` instance."

We used `AddSingleton` (not `AddScoped`) because `BlobContainerClient` is
thread-safe and reusable. Creating one instance for the entire app lifetime
is efficient — it reuses the same HTTP connection to Azure.

Compare with `AddScoped<IProductRepository, ProductRepository>()` which
creates a new instance per HTTP request. That's needed for database contexts
(which track changes per request), but not for Blob Storage.

---

## Part 4 — The Frontend Code (Step by Step)

### 4.1 — AdminUploadPage.tsx

This is the React page you see at `/admin/upload`.

**Getting the API URL:**

```typescript
const API_URL = import.meta.env.VITE_API_URL || 'https://localhost:7136';
```

`import.meta.env.VITE_API_URL` reads the environment variable from `.env`.
Locally, this points to your Azure API. If not set, it falls back to `localhost`.

**The State:**

```typescript
const [products, setProducts] = useState<Product[]>([]);
const [loading, setLoading] = useState(true);
const [uploading, setUploading] = useState<number | null>(null);
const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
```

- `products` — The array of products fetched from the API.
- `loading` — True while fetching products. Shows "Loading products..." text.
- `uploading` — Holds the product ID that is currently uploading. `null` if nothing
  is uploading. This disables the button and shows "Uploading..." for that specific row.
- `message` — A success or error banner at the top of the page.

**Loading Products:**

```typescript
useEffect(() => {
    fetchProducts();
}, []);

async function fetchProducts() {
    try {
        const res = await fetch(`${API_URL}/api/products?limit=50`);
        const data = await res.json();
        setProducts(data.data || []);
    } catch {
        setMessage({ type: 'error', text: 'Failed to load products.' });
    } finally {
        setLoading(false);
    }
}
```

`useEffect(() => { ... }, [])` runs once when the component first appears on
screen (the empty `[]` means "no dependencies, run only once").

It calls `GET /api/products?limit=50` — the same endpoint you tested in Swagger.
The `?limit=50` ensures we get all products (default might be 10). The response
looks like:

```json
{
  "data": [
    { "id": 1, "name": "Lucky Me Pancit Canton", "imageUrl": "", "category": "Noodles", "price": 1.5 },
    { "id": 2, "name": "Datu Puti Sukang Maasim", "imageUrl": "https://pinoypantrystorage...", ... }
  ],
  "totalCount": 10,
  "page": 1
}
```

We extract `data.data` (the array) and save it to state.

**The Upload Function:**

```typescript
async function handleUpload(productId: number, file: File) {
    setUploading(productId);
    setMessage(null);

    const formData = new FormData();
    formData.append('file', file);

    try {
        const res = await fetch(`${API_URL}/api/image/upload?productId=${productId}`, {
            method: 'POST',
            body: formData,
        });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || 'Upload failed');
        }

        const data = await res.json();
        setMessage({ type: 'success', text: `Image uploaded for product #${productId}` });

        setProducts(prev =>
            prev.map(p => (p.id === productId ? { ...p, imageUrl: data.imageUrl } : p))
        );
    } catch (err: any) {
        setMessage({ type: 'error', text: err.message || 'Upload failed' });
    } finally {
        setUploading(null);
    }
}
```

This is the most important function. Let's trace it step by step:

1. `setUploading(productId)` — Mark this product as "currently uploading."
   The button changes to "Uploading..." and becomes disabled.

2. `const formData = new FormData()` — Creates a FormData object. This is the
   browser's built-in way to send files over HTTP. It's the equivalent of
   selecting "form-data" in Postman's Body tab.

3. `formData.append('file', file)` — Adds the file to the form. The key is
   `'file'` which MUST match the parameter name in the C# controller:
   `IFormFile file`. If you named it `'image'` here, the controller wouldn't
   receive it.

4. `fetch(url, { method: 'POST', body: formData })` — Sends the request.
   Important: we do NOT set `Content-Type` header manually. The browser
   automatically sets it to `multipart/form-data` with the correct boundary
   when it sees a FormData body. If you set `Content-Type` yourself, it breaks.

5. The response from the API is:
   ```json
   { "imageUrl": "https://pinoypantrystorage.blob.core.windows.net/...", "message": "Image uploaded successfully." }
   ```

6. `setProducts(prev => prev.map(p => ...))` — Updates the product in the local
   state array. It finds the product with the matching ID and replaces its
   `imageUrl` with the new URL. This makes the thumbnail update instantly
   without needing to refresh the page.

**The File Picker:**

```typescript
function handleFileSelect(productId: number) {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/jpeg,image/png,image/webp,image/gif';
    input.onchange = (e) => {
        const file = (e.target as HTMLInputElement).files?.[0];
        if (file) handleUpload(productId, file);
    };
    input.click();
}
```

This is a trick to open a file picker without a visible `<input>` element:
1. Create a hidden file input
2. Set accepted file types (same as what the backend validates)
3. When the user picks a file, grab it and call `handleUpload`
4. `.click()` opens the file picker dialog

This is cleaner than having a visible file input on each row.

### 4.2 — The Route in App.tsx

```typescript
import { AdminUploadPage } from './pages/AdminUploadPage';

// Inside <Routes>:
<Route path="/admin/upload" element={<AdminUploadPage />} />
```

And we hide the header/footer on admin pages:

```typescript
const isAdminPage = location.pathname.startsWith('/admin');

{!isLoginPage && !isStatsComparePage && !isHomeRunsPage && !isAdminPage && (
    <Header ... />
)}
```

This gives the admin page a clean, standalone layout.

---

## Part 5 — The CORS Fix

### What Was the Problem?

Vite picks a port dynamically. If port 3000 is busy, it tries 3001, then 3002, etc.
Our original CORS policy only allowed 3000 and 3001:

```csharp
policy.WithOrigins(
    "http://localhost:3000",
    "http://localhost:3001",
    "https://gentle-dune-0c69a8700.6.azurestaticapps.net")
```

When Vite started on port 3002, the browser blocked the API calls because
3002 wasn't in the allowed list.

### The Fix: SetIsOriginAllowed

```csharp
policy.SetIsOriginAllowed(origin =>
{
    if (new Uri(origin).Host == "localhost") return true;
    return origin == "https://gentle-dune-0c69a8700.6.azurestaticapps.net";
})
```

Instead of listing every port, we check dynamically:
- If the request comes from `localhost` (any port) → allow it
- If it comes from our Azure URL → allow it
- Everything else → blocked

This is the real-world approach. In production you would typically
have a list of known origins, but for localhost during development,
allowing any port is standard practice.

---

## Part 6 — The NuGet Package

We installed one new NuGet package:

```
Azure.Storage.Blobs
```

This is the official Microsoft SDK for Azure Blob Storage. It provides:
- `BlobContainerClient` — connects to a container
- `BlobClient` — represents a single file (blob)
- `BlobHttpHeaders` — sets headers like Content-Type

You installed it by running in the PinoyPantry.API folder:

```bash
dotnet add package Azure.Storage.Blobs
```

This added a line to `PinoyPantry.API.csproj`:

```xml
<PackageReference Include="Azure.Storage.Blobs" Version="12.x.x" />
```

Think of NuGet packages like npm packages for .NET. `Azure.Storage.Blobs` is
the equivalent of the `@azure/storage-blob` npm package in the JavaScript world.

---

## Part 7 — The Full Flow (End to End)

Here is exactly what happens when you click "Upload" on the admin page:

```
Step 1: User clicks "Upload" button next to "Datu Puti Sukang Maasim"
        ↓
Step 2: Browser opens file picker dialog
        ↓
Step 3: User selects "datu-puti.jpg" from their PC
        ↓
Step 4: JavaScript creates FormData with the file
        ↓
Step 5: Browser sends HTTP POST to:
        https://pinoypantry-api-xxx.azurewebsites.net/api/image/upload?productId=2
        Body: multipart/form-data containing the image bytes
        ↓
Step 6: Azure App Service receives the request
        ↓
Step 7: .NET routes it to ImageController.Upload() because:
        - It's a POST request
        - The URL matches [HttpPost("upload")]
        ↓
Step 8: .NET parses the multipart body and wraps the file as IFormFile
        ↓
Step 9: ImageController validates:
        - File exists? ✓
        - Under 5 MB? ✓
        - Is an image type? ✓
        ↓
Step 10: ImageController calls _blobService.UploadImageAsync(stream, "datu-puti.jpg", "image/jpeg")
         ↓
Step 11: BlobStorageService creates blob name: "products/1710345600_datu-puti.jpg"
         ↓
Step 12: BlobStorageService uploads the file bytes to Azure Blob Storage
         Azure stores it permanently in the "product-images" container
         ↓
Step 13: BlobStorageService returns the public URL:
         "https://pinoypantrystorage.blob.core.windows.net/product-images/products/1710345600_datu-puti.jpg"
         ↓
Step 14: ImageController calls _productService.UpdateImageUrlAsync(2, "https://pinoypantrystorage...")
         ↓
Step 15: ProductService calls _productRepository.UpdateImageUrlAsync(2, "https://pinoypantrystorage...")
         ↓
Step 16: ProductRepository runs SQL:
         UPDATE Products SET ImageUrl = 'https://pinoypantrystorage...' WHERE Id = 2
         ↓
Step 17: ImageController returns HTTP 200:
         { "imageUrl": "https://pinoypantrystorage...", "message": "Image uploaded successfully." }
         ↓
Step 18: JavaScript receives the response
         ↓
Step 19: React updates the product in state with the new imageUrl
         ↓
Step 20: The thumbnail on the admin page updates instantly (no page reload)
         ↓
Step 21: Now when anyone visits the main site, GET /api/products returns
         this product with its imageUrl, and the <img> tag loads it from Azure Blob Storage
```

---

## Part 8 — Errors We Hit Along the Way

### Error 1: 405 Method Not Allowed (Postman)

When first testing in Postman, we got a 405 error. This happened because
Postman was set to GET instead of POST. The endpoint only accepts POST
(`[HttpPost("upload")]`). Changing the method to POST fixed it.

### Error 2: 400 Bad Request — "The file field is required"

Postman sent the request but the API didn't receive the file. The cause:
in Postman's Body → form-data, the "Key" field type was set to "Text" instead
of "File." You need to click the dropdown next to the key name and change it
from "Text" to "File" so Postman sends it as a file upload.

### Error 3: F1 Tier Double Underscore Restriction

We tried adding `AzureBlobStorage__ConnectionString` as an environment variable
in Azure App Service. Azure rejected it:

```
AppSetting with name 'AzureBlobStorage__ConnectionString' is not allowed.
```

The F1 (free) tier does not allow double underscores in setting names. We
changed the code to use the flat name `AzureBlobStorageConnection` instead.

### Error 4: 500 — Azure SQL "not currently available"

After uploading the file to Blob Storage, the API tried to update the product
in the database but got a 500 error. The Azure SQL free tier "sleeps" when
idle and takes a few seconds to wake up. Resending the request worked.

This is a known limitation of the free/basic Azure SQL tiers. In production,
you would use `EnableRetryOnFailure()` in the database configuration to
automatically retry on transient failures.

---

## Part 9 — How the Image Shows on the Main Website

After uploading an image for a product, this is what happens on the public site:

1. User visits `https://gentle-dune-0c69a8700.6.azurestaticapps.net`
2. React loads and calls `GET /api/products`
3. The API returns products with their `imageUrl` fields filled in:
   ```json
   {
     "id": 2,
     "name": "Datu Puti Sukang Maasim",
     "imageUrl": "https://pinoypantrystorage.blob.core.windows.net/product-images/products/1710345600_datu-puti.jpg",
     "price": 2.99
   }
   ```
4. React renders `<img src="https://pinoypantrystorage.blob.core.windows.net/..." />`
5. The browser fetches the image directly from Azure Blob Storage (not from the API)

The API never serves the image bytes. It only stores the URL.
The browser loads images directly from Blob Storage. This is fast and efficient.

```
Browser  →  Azure Static Web App (React HTML/JS/CSS)
Browser  →  Azure App Service (.NET API for product data)
Browser  →  Azure Blob Storage (product images)
```

Three separate services, each doing one job.

---

## Summary

| Component                  | What It Does                                   |
|----------------------------|------------------------------------------------|
| Azure Blob Storage         | Stores image files permanently in the cloud     |
| `BlobStorageService.cs`    | Uploads/deletes files using the Azure SDK       |
| `IBlobStorageService.cs`   | Interface for loose coupling and testability     |
| `ImageController.cs`       | API endpoint that validates and orchestrates upload |
| `ProductRepository.cs`     | Saves the image URL to the database             |
| `AdminUploadPage.tsx`      | React UI for selecting and uploading images     |
| `FormData` (JavaScript)    | Browser API for sending files over HTTP         |
| `IFormFile` (C#)           | .NET's way of receiving uploaded files           |
| Connection string           | Secret key that authenticates with Blob Storage  |
| CORS policy                | Allows the frontend to call the API              |
