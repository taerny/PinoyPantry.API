# Admin Panel, Stripe Payment & Dev Environment — Full Explanation

This document covers everything added after authentication was implemented.
Read it top to bottom. Topics covered:

1. Why we restructured the admin panel
2. Admin Dashboard (stats, charts)
3. Product Management with CRUD
4. Inline editing (qty, price, category)
5. Image upload inside the edit modal
6. Admin Settings (account info, change password, last login)
7. Stripe payment gateway
8. How `.env` was fixed so it auto-switches between local and live

---

## Part 1 — The Problem: Admin Panel Was Too Fragmented

### What We Had Before

Initially the admin had two separate pages:

```
/admin/upload   → manage product images (upload/replace)
```

That was it. No way to add new products, edit prices, update stock,
or see how the store was performing.

### What Was Wrong

1. **Images were separate from products** — to change an image you had to go
   to a different page. That is not how any real system works.
2. **No product management** — you couldn't add or edit products from the UI.
   You would need Postman or Swagger just to add a product.
3. **No dashboard** — you had no overview: how many products? How many have
   images? What is the total inventory value?
4. **No account management** — the admin couldn't change their password.

### What We Built

```
/admin/dashboard   → Store overview (stats, charts, recent products)
/admin/products    → Full product CRUD + inline editing + image upload
/admin/settings    → Account info, last login, change password
```

Images page (`/admin/upload`) still exists but is no longer in the nav —
image upload now lives inside the product edit form.

---

## Part 2 — Shared Admin Layout (AdminLayout Component)

### Why a Shared Layout?

Before, each admin page had its own copy of the header and navigation bar.
That is bad because:
- If you change the nav, you have to update 3 files
- Easy to forget one — bugs happen
- More code, harder to maintain

### The Solution — A Layout Component

We created `src/components/AdminLayout.tsx`. Every admin page wraps its
content with this component:

```tsx
export function AdminDashboardPage() {
  return (
    <AdminLayout activePage="dashboard">
      {/* page content here */}
    </AdminLayout>
  );
}
```

`AdminLayout` renders the dark navy header, the navigation tabs (Dashboard,
Products, Settings), the user name, Store link, and Logout button — once,
in one file.

### How It Works

```tsx
interface AdminLayoutProps {
  children: ReactNode;                                // page content
  activePage: 'dashboard' | 'products' | 'settings'; // which tab is highlighted
}
```

The `activePage` prop tells the layout which nav tab to highlight in gold
(`text-[#F9A825]`). The rest are dimmed (`text-white/50`).

If you add a new admin page later, you:
1. Add a new nav item to the `navItems` array in `AdminLayout.tsx`
2. Add the new string to the `activePage` type
3. Use `<AdminLayout activePage="your-new-page">` in the new page

No duplication.

---

## Part 3 — Admin Dashboard

### What It Does

`/admin/dashboard` shows a real-time overview of the store:

- **4 stat cards** — Total Products, Registered Users, Categories, Inventory Value
- **Category breakdown** — Progress bars showing products per category
- **Recent products** — Last 5 products added, with image status
- **Quick stats** — How many products have images vs missing images

### How the Data Gets There

**Frontend** calls `GET /api/auth/dashboard-stats` with a Bearer token:

```tsx
const res = await fetch(`${API_URL}/api/auth/dashboard-stats`, {
  headers: { 'Authorization': `Bearer ${user.token}` }
});
```

**Backend** (`AuthController`) is protected with `[Authorize(Roles = "Admin")]`
so only admins can see it:

```csharp
[Authorize(Roles = "Admin")]
[HttpGet("dashboard-stats")]
public async Task<IActionResult> GetDashboardStats()
{
    var stats = await _authService.GetDashboardStatsAsync();
    return Ok(stats);
}
```

**AuthService** queries the database:

