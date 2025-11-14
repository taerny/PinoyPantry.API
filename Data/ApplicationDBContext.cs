using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Data
{
    public class ApplicationDBContext: DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }
    }
}
