# Authentication Feature — Full Explanation (From Scratch)

This document explains everything about how login, registration, and
security work in PinoyPantry. Read it top to bottom. No prior knowledge
of JWT or authentication is assumed.

---

## Part 1 — What Is Authentication and Why Do We Need It?

### The Problem

Without authentication, anyone can:
- Create products
- Delete products
- Upload images
- Modify data

That's dangerous. We need a way to:
1. Know WHO is making the request (authentication)
2. Know WHAT they are allowed to do (authorization)

### Authentication vs Authorization

**Authentication** = "Who are you?"
→ You prove your identity by providing email + password.

**Authorization** = "What are you allowed to do?"
→ An Admin can upload images. A Customer can only browse products.

These are two separate steps. First you authenticate (login), then the
system checks your authorization (role) before allowing an action.

---

## Part 2 — What Is JWT?

### The Concept

JWT stands for **JSON Web Token**. It's a small piece of text that proves
who you are.

Think of it like a wristband at a concert:
1. You show your ticket at the entrance (login with email + password)
2. They give you a wristband (JWT token)
3. For the rest of the night, you just show your wristband to get in
   anywhere — no need to show your ticket again
4. The wristband has your info on it (name, VIP or general admission)
5. The wristband expires at the end of the night

### What a JWT Looks Like

A JWT is a long string with three parts separated by dots:

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
```

The three parts are:

**Part 1 — Header** (how it's encoded):
```json
{ "alg": "HS256", "typ": "JWT" }
```
This says "I'm a JWT and I'm signed using the HMAC-SHA256 algorithm."

**Part 2 — Payload** (the actual data):
```json
{
  "sub": "user-id-123",
  "email": "admin@pinoypantry.com",
  "name": "PinoyPantry Admin",
  "role": "Admin",
  "exp": 1710432000
}
```
This contains claims about the user: who they are, their role, and when
the token expires. Anyone can READ this data (it's just Base64 encoded,
not encrypted). But they CANNOT MODIFY it — that's what Part 3 is for.

**Part 3 — Signature** (proves it's real):
The server takes Part 1 + Part 2 + a SECRET KEY (only the server knows)
and creates a digital signature. If anyone modifies the payload (like
changing "Customer" to "Admin"), the signature won't match, and the
server will reject it.

### How JWT Works in Our App

```
Step 1: User sends POST /api/auth/login
        Body: { "email": "admin@pinoypantry.com", "password": "Admin123!" }

Step 2: API checks email exists in database ✓
        API checks password matches (hashed comparison) ✓
        API looks up user's role: "Admin"

Step 3: API creates a JWT token containing:
        - User ID
        - Email
        - Full Name
        - Role: "Admin"
        - Expires: 24 hours from now
        API signs it with the secret key.

Step 4: API returns the token to the frontend:
        {
          "token": "eyJhbG...",
          "email": "admin@pinoypantry.com",
          "fullName": "PinoyPantry Admin",
          "role": "Admin",
          "expiration": "2026-03-17T14:00:00Z"
        }

Step 5: Frontend stores the token in localStorage.

Step 6: Next time the user uploads an image, the frontend sends:
        POST /api/image/upload
        Headers: { "Authorization": "Bearer eyJhbG..." }

Step 7: API receives the request, reads the Authorization header,
        extracts the JWT, verifies the signature, reads the claims,
        and knows: "This is an Admin user, allow the upload."