```csharp
public async Task<DashboardStatsDto> GetDashboardStatsAsync()
{
    var products = await _context.Products.ToListAsync();
    var userCount = await _context.Users.CountAsync();

    var categoryStats = products
        .GroupBy(p => p.Category)
        .Select(g => new CategoryStatDto { Category = g.Key, Count = g.Count() })
        .OrderByDescending(c => c.Count)
        .ToList();

    return new DashboardStatsDto
    {
        TotalProducts = products.Count,
        TotalUsers = userCount,
        ProductsWithImages = products.Count(p => !string.IsNullOrEmpty(p.ImageUrl)),
        TotalCategories = categoryStats.Count,
        TotalInventoryValue = products.Sum(p => p.Price * p.StockQuantity),
        CategoryStats = categoryStats,
        RecentProducts = products.OrderByDescending(p => p.CreatedAt).Take(5)...
    };
}
```

**DTOs used:**

```csharp
public class DashboardStatsDto
{
    public int TotalProducts { get; set; }
    public int TotalUsers { get; set; }
    public int ProductsWithImages { get; set; }
    public int TotalCategories { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<CategoryStatDto> CategoryStats { get; set; } = new();
    public List<RecentProductDto> RecentProducts { get; set; } = new();
}
```

---

## Part 4 — Product Management (Full CRUD)

### What CRUD Means

CRUD = **Create, Read, Update, Delete** — the four basic operations for
any data in a system. Every product management screen in the world does
these four things.

| Operation | HTTP method | Endpoint | Who can do it |
|-----------|-------------|----------|---------------|
| Create | POST | /api/products | Admin only |
| Read | GET | /api/products | Everyone |
| Update | PUT | /api/products/{id} | Admin only |
| Delete | DELETE | /api/products/{id} | Admin only |

The backend was already built. The admin page gives you a UI for it.

### The Product Table

`/admin/products` shows all products in a table:

```
Product image | Name | Category | Price | Stock | Actions (Edit / Delete)
```

### Add Product

Clicking "Add Product" opens a modal form with:
- Product name (text)
- Description (textarea)
- Price (number, step 0.01)
- Stock quantity (number)
- Category (dropdown)
- Image upload (see Part 6)

On submit it calls `POST /api/products` with a Bearer token.

### Edit Product

Clicking the pencil icon opens the same modal pre-filled with existing data.
On submit it calls `PUT /api/products/{id}` with a Bearer token.

### Delete Product

