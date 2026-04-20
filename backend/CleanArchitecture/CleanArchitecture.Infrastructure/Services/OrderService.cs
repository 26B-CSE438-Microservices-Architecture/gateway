using CleanArchitecture.Core.DTOs.Order;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private static readonly ConcurrentDictionary<string, CartResponse> _carts = new();
        private static readonly ConcurrentDictionary<string, OrderResponse> _orders = new();
        private static readonly ConcurrentDictionary<string, OrderResponse> _idempotencyKeys = new();

        private static readonly List<CartItemDto> _mockProducts = new()
        {
            new CartItemDto { ProductId = "p_1", Name = "Margherita Pizza", Price = 120.0 },
            new CartItemDto { ProductId = "p_2", Name = "Burger Menu", Price = 180.0 },
            new CartItemDto { ProductId = "p_3", Name = "Coke 330ml", Price = 45.0 }
        };

        public Task<CartResponse> GetCartAsync(string userId)
        {
            return Task.FromResult(_carts.GetOrAdd(userId, _ => new CartResponse { Items = new(), TotalAmount = 0 }));
        }

        public async Task<CartResponse> AddToCartAsync(string userId, AddCartItemRequest request)
        {
            var cart = await GetCartAsync(userId);
            var product = _mockProducts.FirstOrDefault(p => p.ProductId == request.ProductId);
            
            if (product == null) throw new NotFoundException("PRODUCT_NOT_FOUND", "Product not found");

            var existing = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (existing != null)
            {
                existing.Quantity += request.Quantity;
            }
            else
            {
                cart.Items.Add(new CartItemDto
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = request.Quantity
                });
            }

            RecalculateCart(cart);
            return cart;
        }

        public async Task<CartResponse> UpdateCartItemAsync(string userId, string productId, UpdateCartItemRequest request)
        {
            var cart = await GetCartAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) throw new NotFoundException("ITEM_NOT_FOUND", "Item not found in cart");

            if (request.Quantity <= 0)
            {
                cart.Items.Remove(item);
            }
            else
            {
                item.Quantity = request.Quantity;
            }

            RecalculateCart(cart);
            return cart;
        }

        public async Task<CartResponse> RemoveFromCartAsync(string userId, string productId)
        {
            var cart = await GetCartAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                cart.Items.Remove(item);
            }

            RecalculateCart(cart);
            return cart;
        }

        public Task ClearCartAsync(string userId)
        {
            _carts[userId] = new CartResponse { Items = new(), TotalAmount = 0 };
            return Task.CompletedTask;
        }

        public async Task<OrderResponse> CheckoutAsync(string userId, CheckoutRequest request, string idempotencyKey)
        {
            if (string.IsNullOrEmpty(idempotencyKey))
                throw new ApiException("MISSING_IDEMPOTENCY_KEY", "Idempotency-Key header is required.");

            if (_idempotencyKeys.TryGetValue(idempotencyKey, out var existing))
            {
                return existing;
            }

            var cart = await GetCartAsync(userId);
            if (!cart.Items.Any()) throw new ApiException("CART_EMPTY", "Cannot checkout an empty cart.");

            var orderId = $"ord_{Guid.NewGuid():N}".Substring(0, 16);
            var order = new OrderResponse
            {
                OrderId = orderId,
                UserId = userId,
                Status = "PAYMENT_PENDING",
                TotalAmount = cart.TotalAmount,
                Items = cart.Items.Select(i => new OrderItemDto
                {
                    Id = $"itm_{Guid.NewGuid():N}".Substring(0, 8),
                    Name = i.Name,
                    Price = i.Price,
                    Quantity = i.Quantity
                }).ToList(),
                DeliveryAddress = request.DeliveryAddress,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _orders[orderId] = order;
            _idempotencyKeys[idempotencyKey] = order;
            
            await ClearCartAsync(userId);

            return order;
        }

        public Task<OrderResponse> GetOrderByIdAsync(string userId, string orderId)
        {
            if (!_orders.TryGetValue(orderId, out var order))
                throw new NotFoundException("ORDER_NOT_FOUND", "Order not found");

            return Task.FromResult(order);
        }

        public Task<PagedResponse<OrderResponse>> GetMyOrdersAsync(string userId, string status, int page, int size)
        {
            var query = _orders.Values.Where(o => o.UserId == userId);
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var results = query
                .OrderByDescending(o => o.CreatedAt)
                .Skip(page * size)
                .Take(size)
                .ToList();

            return Task.FromResult(new PagedResponse<OrderResponse>
            {
                Page = page,
                Size = size,
                Total = query.Count(),
                Data = results
            });
        }

        public async Task<OrderResponse> CancelOrderAsync(string userId, string orderId)
        {
            var order = await GetOrderByIdAsync(userId, orderId);
            
            if (order.Status == "DELIVERED" || order.Status == "CANCELLED" || order.Status == "PAID" || order.Status == "PAYMENT_CAPTURE_PENDING")
                throw new ApiException("INVALID_STATE", $"Cannot cancel order in status {order.Status}");

            order.Status = "CANCELLED";
            order.UpdatedAt = DateTime.UtcNow;
            return order;
        }

        public async Task<CartResponse> ReorderAsync(string userId, string orderId)
        {
            var order = await GetOrderByIdAsync(userId, orderId);
            await ClearCartAsync(userId);
            
            foreach (var item in order.Items)
            {
                await AddToCartAsync(userId, new AddCartItemRequest { ProductId = "p_1", Quantity = item.Quantity });
            }

            return await GetCartAsync(userId);
        }

        public async Task<OrderResponse> RequestRefundAsync(string userId, string orderId)
        {
            var order = await GetOrderByIdAsync(userId, orderId);
            if (order.Status != "PAID" && order.Status != "DELIVERED")
                throw new ApiException("REFUND_NOT_ALLOWED", "Only PAID or DELIVERED orders can be refunded.");

            order.Status = "REFUND_REQUESTED";
            order.UpdatedAt = DateTime.UtcNow;
            return order;
        }

        public Task<PagedResponse<OrderResponse>> GetRestaurantOrdersAsync(string restaurantId, string status, int page, int size)
        {
            var query = _orders.Values.AsQueryable();
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var results = query
                .OrderByDescending(o => o.CreatedAt)
                .Skip(page * size)
                .Take(size)
                .ToList();

            return Task.FromResult(new PagedResponse<OrderResponse>
            {
                Page = page,
                Size = size,
                Total = query.Count(),
                Data = results
            });
        }

        public async Task<OrderResponse> ConfirmOrderAsync(string restaurantId, string orderId)
        {
            var order = await GetOrderByIdAsync("system", orderId);
            if (order.Status != "PAYMENT_HELD")
                throw new ApiException("INVALID_STATE", "Order must be in PAYMENT_HELD state to be confirmed.");

            order.Status = "PAYMENT_CAPTURE_PENDING";
            order.UpdatedAt = DateTime.UtcNow;
            
            await ProcessPaymentCallbackAsync(orderId, "CAPTURE_COMPLETED");
            
            return order;
        }

        public async Task<OrderResponse> RejectOrderAsync(string restaurantId, string orderId)
        {
            var order = await GetOrderByIdAsync("system", orderId);
            order.Status = "CANCELLED";
            order.UpdatedAt = DateTime.UtcNow;
            return order;
        }

        public async Task<OrderResponse> UpdateOrderStatusAsync(string restaurantId, string orderId, string status)
        {
            var order = await GetOrderByIdAsync("system", orderId);
            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;
            return order;
        }

        public async Task<OrderResponse> ProcessPaymentCallbackAsync(string orderId, string status)
        {
            if (!_orders.TryGetValue(orderId, out var order))
                throw new NotFoundException("ORDER_NOT_FOUND", "Order not found");

            switch (status)
            {
                case "HOLD_CONFIRMED":
                    order.Status = "PAYMENT_HELD";
                    break;
                case "HOLD_FAILED":
                    order.Status = "CANCELLED";
                    break;
                case "CAPTURE_COMPLETED":
                    order.Status = "PAID";
                    break;
                case "CAPTURE_FAILED":
                    order.Status = "CANCELLED";
                    break;
            }

            order.UpdatedAt = DateTime.UtcNow;
            await Task.Yield(); 
            return order;
        }

        private void RecalculateCart(CartResponse cart)
        {
            cart.TotalAmount = Math.Round(cart.Items.Sum(i => i.Subtotal), 2);
        }
    }
}
