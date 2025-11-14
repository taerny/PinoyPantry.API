using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Data;
using PinoyPantry.API.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

//add services for dependency injection  #erni-man
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<ApplicationDBContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));      
builder.Services.AddScoped<IProductRepository, ProductRepository>();

//configure cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy => { 
        policy.WithOrigins("http://localhost:7136") // React app URL
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

////cors
app.UseCors("AllowReactApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
