using PinoyPantry.API.Models;

namespace PinoyPantry.API.Services
{
    public interface IBatchStockService
    {
        Task DeductAsync(Product product, int quantity);
        Task RestockAsync(Product product, int quantity);
    }
}
