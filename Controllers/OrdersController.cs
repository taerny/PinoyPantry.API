using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Data;
using PinoyPantry.API.DTOs;

namespace PinoyPantry.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDBContext _context;

    public OrdersController(ApplicationDBContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Applies an order by decrementing stock quantities for the given cart items.
    /// </summary>
    /// <remarks>
    /// This is a simplified endpoint for the portfolio demo. It does not yet
    /// create Order / OrderItem records, it only updates product stock.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> ApplyOrder([FromBody] List<CartItemDto> items)
    {
        if (items == null || items.Count == 0)
        {
            return BadRequest(new { message = "Cart is empty." });
        }

        var productIds = items.Select(i => i.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        foreach (var item in items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product == null) continue;

            var newQty = product.StockQuantity - item.Quantity;
            product.StockQuantity = newQty < 0 ? 0 : newQty;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Inventory updated for order." });
    }
}