```

### Why Not Just Use Sessions/Cookies?

In traditional web apps (like PHP), you log in and get a session cookie.
The server stores your session data in memory or a database.

JWT is different — the server stores NOTHING. All the user info is IN
the token itself. This is called "stateless" authentication. Benefits:

- **Scalable** — If you have 10 servers, any server can verify the token.
  No need to share session storage.
- **API-friendly** — Perfect for REST APIs where the frontend and backend
  are separate applications (like our React + .NET setup).
- **Mobile-friendly** — Works the same for web, iOS, and Android apps.

### JWT vs Cookies Comparison

| Feature        | Session/Cookie          | JWT Token               |
|---------------|-------------------------|-------------------------|
| Stored where? | Server (memory/DB)      | Client (localStorage)   |
| Sent how?     | Cookie header (auto)    | Authorization header    |
| Stateless?    | No (server tracks it)   | Yes (self-contained)    |
| Good for APIs?| Not ideal               | Perfect                 |
| Scalability   | Harder (shared state)   | Easy (no shared state)  |

---

## Part 3 — ASP.NET Identity (The User Management System)

### What Is ASP.NET Identity?

It's Microsoft's built-in system for managing users. Instead of writing
code to hash passwords, create user tables, handle roles, and manage
security — Identity does it all for you.

### What Identity Gives You For Free

1. **User table** with secure password hashing (bcrypt-like)
2. **Role system** (Admin, Customer, etc.)
3. **Account lockout** (lock after too many failed attempts)
4. **Email confirmation** (optional)
5. **Two-factor authentication** (optional)
6. **Password reset** (optional)

### The NuGet Packages We Installed

```
Microsoft.AspNetCore.Identity.EntityFrameworkCore  (v8.0.22)
Microsoft.AspNetCore.Authentication.JwtBearer      (v8.0.22)
```

- **Identity.EntityFrameworkCore** — The user management system that stores
  data using Entity Framework (same ORM we already use for Products).
- **Authentication.JwtBearer** — The middleware that reads JWT tokens from
  the Authorization header and validates them.

### The Database Tables Identity Creates

When you run `Add-Migration AddIdentityAuth` and `Update-Database`, these
tables are created in your database:

| Table Name          | Purpose                                    |
|---------------------|--------------------------------------------|
| AspNetUsers         | All user accounts (email, hashed password) |
| AspNetRoles         | Role definitions (Admin, Customer)          |
| AspNetUserRoles     | Which user has which role                   |
| AspNetUserClaims    | Extra claims per user (we don't use this)   |
| AspNetUserLogins    | External login providers (Google, etc.)     |
| AspNetUserTokens    | Refresh tokens and 2FA tokens              |
| AspNetRoleClaims    | Extra claims per role                       |

The most important ones are `AspNetUsers`, `AspNetRoles`, and `AspNetUserRoles`.

---

## Part 4 — The Backend Code (Step by Step)

### 4.1 — ApplicationUser.cs (The User Model)

**File:** `Models/ApplicationUser.cs`

```csharp
using Microsoft.AspNetCore.Identity;

namespace PinoyPantry.API.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**What this does:**

`IdentityUser` is a class from ASP.NET Identity that already has:
- `Id` (string, a GUID)
- `UserName`
- `Email`
- `PasswordHash` (the hashed password — never stored in plain text)
- `PhoneNumber`
- `EmailConfirmed`
- And many more fields...

We extend it by adding `FullName`, `Address`, and `CreatedAt`. This is
like inheriting from a base class that already has 90% of what you need.

**Why not just use IdentityUser directly?**

