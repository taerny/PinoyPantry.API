using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Services;

namespace PinoyPantry.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // GET: api/orders — Admin only
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<OrderResponseDto>>> GetAllOrders()
    {
        return Ok(await _orderService.GetAllOrdersAsync());
    }

    // GET: api/orders/5 — Admin only
    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            return NotFound(new { message = $"Order #{id} not found." });

        return Ok(order);
    }

    // POST: api/orders — public, submitted from checkout
    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder(CreateOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CustomerName) || string.IsNullOrWhiteSpace(dto.CustomerEmail))
            return BadRequest(new { message = "Name and email are required." });

        try
        {
            var order = await _orderService.CreateOrderAsync(dto);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT: api/orders/5/status — Admin only, e.g. mark Paid once bank transfer is confirmed
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/status")]
    public async Task<ActionResult<OrderResponseDto>> UpdateStatus(int id, UpdateOrderStatusDto dto)
    {
        var (order, error) = await _orderService.UpdateStatusAsync(id, dto.Status);

        if (order == null && error == null)
            return NotFound(new { message = $"Order #{id} not found." });

        if (error != null)
            return UnprocessableEntity(new { message = error });

        return Ok(order);
    }

    // PUT: api/orders/5/delivery-fee — Admin only. Sets the fee once arranged with the customer
    // (for "Delivery outside Dunedin" orders, where it starts unknown) and re-notifies them.
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/delivery-fee")]
    public async Task<ActionResult<OrderResponseDto>> UpdateDeliveryFee(int id, UpdateDeliveryFeeDto dto)
    {
        var (order, error) = await _orderService.UpdateDeliveryFeeAsync(id, dto.DeliveryFee);

        if (order == null && error == null)
            return NotFound(new { message = $"Order #{id} not found." });

        if (error != null)
            return UnprocessableEntity(new { message = error });

        return Ok(order);
    }
}
