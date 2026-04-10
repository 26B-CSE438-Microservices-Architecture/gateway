using CleanArchitecture.Core.DTOs.Order;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IOrderService
    {
        Task<CheckoutPreviewResponse> GetCheckoutPreviewAsync(string userId, CheckoutPreviewRequest request);
        Task<CreateOrderResponse> CreateOrderAsync(string userId, CreateOrderRequest request);
        Task<PagedOrdersResponse> GetOrdersAsync(string userId, int page, int limit);
        Task<OrderDetailDto> GetOrderByIdAsync(string userId, string orderId);
        Task SubmitRatingAsync(string userId, string orderId, DTOs.Review.SubmitRatingRequest request);
    }
}