You could. But in a real app, you always need custom fields (like a
customer's address or display name). By creating `ApplicationUser`, we
have a place to add those.

### 4.2 — ApplicationDBContext.cs (Updated)

**File:** `Data/ApplicationDBContext.cs`

```csharp
public class ApplicationDBContext : IdentityDbContext<ApplicationUser>
```

**Before:** `ApplicationDBContext : DbContext`
**After:** `ApplicationDBContext : IdentityDbContext<ApplicationUser>`

This single change tells Entity Framework: "This database context now
includes all the Identity tables." `IdentityDbContext` inherits from
`DbContext`, so everything else (like `DbSet<Product>`) still works.

### 4.3 — Auth DTOs (Data Transfer Objects)

**File:** `DTOs/AuthDtos.cs`

```csharp
public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}
```

Same pattern as Product DTOs:
- `RegisterDto` — What the frontend sends when creating an account
- `LoginDto` — What the frontend sends when logging in
- `AuthResponseDto` — What the API returns after successful login/register

The frontend never receives the `PasswordHash`. It only gets the JWT token
and basic user info.

### 4.4 — AuthController.cs (The Main Auth Logic)

**File:** `Controllers/AuthController.cs`

This is the heart of authentication. Let's break it down method by method.

**Constructor:**

```csharp
public AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IConfiguration configuration)
{
    _userManager = userManager;
    _signInManager = signInManager;
    _configuration = configuration;
}
```

- `UserManager<ApplicationUser>` — Provided by ASP.NET Identity. It handles
  creating users, finding users by email, hashing passwords, managing roles.
  You don't write this code — Identity provides it via DI.

- `SignInManager<ApplicationUser>` — Handles password verification. When a
  user logs in, it compares the provided password against the stored hash.

- `IConfiguration` — Reads settings from `appsettings.json` (like the JWT
  secret key, issuer, etc.).

**Register Endpoint:**

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register(RegisterDto dto)
{
    var user = new ApplicationUser
    {
        UserName = dto.Email,
        Email = dto.Email,
        FullName = dto.FullName,
        PhoneNumber = dto.Phone,
        Address = dto.Address,
        EmailConfirmed = true
    };

    var result = await _userManager.CreateAsync(user, dto.Password);

    if (!result.Succeeded)
    {
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return BadRequest(new { message = errors });
    }

    await _userManager.AddToRoleAsync(user, "Customer");

    var token = await GenerateToken(user);
    return Ok(token);
}
```

Step by step:

1. Creates an `ApplicationUser` object from the registration data.
   `UserName` is set to the email (Identity requires a username).
   `EmailConfirmed = true` skips email verification (for simplicity).

2. `_userManager.CreateAsync(user, dto.Password)` — This does TWO things:
   - Hashes the password using a secure algorithm (PBKDF2 with 100,000 iterations)
   - Inserts the user into the `AspNetUsers` table
   Identity NEVER stores the plain-text password. It stores a hash that
   looks like: `AQAAAAEAACcQAAAAEE6vE8bF3x...`

3. `result.Succeeded` — If the email already exists, or the password doesn't
   meet requirements, this is `false` and we return the error messages.

4. `_userManager.AddToRoleAsync(user, "Customer")` — Adds a row to the
   `AspNetUserRoles` table linking this user to the "Customer" role.

5. `GenerateToken(user)` — Creates and returns a JWT token so the user is
   automatically logged in after registering (no need to log in separately).

**Login Endpoint:**

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login(LoginDto dto)
{
    var user = await _userManager.FindByEmailAsync(dto.Email);
    if (user == null)
        return Unauthorized(new { message = "Invalid email or password." });

    var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
    if (!result.Succeeded)
        return Unauthorized(new { message = "Invalid email or password." });

    var token = await GenerateToken(user);
    return Ok(token);
}
```

Step by step:

1. Find the user by email. If not found, return 401 Unauthorized.

2. `CheckPasswordSignInAsync` — Takes the plain-text password the user typed,
   hashes it using the same algorithm, and compares it to the stored hash.
   If they match, the password is correct.

   **Important:** We say "Invalid email or password" in BOTH cases (user not
   found AND wrong password). This is a security practice — you never tell
   an attacker which one was wrong. If you said "Email not found," they
   would know that email doesn't have an account.

3. If password matches, generate a JWT token and return it.

**Me Endpoint (Get Current User):**

```csharp
[Authorize]
[HttpGet("me")]
public async Task<IActionResult> GetCurrentUser()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var user = await _userManager.FindByIdAsync(userId!);
    var roles = await _userManager.GetRolesAsync(user);

    return Ok(new
    {
        email = user.Email,
        fullName = user.FullName,
        phone = user.PhoneNumber,
        address = user.Address,
        role = roles.FirstOrDefault() ?? "Customer"
    });
}
```

The `[Authorize]` attribute means: "This endpoint requires a valid JWT token."
If you call it without a token, you get 401 Unauthorized.

`User.FindFirstValue(ClaimTypes.NameIdentifier)` reads the user ID from the
JWT token's claims. The `User` object is automatically populated by the JWT
middleware when it validates the token.

**Generate Token Method:**

```csharp
private async Task<AuthResponseDto> GenerateToken(ApplicationUser user)
{
    var roles = await _userManager.GetRolesAsync(user);
    var role = roles.FirstOrDefault() ?? "Customer";

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Email, user.Email!),
        new(ClaimTypes.Name, user.FullName),
        new(ClaimTypes.Role, role)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expiration = DateTime.UtcNow.AddHours(24);

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: expiration,
        signingCredentials: creds
    );

    return new AuthResponseDto
    {
        Token = new JwtSecurityTokenHandler().WriteToken(token),
        Email = user.Email!,
        FullName = user.FullName,
        Role = role,
        Expiration = expiration
    };
}
```

This is where the JWT token is actually created. Let's trace it:

1. **Claims** — These are the pieces of info embedded in the token:
   - `NameIdentifier` = user's database ID
   - `Email` = user's email
   - `Name` = user's full name
   - `Role` = "Admin" or "Customer"

2. **Key** — The secret key from `appsettings.json`. This MUST be at least
   32 characters long for HMAC-SHA256. This key is what makes the signature
   unforgeable. If someone doesn't know the key, they can't create a valid token.

3. **SigningCredentials** — Uses HMAC-SHA256 algorithm. This is the same
   algorithm used in many production systems.

4. **JwtSecurityToken** — Creates the actual token with:
   - `issuer` — Who created the token ("PinoyPantryAPI")
   - `audience` — Who the token is for ("PinoyPantryClient")
   - `claims` — The user data
   - `expires` — 24 hours from now
   - `signingCredentials` — The key + algorithm for the signature

5. **WriteToken()** — Converts the token object into the final string
   (the `eyJhbG...` format).

### 4.5 — Program.cs (Configuration)

**Identity Registration:**

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDBContext>()
.AddDefaultTokenProviders();
```

This tells .NET:
- Use `ApplicationUser` as the user model and `IdentityRole` for roles
- Password rules: minimum 6 characters, no special requirements
  (relaxed for development — in production you'd want stricter rules)
- Emails must be unique (no two accounts with the same email)
- Store everything in our existing database via `ApplicationDBContext`
- Add token providers for password reset, email confirmation, etc.

**JWT Authentication Configuration:**

```csharp
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});
```

This configures the JWT middleware. When a request arrives with an
`Authorization: Bearer eyJhbG...` header, this middleware automatically:

1. Reads the token from the header
2. Checks the issuer matches "PinoyPantryAPI"
3. Checks the audience matches "PinoyPantryClient"
4. Checks the token hasn't expired
5. Verifies the signature using the secret key
6. If everything passes, populates `User` with the claims (so controllers
   can read `User.FindFirstValue(ClaimTypes.Role)` etc.)
7. If anything fails, returns 401 Unauthorized

Each validation parameter explained:
- `ValidateIssuer = true` — Reject tokens not issued by our API
- `ValidateAudience = true` — Reject tokens not intended for our client
- `ValidateLifetime = true` — Reject expired tokens
- `ValidateIssuerSigningKey = true` — Verify the signature (most important!)

**Middleware Order:**

```csharp
app.UseCors("AllowReactApp");
app.UseAuthentication();  // ← NEW: Reads and validates JWT tokens
app.UseAuthorization();   // ← Checks [Authorize] attributes
app.MapControllers();
```

The order matters:
1. CORS runs first (allows cross-origin requests)
2. Authentication reads the JWT token and identifies the user
3. Authorization checks if the identified user has permission
4. MapControllers handles the actual request

If you swap Authentication and Authorization, it breaks.

**Seeding Roles and Admin User:**

```csharp
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "Customer" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var adminEmail = "admin@pinoypantry.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "PinoyPantry Admin",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(admin, "Admin123!");
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}
```

This runs ONCE when the API starts. It:
1. Creates the "Admin" and "Customer" roles if they don't exist
2. Creates a default admin account if it doesn't exist

This ensures you always have an admin account to log into, even on a
fresh database. The password "Admin123!" is hashed before storage.

### 4.6 — Protected Endpoints

```csharp
[Authorize(Roles = "Admin")]
[HttpPost]
public async Task<ActionResult<ProductResponseDto>> CreateProduct(...)
```

The `[Authorize(Roles = "Admin")]` attribute means:
- A valid JWT token is required
- The token's `role` claim must be "Admin"
- If not, return 403 Forbidden

We added this to:
- `POST /api/products` (create product) — Admin only
- `PUT /api/products/{id}` (update product) — Admin only
- `DELETE /api/products/{id}` (delete product) — Admin only
- `POST /api/image/upload` (upload image) — Admin only

We did NOT add it to:
- `GET /api/products` — Anyone can browse products
- `GET /api/products/{id}` — Anyone can view a product
- `POST /api/auth/register` — Anyone can create an account
- `POST /api/auth/login` — Anyone can log in

---

## Part 5 — The Frontend Code (Step by Step)

### 5.1 — AuthContext.tsx (Storing Auth State)

**File:** `src/contexts/AuthContext.tsx`

This is the React "brain" for authentication. It stores the user's token
and provides login/register/logout functions to any component.

**The User Interface:**

```typescript
interface User {
  email: string;
  fullName: string;
  role: string;
  token: string;
  expiration: string;
}
```

This matches what the API returns in `AuthResponseDto`.

**Loading From localStorage:**

```typescript
useEffect(() => {
    const stored = localStorage.getItem('pp_user');
    if (stored) {
        const parsed = JSON.parse(stored) as User;
        if (new Date(parsed.expiration) > new Date()) {
            setUser(parsed);
        } else {
            localStorage.removeItem('pp_user');
        }
    }
    setLoading(false);
}, []);
```

When the app first loads, this checks if a previous login exists in
`localStorage`. If the token hasn't expired, the user is automatically
logged in. If it has expired, it's removed.

This means: if you close your browser and come back, you're still logged
in (until the token expires after 24 hours).

**The Login Function:**

```typescript
async function login(email: string, password: string) {
    const res = await fetch(`${API_URL}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
    });

    if (!res.ok) {
        const err = await res.json();
        throw new Error(err.message || 'Login failed');
    }

    const data = await res.json();
    const userData: User = {
        email: data.email,
        fullName: data.fullName,
        role: data.role,
        token: data.token,
        expiration: data.expiration,
    };
    setUser(userData);
    localStorage.setItem('pp_user', JSON.stringify(userData));
}
```

1. Sends email + password to the API as JSON
2. If the API returns an error (wrong password), throws an Error
3. If successful, saves the user data (including the JWT token) to state
   AND localStorage

**The Logout Function:**

```typescript
function logout() {
    setUser(null);
    localStorage.removeItem('pp_user');
}
```

Simply clears the user from state and localStorage. The JWT token is
discarded. There's no "logout" API call needed because JWTs are stateless —
the server doesn't track sessions. Removing the token from the client is
enough.

**The isAdmin Helper:**

```typescript
const isAdmin = user?.role === 'Admin';
```

Any component can use `const { isAdmin } = useAuth()` to check if the
current user is an admin. This is used to show/hide admin features.

### 5.2 — LoginPage.tsx (The UI)

The login page has two tabs (Login and Sign Up) and now calls the real API.

**Login Submit:**

```typescript
if (isLogin) {
    await login(formData.email, formData.password);
    setSuccess('Welcome back!');
    setTimeout(() => onClose(), 1000);
}
```

Calls the `login` function from AuthContext. If successful, shows a
success message and redirects to the home page after 1 second.

**Register Submit:**

```typescript
await register({
    email: formData.email,
    password: formData.password,
    fullName: formData.fullName,
    phone: formData.phone,
    address: formData.address,
});
```

Sends all the registration fields to the API. After success, the user
is automatically logged in (the API returns a token on registration too).

**Already Logged In:**

If the user visits `/login` while already logged in, they see a welcome
screen with their name, email, and role, plus a link to the admin panel
(if they're an admin).

### 5.3 — AdminUploadPage.tsx (Protected)

**Access Check:**

```typescript
const { user, isAdmin, logout } = useAuth();

if (!user || !isAdmin) {
    return (
        <div>
            <h2>Access Denied</h2>
            <p>You must be logged in as an Admin to access this page.</p>
            <a href="/login">Go to Login</a>
        </div>
    );
}
```

If you're not logged in or not an admin, you see "Access Denied" instead
of the upload page. This is CLIENT-SIDE protection.

But even if someone bypasses this (by modifying JavaScript), the API will
still reject their requests because the `[Authorize(Roles = "Admin")]`
attribute checks the JWT token server-side. You need BOTH client-side
and server-side protection.

**Sending the Token with Uploads:**

```typescript
const res = await fetch(`${API_URL}/api/image/upload?productId=${productId}`, {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${user.token}` },
    body: formData,
});
```

The `Authorization: Bearer <token>` header is added to every request that
needs authentication. The API's JWT middleware reads this header and
verifies the token before the controller code runs.

### 5.4 — App.tsx (AuthProvider Wrapper)

```typescript
<BrowserRouter>
    <AuthProvider>
        <CartProvider>
            <AppContent />
        </CartProvider>
    </AuthProvider>