Clicking the trash icon shows a confirmation modal ("Delete Product? This
action cannot be undone."). On confirm it calls `DELETE /api/products/{id}`.

---

## Part 5 — Inline Editing (Price, Stock, Category)

### Why Inline Editing

For small, frequent changes — like restocking after delivery — opening a
full modal is slow. Click, scroll, find the field, change it, close.

Inline editing lets you click directly on the value in the table, change it,
press Enter, and it saves. Much faster.

This is how Shopify admin, WooCommerce, and most real inventory systems work.

### How It Works — Step by Step

**1. State for tracking which cell is being edited:**

```tsx
interface InlineEdit {
  id: number;      // which product
  field: 'price' | 'stockQuantity' | 'category';  // which field
  value: string;   // current typed value
}

const [inlineEdit, setInlineEdit] = useState<InlineEdit | null>(null);
```

`null` means nothing is being edited. When you click a value, state becomes:
```tsx
{ id: 5, field: 'price', value: '1.50' }
```

**2. Clicking a value starts inline editing:**

```tsx
<button
  onClick={() => setInlineEdit({ id: product.id, field: 'price', value: product.price.toString() })}
  className="... group"
>
  <Pencil className="opacity-0 group-hover:opacity-40" />  {/* hint icon */}
  ${product.price.toFixed(2)}
</button>
```

The small pencil icon appears on hover as a hint that the field is editable.

**3. When `inlineEdit` is set, render an input instead of the button:**

```tsx
{isInlinePrice ? (
  <input
    type="number"
    value={inlineEdit.value}
    onChange={e => setInlineEdit({ ...inlineEdit, value: e.target.value })}
    onBlur={saveInline}           // save when you click away
    onKeyDown={e => {
      if (e.key === 'Enter') saveInline();   // save on Enter
      if (e.key === 'Escape') cancelInline(); // cancel on Escape
    }}
  />
) : (
  <button onClick={...}>${product.price.toFixed(2)}</button>
)}
```

**4. Saving with optimistic update:**

```tsx
async function saveInline() {
  // Build updated product with the new field value
  const updated = {
    name: product.name,
    price: inlineEdit.field === 'price' ? parseFloat(inlineEdit.value) : product.price,
    stockQuantity: inlineEdit.field === 'stockQuantity' ? parseInt(inlineEdit.value) : product.stockQuantity,
    // ... etc
  };

  setInlineEdit(null); // stop editing immediately

  // Optimistic update — show new value right away (before API response)
  setProducts(prev => prev.map(p => p.id === id ? { ...p, ...updated } : p));

  // Then call the API
  const res = await fetch(`/api/products/${id}`, {
    method: 'PUT',
    headers: { 'Authorization': `Bearer ${token}` },
    body: JSON.stringify(updated),
  });

  if (!res.ok) {
    fetchProducts(); // if API fails, revert by re-fetching
  }
}
```

### What Is "Optimistic Update"?

Normal flow:
```
User changes value → wait for API response (~300ms) → UI updates
```

Optimistic update:
```
User changes value → UI updates immediately → API call runs in background
```

The UI feels instant. If the API fails, you revert. This is how most
modern apps work (Gmail, Notion, Shopify all do this).

### Fields Available for Inline Edit

| Field | Control | How to save |
|-------|---------|-------------|
| Price | Number input | Press Enter or click away |
| Stock Quantity | Number input | Press Enter or click away |
| Category | Dropdown select | Select option (auto-saves on blur) |

Press `Escape` to cancel any inline edit without saving.

---

## Part 6 — Image Upload Inside the Edit Modal

### Why We Moved It

Before: `/admin/upload` was a separate page just for images.

After: Image upload is a section at the top of the Edit Product modal.

This is the correct approach because:
- Image IS a product field — it belongs with the product form
- No need to navigate away just to update an image
- The product image preview shows immediately after upload

### How It Works

**In the modal, at the top of the form:**

```tsx
<div className="flex items-center gap-4">
  {/* Current image preview */}
  <div className="w-20 h-20 rounded-xl overflow-hidden bg-gray-100">
    {form.imageUrl
      ? <img src={form.imageUrl} alt="Product" className="w-full h-full object-cover" />
      : <div>NO IMAGE</div>
    }
  </div>

  {/* Upload button */}
  <button type="button" onClick={handleImageSelect} disabled={uploading}>
    <Upload className="w-4 h-4" />
    {uploading ? 'Uploading...' : form.imageUrl ? 'Replace Image' : 'Upload Image'}
  </button>
</div>
```

**The upload function:**

```tsx
function handleImageSelect() {
  // Create invisible file input
  const input = document.createElement('input');
  input.type = 'file';
  input.accept = 'image/jpeg,image/png,image/webp,image/gif';

  input.onchange = async (e) => {
    const file = e.target.files[0];
    const formData = new FormData();
    formData.append('file', file);

    const res = await fetch(`/api/image/upload?productId=${editingId}`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${user.token}` },
      body: formData,
    });

    const data = await res.json();
    // Update image preview immediately in the form
    setForm(f => ({ ...f, imageUrl: data.imageUrl }));
    // Also update in the product list (so table thumbnail updates)
    setProducts(prev => prev.map(p => p.id === editingId ? { ...p, imageUrl: data.imageUrl } : p));
  };

  input.click(); // open file picker
}
```

The image uploads to Azure Blob Storage immediately when you pick the file —
even before you click "Save Changes." The `imageUrl` is stored in `form.imageUrl`
and will be included when you submit the form.

---

## Part 7 — Admin Settings (Account Info + Change Password)

### What It Does

`/admin/settings` shows:

**Account Information panel:**
- Large avatar (first letter of name)
- Full name + role badge (Admin = red, Customer = blue)
- Email address
- Role
- Member Since (account creation date)
- **Last Login** (date and time of most recent login)

**Change Password form:**
- Current password
- New password
- Confirm new password
- Validates that new passwords match before calling API
- Validates minimum 6 characters

### Last Login — How It Works

**Backend** — `ApplicationUser` model has a new field:

```csharp
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }   // ← new (nullable)
}
```

Nullable (`?`) because on first registration there is no last login yet.

**Every time someone logs in**, `AuthService.LoginAsync` updates this:

```csharp
public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
{
    var user = await _userManager.FindByEmailAsync(dto.Email);
    // ... password check ...

    user.LastLoginAt = DateTime.UtcNow;   // record the login time
    await _userManager.UpdateAsync(user); // save to database

    return await GenerateTokenResponse(user);
}
```

**EF Core migration** was needed because this is a new column:

```
Add-Migration AddLastLoginAt
Update-Database
```

This creates the `LastLoginAt` column in the `AspNetUsers` table.

### Change Password — How It Works

**Frontend** calls `POST /api/auth/change-password` with Bearer token:

```tsx
const res = await fetch(`${API_URL}/api/auth/change-password`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${user.token}`
  },
  body: JSON.stringify({
    currentPassword: passwords.current,
    newPassword: passwords.newPw
  }),
});
```

**Backend** uses ASP.NET Identity's built-in `ChangePasswordAsync`:

```csharp
public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto)
{
    var user = await _userManager.FindByIdAsync(userId);

    var result = await _userManager.ChangePasswordAsync(
        user,
        dto.CurrentPassword,  // verifies current password is correct
        dto.NewPassword        // sets the new password (hashed automatically)
    );

    if (!result.Succeeded)
        throw new InvalidOperationException(string.Join(", ", result.Errors...));
}
```

ASP.NET Identity handles password hashing — you never store plain text passwords.
If the current password is wrong, `ChangePasswordAsync` returns an error.

---

## Part 8 — Stripe Payment Gateway

### What Is Stripe?

Stripe is the most widely used payment processor in the world. Used by Amazon,
Shopify, Uber, and thousands of other apps. It handles:
- Credit/debit card processing
- Security (PCI compliance)
- Fraud detection
- Payment intents and confirmations

You never touch raw card numbers. Stripe's JavaScript collects them in a
secure iframe — your code never sees the card number at all.

### Architecture — How Payment Works

```
User selects "Credit Card" in checkout
  → Frontend calls POST /api/payment/create-payment-intent (our API)
    → Our API calls Stripe API (creates a PaymentIntent)
      → Stripe returns a "client secret" (a temporary key)
        → Frontend receives client secret
          → Stripe Elements uses it to render secure card form
            → User enters card details (goes directly to Stripe, NOT our server)
              → User clicks "Pay $X.XX"
                → Stripe processes payment
                  → Returns success/failure
                    → Frontend shows order confirmation
```

The key insight: **your server never sees the card number.** Stripe's form
collects it directly. Your server only creates a PaymentIntent (the intent
to charge) and Stripe does the rest.

### Backend — PaymentController

```csharp
[HttpPost("create-payment-intent")]
public async Task<IActionResult> CreatePaymentIntent(CreatePaymentIntentDto dto)
{
    var totalAmount = dto.Items.Sum(item => item.Price * item.Quantity);
    var amountInCents = (long)(totalAmount * 100); // Stripe uses cents

    StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

    var options = new PaymentIntentCreateOptions
    {
        Amount = amountInCents,
        Currency = "nzd",
        AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
        {
            Enabled = true, // card, Apple Pay, Google Pay etc.
        },
    };

    var service = new PaymentIntentService();
    var paymentIntent = await service.CreateAsync(options);

    return Ok(new { clientSecret = paymentIntent.ClientSecret });
}
```

**`GET /api/payment/config`** returns the publishable key (safe to expose
to the browser):

```csharp
[HttpGet("config")]
public IActionResult GetConfig()
{
    return Ok(new { publishableKey = _configuration["Stripe:PublishableKey"] });
}
```

### Two Keys — Why?

| Key | Starts with | Exposed to public? | Used for |
|-----|-------------|-------------------|----------|
| Publishable key | `pk_test_...` | Yes — frontend | Initialize Stripe.js on the browser |
| Secret key | `sk_test_...` | NO — backend only | Create PaymentIntents, charge cards |

The publishable key is safe in the browser. The secret key must NEVER be in
frontend code. It lives in `appsettings.json` (local) or Azure App Service
environment variables (live).

### Placeholder Keys

In `appsettings.json` we added:

```json
"Stripe": {
  "PublishableKey": "pk_test_REPLACE_WITH_YOUR_PUBLISHABLE_KEY",
  "SecretKey": "sk_test_REPLACE_WITH_YOUR_SECRET_KEY"
}
```

Replace these with real keys from https://dashboard.stripe.com/test/apikeys
when you create a Stripe account. For now the checkout shows a friendly
"Payment service unavailable" message instead of crashing.

### Frontend — StripeCheckout Component

`src/components/StripeCheckout.tsx` handles the entire Stripe UI:

```tsx
export function StripeCheckout({ items, total, onSuccess, onCancel }) {
  const [clientSecret, setClientSecret] = useState(null);

  useEffect(() => {
    // 1. Create payment intent on our server
    fetch('/api/payment/create-payment-intent', {
      method: 'POST',
      body: JSON.stringify({ items })
    })
    .then(r => r.json())
    .then(data => setClientSecret(data.clientSecret));
  }, [items]);

  // 2. Once we have the secret, render Stripe's card form
  return (
    <Elements stripe={stripePromise} options={{ clientSecret }}>
      <CheckoutForm onSuccess={onSuccess} total={total} />
    </Elements>
  );
}
```

**`CheckoutForm`** uses Stripe's built-in `PaymentElement` component:

```tsx
function CheckoutForm({ onSuccess, total }) {
  const stripe = useStripe();
  const elements = useElements();

  async function handleSubmit(e) {
    e.preventDefault();
    const { error } = await stripe.confirmPayment({
      elements,
      confirmParams: { return_url: window.location.origin + '/checkout?success=true' },
    });
    if (!error) onSuccess();
  }

  return (
    <form onSubmit={handleSubmit}>
      <PaymentElement />   {/* Stripe renders the card form here */}
      <button>Pay ${total.toFixed(2)} NZD</button>
    </form>
  );
}
```

### Where It Appears

In `CheckoutPage.tsx`, when the user selects "Credit/Debit Card":

```tsx
{paymentMethod === 'card' ? (
  <StripeCheckout
    items={cartItems}
    total={total}
    onSuccess={() => { setOrderNumber(...); setOrderComplete(true); }}
    onCancel={() => setPaymentMethod('')}
  />
) : (
  <button onClick={handlePlaceOrder}>Place Order</button>
)}
```

Other payment methods (Cash on Delivery, GCash, PayMaya) still use the
simulated "Place Order" flow. Only "Credit/Debit Card" triggers Stripe.

### NuGet Package

```
dotnet add package Stripe.net
```

### npm Package

```
npm install @stripe/stripe-js @stripe/react-stripe-js
```

---

## Part 9 — The .env Fix: Auto-Switch Between Local and Live

### The Old Problem

The `.env` file had a single `VITE_API_URL` value. When you ran locally
you had to change it to `https://localhost:7136`. When you pushed to Azure
you had to change it back. Easy to forget, annoying.

```env
# Had to manually change this every time
VITE_API_URL=https://pinoypantry-api-f0a8hbfwc6fwdfbg.australiaeast-01.azurewebsites.net
```

### How Vite Environment Files Work

Vite supports multiple `.env` files and loads them based on mode:

| File | When is it loaded? |
|------|--------------------|
| `.env` | Always (base config) |
| `.env.development` | Only when running `npm run dev` |
| `.env.production` | Only when running `npm run build` |
| `.env.local` | Always, overrides `.env`, ignored by git |
| `.env.development.local` | Only on dev, ignored by git |

If the same variable appears in both `.env` and `.env.development`, the
more specific file wins when in development mode.

### The Solution

We created `.env.development`:

```env
# Automatically used by Vite when running: npm run dev
VITE_API_URL=https://localhost:7136
```

And kept `.env` pointing to Azure:

```env
# Used by GitHub Actions build (production)
VITE_API_URL=https://pinoypantry-api-f0a8hbfwc6fwdfbg.australiaeast-01.azurewebsites.net
```

**Result:**

```
npm run dev           → uses .env.development → VITE_API_URL = https://localhost:7136
npm run build         → uses .env             → VITE_API_URL = Azure URL
GitHub Actions build  → uses .env             → VITE_API_URL = Azure URL
```

You never touch these files again. The right URL is picked automatically
depending on how you are running the app.

### Why This Works — Vite Build Time

Important concept: Vite bakes environment variables into the JavaScript
bundle at BUILD time. They are not read at runtime like Node.js server vars.

```tsx
// In your code:
const API_URL = import.meta.env.VITE_API_URL || 'https://localhost:7136';
```

When you run `npm run build`, Vite replaces `import.meta.env.VITE_API_URL`
with the actual string from the `.env` file. The final `index.js` bundle
literally contains the URL — not a variable lookup.

That is also why adding env vars in the Azure portal does NOT help for
Vite apps — the portal vars are available at runtime (when the server
starts), but the bundle was already built by then. The var must be present
during `npm run build`.

### .gitignore and .env.development

`.env.development` does NOT contain secrets (just a localhost URL), so it
is safe to commit to GitHub. It was committed and pushed.

If your `.env.development` ever contained secrets (API keys, passwords),
rename it to `.env.development.local` — Vite still loads it but `.gitignore`
excludes `*.local` files automatically.

---

## Part 10 — EF Core Migration Explained (LastLoginAt)

### Why Migration Was Needed

When you add a new property to a C# model class, EF Core knows about it
in code — but the actual database table does NOT have the column yet.
If you run the app without adding the column, it crashes:

```
Microsoft.Data.SqlClient.SqlException: 'Invalid column name 'LastLoginAt'.'
```

### How Migration Works

A migration is a C# file that describes the database change:

**Step 1 — Create the migration file:**

```
Add-Migration AddLastLoginAt
```

EF Core compares your current models to the last migration snapshot and
generates a new file in the `Migrations/` folder:

```csharp
public partial class AddLastLoginAt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "LastLoginAt",
            table: "AspNetUsers",
            type: "datetime2",
            nullable: true);  // nullable because new users have no last login
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "LastLoginAt", table: "AspNetUsers");
    }
}
```

`Up()` = what to do when applying the migration (add column).
`Down()` = how to undo it (drop column).

**Step 2 — Apply to local database:**

```
Update-Database
```

EF Core runs the `Up()` method against your local SQL Server. The column
now exists in `PinoyPantryDb.AspNetUsers`.

**Step 3 — Apply to Azure SQL:**

```
Update-Database -Connection "Server=tcp:pinoypantry-server.database.windows.net,..."
```

Same migration, different target database. This is why the same migration
file works for both environments — you write it once, apply it anywhere.

### One Migration for Multiple Column Changes

You do NOT run `Add-Migration` once per column. EF Core captures ALL
pending model changes into one migration file.

Example: if you added `LastLoginAt`, `ProfilePicture`, and `Bio` all at once
before running `Add-Migration`, a single migration handles all three:

```
Add-Migration AddUserProfileFields
```

The migration name is just a label for humans. EF Core determines what to
include by comparing your models to the last snapshot — not by the name.

---

## Part 11 — Full Architecture After All Features

```
PinoyPantry.API/
├── Controllers/
│   ├── AuthController.cs          ← Login, Register, Me, Change Password, Dashboard Stats
│   ├── ProductsController.cs      ← Product CRUD (Admin protected)
│   ├── ImageController.cs         ← Image upload to Azure Blob
│   └── PaymentController.cs       ← Stripe payment intent + config
├── Services/
│   ├── IAuthService.cs            ← Auth contract
│   ├── AuthService.cs             ← JWT + Identity + LastLoginAt + Dashboard
│   ├── IProductService.cs         ← Product contract
│   ├── ProductService.cs          ← Product business logic
│   ├── IBlobStorageService.cs     ← Blob storage contract
│   └── BlobStorageService.cs      ← Azure Blob upload
├── Repositories/
│   ├── IProductRepository.cs      ← Data access contract
│   └── ProductRepository.cs       ← EF Core queries
├── Models/
│   ├── Product.cs                 ← Product entity
│   └── ApplicationUser.cs         ← User entity (+ LastLoginAt)
├── DTOs/
│   ├── ProductDtos.cs             ← Product request/response shapes
│   ├── AuthDtos.cs                ← Auth DTOs (+ ChangePasswordDto)
│   ├── UserProfileDto.cs          ← Profile response (+ LastLoginAt)
│   ├── DashboardStatsDto.cs       ← Dashboard stats response
│   └── PaymentDtos.cs             ← Stripe payment DTOs
├── Data/
│   ├── ApplicationDBContext.cs    ← Database context (IdentityDbContext)
│   └── DataSeeder.cs              ← Seeds roles + admin account
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
├── Migrations/                    ← EF Core migration files
│   └── ...AddLastLoginAt.cs       ← LastLoginAt column migration
└── Program.cs                     ← DI + middleware pipeline

PinoyPantry.Client/
├── src/
│   ├── components/
│   │   ├── AdminLayout.tsx         ← Shared admin header + nav (Dashboard, Products, Settings)
│   │   ├── StripeCheckout.tsx      ← Stripe Elements checkout form
│   │   ├── Header.tsx              ← Main site header (with user dropdown)
│   │   └── ...
│   ├── pages/
│   │   ├── AdminDashboardPage.tsx  ← Stats cards, category chart, recent products
│   │   ├── AdminProductsPage.tsx   ← Full CRUD + inline edit + image upload in modal
│   │   ├── AdminSettingsPage.tsx   ← Account info + change password
│   │   ├── AdminUploadPage.tsx     ← Legacy (still works, not in nav)
│   │   ├── CheckoutPage.tsx        ← Checkout with Stripe card option
│   │   ├── LoginPage.tsx           ← Login + register
│   │   └── ...
│   ├── contexts/
│   │   ├── AuthContext.tsx         ← JWT token state + login/logout
│   │   └── CartContext.tsx         ← Shopping cart state
│   ├── services/
│   │   └── productService.ts       ← Fetches from .NET API
│   └── App.tsx                     ← Routes + providers
├── .env                            ← Azure URL (used in production builds)
├── .env.development                ← localhost URL (auto-loaded on npm run dev)
└── package.json
```

---

## Part 12 — Summary of All API Endpoints (Full List)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | /api/products | None | Get all products (paginated) |
| GET | /api/products/{id} | None | Get single product |
| POST | /api/products | Admin | Create product |
| PUT | /api/products/{id} | Admin | Update product |
| DELETE | /api/products/{id} | Admin | Delete product |
| POST | /api/image/upload | Admin | Upload image to Azure Blob |
| POST | /api/auth/register | None | Register new user |
| POST | /api/auth/login | None | Login, returns JWT |
| GET | /api/auth/me | Any user | Get current user profile |
| POST | /api/auth/change-password | Any user | Change password |
| GET | /api/auth/dashboard-stats | Admin | Get store stats |
| POST | /api/payment/create-payment-intent | None | Create Stripe PaymentIntent |
| GET | /api/payment/config | None | Get Stripe publishable key |

---

## Part 13 — What Still Needs Doing (When You're Ready)

| Feature | Notes |
|---------|-------|
| Real Stripe keys | Sign up at stripe.com, get test keys, replace placeholders in appsettings.json and Azure env vars |
| Azure migration | Run `Update-Database -Connection "..."` to add LastLoginAt to Azure SQL |
| Unit tests | xUnit + Moq + FluentAssertions for API services |
| Order history | Store completed orders in database |
| Customer admin | List of registered customers in admin panel |
| Search in admin products | Filter table by name/category |
| Stock alerts | Highlight products with stock = 0 in dashboard |
