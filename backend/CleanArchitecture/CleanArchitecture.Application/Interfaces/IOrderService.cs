using CleanArchitecture.Core.DTOs.Order;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IOrderService
    {
        // --- Cart Management ---
        Task<CartResponse> GetCartAsync(string userId);
        Task<CartResponse> AddToCartAsync(string userId, AddCartItemRequest request);
        Task<CartResponse> UpdateCartItemAsync(string userId, string productId, UpdateCartItemRequest request);
        Task<CartResponse> RemoveFromCartAsync(string userId, string productId);
        Task ClearCartAsync(string userId);
        Task<OrderResponse> CheckoutAsync(string userId, CheckoutRequest request, string idempotencyKey);

        // --- Customer Order Management ---
        Task<OrderResponse> GetOrderByIdAsync(string userId, string orderId);
        Task<PagedResponse<OrderResponse>> GetMyOrdersAsync(string userId, string status, int page, int size);
        Task<OrderResponse> CancelOrderAsync(string userId, string orderId);
        Task<CartResponse> ReorderAsync(string userId, string orderId);
        Task<OrderResponse> RequestRefundAsync(string userId, string orderId);

        // --- Restaurant Order Management ---
        Task<PagedResponse<OrderResponse>> GetRestaurantOrdersAsync(string restaurantId, string status, int page, int size);
        Task<OrderResponse> ConfirmOrderAsync(string restaurantId, string orderId);
        Task<OrderResponse> RejectOrderAsync(string restaurantId, string orderId);
        Task<OrderResponse> UpdateOrderStatusAsync(string restaurantId, string orderId, string status);

        // --- Internal Operations ---
        Task<OrderResponse> ProcessPaymentCallbackAsync(string orderId, string status);
    }
}