</BrowserRouter>
```

`AuthProvider` wraps the entire app so any component can call `useAuth()`.
It's placed outside `CartProvider` because auth is more fundamental —
you might need to know the user before doing anything else.

---

## Part 6 — The JWT Settings

### appsettings.json

```json
{
  "Jwt": {
    "Key": "PinoyPantry-Super-Secret-Key-2026-Must-Be-At-Least-32-Chars!",
    "Issuer": "PinoyPantryAPI",
    "Audience": "PinoyPantryClient",
    "ExpirationInHours": 24
  }
}
```

- **Key** — The secret used to sign tokens. MUST be at least 32 characters.
  In production, this should be a random string stored in environment
  variables (never in source code).
- **Issuer** — Identifies who created the token. Like a stamp saying
  "issued by PinoyPantry API."
- **Audience** — Identifies who the token is for. Like saying "this token
  is valid for the PinoyPantry frontend."
- **ExpirationInHours** — How long the token is valid. After 24 hours,
  the user must log in again.

### Azure App Service Environment Variables

For the live site, these must be added to the `pinoypantry-api` App Service:

| Name           | Value                                                           |
|----------------|-----------------------------------------------------------------|
| Jwt__Key       | PinoyPantry-Super-Secret-Key-2026-Must-Be-At-Least-32-Chars!   |
| Jwt__Issuer    | PinoyPantryAPI                                                  |
| Jwt__Audience  | PinoyPantryClient                                               |

The double underscores (`__`) represent nested JSON in .NET configuration.
`Jwt__Key` maps to `Jwt:Key` in code.

---

## Part 7 — Password Security

### How Passwords Are Stored

When you register with password "Admin123!", Identity does this:

1. Generates a random salt (random bytes unique to this password)
2. Runs PBKDF2 with 100,000 iterations:
   `hash = PBKDF2(password + salt, 100000 iterations)`
3. Stores the result in the database:
   `AQAAAAIAAYagAAAAEE6vE8bF3x...`

The stored hash contains the algorithm version, salt, and hash — all in one string.

### How Login Verification Works

When you log in with "Admin123!":

1. Identity reads the stored hash from the database
2. Extracts the salt from the stored hash
3. Runs the same PBKDF2 algorithm with the provided password + salt
4. Compares the result to the stored hash
5. If they match, the password is correct

**Key point:** The password is NEVER stored or compared in plain text.
Even if someone steals the entire database, they can't reverse the hashes
back to passwords. They would have to try billions of guesses (which takes
years due to the 100,000 iterations).

---

## Part 8 — The Roles System

### How Roles Work

We have two roles:

| Role     | Can Do                                        |
|----------|-----------------------------------------------|
| Admin    | Everything — browse, create, update, delete, upload images |
| Customer | Browse products only                          |

### How Roles Are Checked

1. When a user logs in, the API looks up their role from `AspNetUserRoles`
2. The role is added as a claim in the JWT token
3. When a request hits `[Authorize(Roles = "Admin")]`:
   - The JWT middleware reads the role claim from the token
   - If the role is "Admin", the request proceeds
   - If not, the API returns 403 Forbidden

### Default Accounts

| Email                  | Password   | Role     |
|------------------------|-----------|----------|
| admin@pinoypantry.com  | Admin123! | Admin    |
| (any new registration) | (chosen)  | Customer |

The admin account is created automatically when the API first starts.
All new registrations get the "Customer" role.

---

## Part 9 — The Full Flow (End to End)

### Login Flow

```
Step 1:  User opens /login in the browser
Step 2:  Types admin@pinoypantry.com and Admin123!
Step 3:  Clicks "Login"
Step 4:  React calls POST /api/auth/login with { email, password }
Step 5:  .NET AuthController.Login() runs
Step 6:  _userManager.FindByEmailAsync("admin@pinoypantry.com")
         → Finds the user in AspNetUsers table
