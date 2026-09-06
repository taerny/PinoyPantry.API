using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Data;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Services
{
    public class OrderService : IOrderService
    {
        private static readonly string[] ValidStatuses = { "Pending", "Paid", "Cancelled", "Completed" };

        private static readonly string[] DeliveryMethods =
        {
            "Click & Collect",
            "Delivery within Dunedin",
            "Delivery outside Dunedin",
        };

        private const decimal DunedinDeliveryFee = 5.00m;

        private readonly ApplicationDBContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ApplicationDBContext context, IEmailService emailService, ILogger<OrderService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<List<OrderResponseDto>> GetAllOrdersAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return orders.Select(ToDto).ToList();
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            return order == null ? null : ToDto(order);
        }

        public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("Cart is empty.");

            if (!DeliveryMethods.Contains(dto.DeliveryMethod))
                throw new InvalidOperationException("Please choose a delivery method.");

            // No address needed for Click & Collect — required for both delivery options.
            if (dto.DeliveryMethod != "Click & Collect" && string.IsNullOrWhiteSpace(dto.CustomerAddress))
                throw new InvalidOperationException("Please enter a delivery address.");

            var productIds = dto.Items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            decimal total = 0;
            var itemsToCreate = new List<OrderItem>();

            foreach (var item in dto.Items)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                    throw new InvalidOperationException($"Product #{item.ProductId} no longer exists.");

                if (item.Quantity < 1)
                    throw new InvalidOperationException($"Invalid quantity for \"{product.Name}\".");

                if (product.StockQuantity < item.Quantity)
                    throw new InvalidOperationException($"Sorry, only {product.StockQuantity} of \"{product.Name}\" left in stock.");

                total += product.Price * item.Quantity;

                itemsToCreate.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = item.Quantity,
                });

                product.StockQuantity -= item.Quantity;
            }

            // Delivery within Dunedin is a fixed fee; outside Dunedin is null ("to be
            // arranged" — the owner sets it later once agreed with the customer, see
            // UpdateDeliveryFeeAsync); Click & Collect is a known $0.
            decimal? deliveryFee = dto.DeliveryMethod switch
            {
                "Delivery within Dunedin" => DunedinDeliveryFee,
                "Delivery outside Dunedin" => null,
                _ => 0.00m,
            };

            var order = new Order
            {
                CustomerName = dto.CustomerName,
                CustomerEmail = dto.CustomerEmail,
                CustomerPhone = dto.CustomerPhone,
                CustomerAddress = dto.CustomerAddress,
                Notes = dto.Notes,
                Status = "Pending",
                DeliveryMethod = dto.DeliveryMethod,
                DeliveryFee = deliveryFee,
                Total = total + (deliveryFee ?? 0),
                Items = itemsToCreate,
            };

            // SQL Server's connection-retry policy (EnableRetryOnFailure in Program.cs) requires
            // manual transactions to run through its execution strategy, not a bare
            // BeginTransactionAsync — otherwise a transient retry could silently corrupt the
            // transaction. See: https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Invoice number depends on the auto-increment Id, which isn't known until
                // the row exists — so it's a follow-up update inside the same transaction.
                order.InvoiceNumber = $"INV-{order.Id:D6}";
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            });

            // Order is already saved either way — a failed notification email shouldn't
            // undo it or fail the request, just get logged.
            try
            {
                await _emailService.SendOrderConfirmationEmailAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation email for order {OrderId}", order.Id);
            }

            try
            {
                await _emailService.SendNewOrderNotificationEmailAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send new order notification email for order {OrderId}", order.Id);
            }

            return ToDto(order);
        }

        public async Task<OrderResponseDto> CreateWalkInOrderAsync(CreateWalkInOrderDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("Add at least one item to the sale.");

            var productIds = dto.Items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            decimal total = 0;
            var itemsToCreate = new List<OrderItem>();

            foreach (var item in dto.Items)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                    throw new InvalidOperationException($"Product #{item.ProductId} no longer exists.");

                if (item.Quantity < 1)
                    throw new InvalidOperationException($"Invalid quantity for \"{product.Name}\".");

                if (product.StockQuantity < item.Quantity)
                    throw new InvalidOperationException($"Only {product.StockQuantity} of \"{product.Name}\" left in stock.");

                total += product.Price * item.Quantity;

                itemsToCreate.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = item.Quantity,
                });

                product.StockQuantity -= item.Quantity;
            }

            var order = new Order
            {
                CustomerName = string.IsNullOrWhiteSpace(dto.CustomerName) ? "Walk-in Customer" : dto.CustomerName,
                CustomerEmail = dto.CustomerEmail ?? string.Empty,
                Notes = dto.Notes,
                Status = dto.AlreadyPaid ? "Paid" : "Pending",
                Channel = "Walk-in",
                DeliveryMethod = null,
                DeliveryFee = 0.00m,
                Total = total,
                Items = itemsToCreate,
            };

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                order.InvoiceNumber = $"INV-{order.Id:D6}";
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            });

            // Owner always gets notified so there's an email trail alongside the admin
            // panel, showing whether it was paid on the spot or the customer is paying
            // later. Customer only gets a receipt if they gave an email — many walk-in
            // customers won't.
            try
            {
                await _emailService.SendWalkInOwnerNotificationEmailAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send walk-in owner notification email for order {OrderId}", order.Id);
            }

            if (!string.IsNullOrWhiteSpace(order.CustomerEmail))
            {
                try
                {
                    await _emailService.SendWalkInReceiptEmailAsync(order);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send walk-in receipt email for order {OrderId}", order.Id);
                }
            }

            return ToDto(order);
        }

        public async Task<(OrderResponseDto? Order, string? Error)> UpdateStatusAsync(int id, string status)
        {
            if (!ValidStatuses.Contains(status))
                return (null, $"Invalid status '{status}'.");

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return (null, null); // caller distinguishes "not found" from "invalid" via null Error + null Order

            if (order.Status is "Cancelled" or "Completed")
                return (null, $"This order is already {order.Status} and cannot be updated further.");

            if (status == "Completed" && order.Status != "Paid")
                return (null, "Only Paid orders can be marked as Completed.");

            if (status == "Cancelled")
            {
                foreach (var item in order.Items.Where(i => i.ProductId != null))
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                        product.StockQuantity += item.Quantity;
                }
            }

            order.Status = status;
            await _context.SaveChangesAsync();

            return (ToDto(order), null);
        }

        // Sets the delivery fee once the owner has arranged it with the customer (only
        // meaningful for "Delivery outside Dunedin" orders, where the fee starts as null/TBD).
        // Recalculates the total and re-notifies the customer with the confirmed amount to pay.
        public async Task<(OrderResponseDto? Order, string? Error)> UpdateDeliveryFeeAsync(int id, decimal deliveryFee)
        {
            if (deliveryFee < 0)
                return (null, "Delivery fee cannot be negative.");

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return (null, null);

            var itemsTotal = order.Items.Sum(i => i.Price * i.Quantity);
            order.DeliveryFee = deliveryFee;
            order.Total = itemsTotal + deliveryFee;
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendDeliveryFeeConfirmedEmailAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send delivery fee confirmation email for order {OrderId}", order.Id);
            }

            return (ToDto(order), null);
        }

        private static OrderResponseDto ToDto(Order order) => new()
        {
            Id = order.Id,
            InvoiceNumber = order.InvoiceNumber ?? "",
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            CustomerAddress = order.CustomerAddress,
            Notes = order.Notes,
            DeliveryMethod = order.DeliveryMethod,
            DeliveryFee = order.DeliveryFee,
            Status = order.Status,
            Channel = order.Channel,
            Total = order.Total,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemResponseDto
            {
                ProductName = i.ProductName,
                Price = i.Price,
                Quantity = i.Quantity,
            }).ToList(),
        };
    }
}
