using CleanArchitecture.Core.DTOs.Payment;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentMethodsResponse> GetPaymentMethodsAsync(string userId);
        Task<AddCardResponse> AddCardAsync(string userId, AddCardRequest request);
        Task DeleteCardAsync(string userId, string cardId);
        Task<PaymentIntentResponse> CreatePaymentIntentAsync(string userId, PaymentIntentRequest request);
    }
}