Step 7:  _signInManager.CheckPasswordSignInAsync(user, "Admin123!")
         → Hashes "Admin123!" and compares to stored hash → Match!
Step 8:  GenerateToken(user) creates JWT with claims:
         { userId: "abc-123", role: "Admin", exp: tomorrow }
Step 9:  Signs the token with the secret key
Step 10: Returns { token: "eyJhbG...", role: "Admin", ... }
Step 11: React receives the response
Step 12: Saves user data to state and localStorage
Step 13: Shows "Welcome back, PinoyPantry Admin!"
Step 14: Redirects to home page after 1 second
```

### Protected Request Flow (Image Upload)

```
Step 1:  Admin clicks "Upload" on /admin/upload
Step 2:  React creates FormData with the image file
Step 3:  React adds header: Authorization: Bearer eyJhbG...
Step 4:  Sends POST /api/image/upload?productId=1
Step 5:  .NET JWT middleware intercepts the request
Step 6:  Reads the Authorization header
Step 7:  Extracts the JWT token
Step 8:  Verifies the signature using the secret key → Valid!
Step 9:  Checks expiration → Not expired!
Step 10: Reads claims: role = "Admin"
Step 11: [Authorize(Roles = "Admin")] → "Admin" matches → Allowed!
Step 12: ImageController.Upload() runs normally
Step 13: Image uploaded to Azure Blob Storage
Step 14: Response returned to React
```

### Unauthorized Request Flow

```
Step 1:  Regular customer tries to call POST /api/products
Step 2:  Their token has role = "Customer"
Step 3:  JWT middleware validates token → Valid
Step 4:  [Authorize(Roles = "Admin")] → "Customer" ≠ "Admin" → REJECTED
Step 5:  API returns 403 Forbidden
Step 6:  React shows an error message
```

---

## Part 10 — Comparison With Other Frameworks

If you've used other frameworks, here's how the concepts map:

### PHP / Laravel

| Our App (.NET)                  | Laravel Equivalent                  |
|---------------------------------|-------------------------------------|
| ASP.NET Identity                | Laravel Breeze / Sanctum / Passport |
| ApplicationUser                 | User model (Eloquent)               |
| JWT token                       | Sanctum token or Passport JWT       |
| [Authorize]                     | `auth` middleware                   |
| [Authorize(Roles = "Admin")]    | `Gate::allows('admin')`             |
| UserManager                     | Auth facade                         |
| appsettings.json                | .env file                           |

### Node.js / Express

| Our App (.NET)                  | Express Equivalent                  |
|---------------------------------|-------------------------------------|
| ASP.NET Identity                | Passport.js + bcrypt                |
| JWT generation                  | jsonwebtoken (npm package)          |
| [Authorize] attribute           | auth middleware function             |
| UserManager.CreateAsync         | bcrypt.hash() + db.insert()         |
| SignInManager.CheckPassword     | bcrypt.compare()                    |

---

## Part 11 — The Service Layer Refactor (Senior Approach)

### Why We Refactored

The first version had all the auth logic (JWT generation, password checking,
user creation) directly inside `AuthController.cs`. This works, but it's messy:

- Controllers should only handle HTTP (read request, return response)
- Business logic should be in a separate Service class
- This is the same pattern we use for Products: `ProductsController` → `ProductService`

### The Clean Architecture

```
AuthController.cs          ← Thin controller: handles HTTP, calls service
    ↓
