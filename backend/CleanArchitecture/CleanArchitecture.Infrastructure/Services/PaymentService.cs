using CleanArchitecture.Core.DTOs.Payment;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private static readonly Dictionary<string, List<SavedCardDto>> _cards = new();

        public Task<PaymentMethodsResponse> GetPaymentMethodsAsync(string userId)
        {
            if (!_cards.ContainsKey(userId))
            {
                _cards[userId] = new List<SavedCardDto>
                {
                    new SavedCardDto
                    {
                        Id = "card_1",
                        Title = "Mastercard",
                        Detail = "**** 2741",
                        IsDefault = true
                    }
                };
            }

            return Task.FromResult(new PaymentMethodsResponse
            {
                WalletBalance = 120.5,
                SavedCards = _cards[userId]
            });
        }

        public Task<AddCardResponse> AddCardAsync(string userId, AddCardRequest request)
        {
            if (!_cards.ContainsKey(userId))
                _cards[userId] = new List<SavedCardDto>();

            var last4 = request.CardNumber?.Length >= 4
                ? request.CardNumber.Substring(request.CardNumber.Length - 4)
                : "****";

            var cardId = $"card_{Guid.NewGuid():N}".Substring(0, 10);
            _cards[userId].Add(new SavedCardDto
            {
                Id = cardId,
                Title = "Kart",
                Detail = $"**** {last4}",
                IsDefault = false
            });

            return Task.FromResult(new AddCardResponse
            {
                CardId = cardId,
                Message = "Card added successfully"
            });
        }

        public Task DeleteCardAsync(string userId, string cardId)
        {
            if (!_cards.ContainsKey(userId))
                throw new NotFoundException("CARD_NOT_FOUND", "Card not found");

            var card = _cards[userId].FirstOrDefault(c => c.Id == cardId);
            if (card == null)
                throw new NotFoundException("CARD_NOT_FOUND", "Card not found");

            _cards[userId].Remove(card);
            return Task.CompletedTask;
        }

        public Task<PaymentIntentResponse> CreatePaymentIntentAsync(string userId, PaymentIntentRequest request)
        {
            var secret = $"pi_{Guid.NewGuid():N}_secret_{Guid.NewGuid():N}".Substring(0, 32);
            return Task.FromResult(new PaymentIntentResponse
            {
                ClientSecret = secret
            });
        }
    }
}
