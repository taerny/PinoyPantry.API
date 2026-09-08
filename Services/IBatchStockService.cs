using PinoyPantry.API.Models;

namespace PinoyPantry.API.Services
{
    public interface IBatchStockService
    {
        Task DeductAsync(Product product, int quantity);
        Task RestockAsync(Product product, int quantity);

        // Sets Product.CostPrice (and recomputes RecommendedRetail) from whichever batch is
        // currently the active FIFO-selling one — call after anything that could change which
        // batch that is (deduct, restock, add batch, delete batch).
        Task SyncProductCostAsync(Product product);
    }
}