IAuthService.cs            ← Interface (contract)
AuthService.cs             ← Business logic: JWT generation, login, register
    ↓
UserManager<ApplicationUser>  ← ASP.NET Identity (database operations)
```

Compare with Products:

```
ProductsController.cs      ← Thin controller: handles HTTP, calls service
    ↓
IProductService.cs         ← Interface (contract)
ProductService.cs          ← Business logic
    ↓
IProductRepository.cs      ← Interface (contract)
ProductRepository.cs       ← Database operations (EF Core)
```

Same pattern. Every feature follows: Controller → Service → Data.

### IAuthService.cs (The Contract)

```csharp
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<UserProfileDto?> GetProfileAsync(string userId);
}
```

Three methods. The controller calls these. It doesn't know or care how
they work internally.

### AuthService.cs (The Implementation)

This class contains ALL the auth business logic:
- Creating users with `UserManager`
- Verifying passwords with `SignInManager`
- Generating JWT tokens
- Looking up user profiles

The controller is now just ~50 lines instead of ~130 lines. It only does:
1. Validate input
2. Call the service
3. Return the appropriate HTTP status code

### AuthController.cs (Slim Version)

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login(LoginDto dto)
{
    try
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(result);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Unauthorized(new { message = ex.Message });
    }
}
```

Notice: no JWT logic, no UserManager, no password checking. The controller
just calls `_authService.LoginAsync()` and handles the result. Clean.

### DataSeeder.cs (Extracted from Program.cs)

The seed code that creates roles and the admin account was moved from
`Program.cs` to its own class: `Data/DataSeeder.cs`.

Program.cs now has just one line for seeding:

```csharp
using (var scope = app.Services.CreateScope())
{
    await DataSeeder.SeedRolesAndAdmin(scope.ServiceProvider);
}
```

This keeps Program.cs focused on configuration and middleware, not data seeding.

### Registration in Program.cs

```csharp
builder.Services.AddScoped<IAuthService, AuthService>();
```

One line. Same as products. DI handles the rest.

---

## Part 12 — React Hooks Rule (Bug We Fixed)

### The Problem

When we first added auth checking to `AdminUploadPage.tsx`, we had this
structure:

```typescript
export function AdminUploadPage() {
    const { user, isAdmin } = useAuth();        // ← Hook 1
    const [products, setProducts] = useState();  // ← Hook 2

    if (!user || !isAdmin) {                     // ← Early return
        return <div>Access Denied</div>;
    }

    useEffect(() => {                            // ← Hook 3 (AFTER return!)
        fetchProducts();
    }, []);
```

This breaks React's "Rules of Hooks":
- Hooks must ALWAYS run in the same order on every render
- You can't put hooks after conditional returns
- React tracks hooks by their position in the call sequence

On the first render, `user` is null (auth is still loading from localStorage),
so the function returns early. `useEffect` never runs. On the second render,
`user` is loaded, so the function passes the check and hits `useEffect`.
But React sees: "First render had 2 hooks, second render has 3 hooks" → crash.

### The Fix

Move ALL hooks to the top, before any conditional returns:

```typescript
export function AdminUploadPage() {
    const { user, isAdmin, loading: authLoading } = useAuth();  // Hook 1
    const [products, setProducts] = useState();                   // Hook 2
    const [loading, setLoading] = useState(true);                 // Hook 3

    useEffect(() => {                                             // Hook 4
        if (!authLoading && user && isAdmin) {
            fetchProducts();
        }
    }, [authLoading, user, isAdmin]);

    // NOW safe to do conditional returns
    if (authLoading) return <div>Loading...</div>;
    if (!user || !isAdmin) return <div>Access Denied</div>;
```

All 4 hooks run on EVERY render, in the same order. The conditional logic
is INSIDE the useEffect, not wrapping it. This is the correct React pattern.

---

## Part 13 — Full Project Architecture (Final State)

```
PinoyPantry.API/
├── Controllers/
│   ├── AuthController.cs         ← Login, Register, Me (thin)
│   ├── ProductsController.cs     ← Product CRUD (thin)
│   └── ImageController.cs        ← Image upload (thin)
├── Services/
│   ├── IAuthService.cs           ← Auth contract
│   ├── AuthService.cs            ← JWT + Identity logic
│   ├── IProductService.cs        ← Product contract
│   ├── ProductService.cs         ← Product business logic
│   ├── IBlobStorageService.cs    ← Blob storage contract
│   └── BlobStorageService.cs     ← Azure Blob upload/delete
├── Repositories/
│   ├── IProductRepository.cs     ← Data access contract
│   └── ProductRepository.cs      ← EF Core queries
├── Models/
│   ├── Product.cs                ← Product entity
│   └── ApplicationUser.cs        ← User entity (extends IdentityUser)
├── DTOs/
│   ├── ProductDtos.cs            ← Product request/response shapes
│   ├── AuthDtos.cs               ← Auth request/response shapes
│   └── UserProfileDto.cs         ← Profile response shape
├── Data/
│   ├── ApplicationDBContext.cs   ← Database context (IdentityDbContext)
│   └── DataSeeder.cs             ← Seeds roles + admin account
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs  ← Global error handling
├── Validators/
│   ├── CreateProductDtoValidator.cs    ← FluentValidation rules
│   └── UpdateProductDtoValidator.cs    ← FluentValidation rules
├── Migrations/                   ← EF Core migration files
└── Program.cs                    ← DI registration + middleware pipeline

PinoyPantry.Client/
├── src/
│   ├── contexts/
│   │   ├── AuthContext.tsx        ← JWT token state + login/logout
│   │   └── CartContext.tsx        ← Shopping cart state
│   ├── pages/
│   │   ├── LoginPage.tsx          ← Login + Register UI
│   │   ├── AdminUploadPage.tsx    ← Protected image upload
│   │   ├── HomePage.tsx           ← Product listing
│   │   ├── CategoryPage.tsx       ← Filtered products
│   │   └── ...
│   ├── services/
│   │   ├── apiProductService.ts   ← Fetches from .NET API
│   │   └── productService.ts      ← Data source selector
│   └── App.tsx                    ← Routes + providers
└── .env                           ← API URL configuration
```

---

## Summary

| Component                   | What It Does                                        |
|-----------------------------|-----------------------------------------------------|
| `ApplicationUser.cs`        | User model with custom fields (extends IdentityUser)|
| `IAuthService.cs`           | Auth service contract (interface)                    |
| `AuthService.cs`            | JWT generation, login, register logic                |
| `AuthController.cs`         | Thin HTTP layer — calls AuthService                  |
| `DataSeeder.cs`             | Seeds roles + admin account on startup               |
| `AuthDtos.cs`               | Request/response shapes for auth endpoints           |
| `UserProfileDto.cs`         | Shape for user profile responses                     |
| `IdentityDbContext`         | Adds Identity tables to our database                 |
| `Program.cs` (Identity)     | Configures user management + password rules          |
| `Program.cs` (JWT)          | Configures token validation                          |
| `[Authorize]`               | Protects endpoints — requires valid token            |
| `[Authorize(Roles="Admin")]`| Requires Admin role specifically                     |
| `AuthContext.tsx`            | React state for current user + token                 |
| `LoginPage.tsx`             | Login/register UI that calls the API                 |
| `AdminUploadPage.tsx`       | Protected page — checks auth before rendering        |
| `localStorage`              | Persists the JWT token across browser sessions       |
| `Authorization: Bearer`     | HTTP header that sends the token with each request   |
