using CleanArchitecture.Core.DTOs.Order;
using CleanArchitecture.Core.DTOs.Review;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private static readonly Dictionary<string, List<OrderDetailDto>> _orders = new();

        private static readonly AddressSnapshotDto _defaultAddressSnapshot = new AddressSnapshotDto
        {
            AddressTitle = "Ev",
            City = "Antalya",
            District = "Kepez",
            Neighborhood = "Kültür Mah",
            Street = "3818 Sokak",
            BuildingNo = "8",
            Floor = "3",
            ApartmentNo = "6",
            AddressDescription = "Kapý zili çalýþmýyor",
            Location = new OrderLocationDto { Lat = 36.884804, Lng = 30.704044 }
        };

        public Task<CheckoutPreviewResponse> GetCheckoutPreviewAsync(string userId, CheckoutPreviewRequest request)
        {
            var itemsSubtotal = request.Items.Sum(i => i.Quantity * 210.0);
            var deliveryFee = 24.9;
            var serviceFee = 8.99;
            var discountAmount = 40.0;
            var total = itemsSubtotal + deliveryFee + serviceFee - discountAmount;

            return Task.FromResult(new CheckoutPreviewResponse
            {
                ItemsSubtotal = itemsSubtotal,
                DeliveryFee = deliveryFee,
                ServiceFee = serviceFee,
                DiscountAmount = discountAmount,
                TotalAmount = Math.Round(total, 2)
            });
        }

        public Task<CreateOrderResponse> CreateOrderAsync(string userId, CreateOrderRequest request)
        {
            if (!_orders.ContainsKey(userId))
                _orders[userId] = new List<OrderDetailDto>();

            var orderId = $"order_{Guid.NewGuid():N}".Substring(0, 12);
            var totalPrice = request.Items.Sum(i => i.Quantity * 180.0);

            var orderDetail = new OrderDetailDto
            {
                Id = orderId,
                Status = "PREPARING",
                StatusLabel = "Sipariþ hazýrlanýyor",
                EtaRange = $"{DateTime.Now.AddMinutes(25):HH:mm} - {DateTime.Now.AddMinutes(35):HH:mm}",
                ActiveStepIndex = 1,
                AddressSnapshot = _defaultAddressSnapshot,
                Steps = new List<OrderStepDto>
                {
                    new OrderStepDto { Title = "Sipariþ alýndý", IsCompleted = true },
                    new OrderStepDto { Title = "Hazýrlanýyor", IsCompleted = false },
                    new OrderStepDto { Title = "Kurye yolda", IsCompleted = false },
                    new OrderStepDto { Title = "Teslim edildi", IsCompleted = false }
                }
            };

            _orders[userId].Add(orderDetail);

            return Task.FromResult(new CreateOrderResponse
            {
                OrderId = orderId,
                Message = "Order created successfully",
                VendorId = request.VendorId,
                Status = "preparing",
                TotalPrice = Math.Round(totalPrice, 2),
                AddressSnapshot = _defaultAddressSnapshot
            });
        }

        public Task<PagedOrdersResponse> GetOrdersAsync(string userId, int page, int limit)
        {
            if (!_orders.ContainsKey(userId))
            {
                _orders[userId] = new List<OrderDetailDto>
                {
                    new OrderDetailDto
                    {
                        Id = "order_555",
                        Status = "DELIVERED",
                        StatusLabel = "Teslim edildi",
                        EtaRange = "13:32 - 13:38",
                        ActiveStepIndex = 3,
                        AddressSnapshot = _defaultAddressSnapshot,
                        Steps = new List<OrderStepDto>
                        {
                            new OrderStepDto { Title = "Sipariþ alýndý", IsCompleted = true },
                            new OrderStepDto { Title = "Hazýrlanýyor", IsCompleted = true },
                            new OrderStepDto { Title = "Kurye yolda", IsCompleted = true },
                            new OrderStepDto { Title = "Teslim edildi", IsCompleted = true }
                        }
                    }
                };
            }

            var all = _orders[userId];
            var paged = all.Skip((page - 1) * limit).Take(limit)
                .Select(o => new OrderSummaryDto
                {
                    Id = o.Id,
                    VendorName = "Burger Point",
                    Status = o.Status,
                    TotalAmount = 413.89,
                    DateLabel = "Bugün 13:05",
                    AddressSnapshot = o.AddressSnapshot,
                    ItemSummary = "Double Smash Burger x2",
                    DeliveredItemCount = 2
                })
                .ToList();

            return Task.FromResult(new PagedOrdersResponse
            {
                Page = page,
                Limit = limit,
                Total = all.Count,
                Data = paged
            });
        }

        public Task<OrderDetailDto> GetOrderByIdAsync(string userId, string orderId)
        {
            if (_orders.ContainsKey(userId))
            {
                var order = _orders[userId].FirstOrDefault(o => o.Id == orderId);
                if (order != null)
                    return Task.FromResult(order);
            }

            // Return mock for well-known order_555
            if (orderId == "order_555")
            {
                return Task.FromResult(new OrderDetailDto
                {
                    Id = "order_555",
                    Status = "DELIVERING",
                    StatusLabel = "Kurye yolda",
                    EtaRange = "13:32 - 13:38",
                    ActiveStepIndex = 2,
                    AddressSnapshot = _defaultAddressSnapshot,
                    Steps = new List<OrderStepDto>
                    {
                        new OrderStepDto { Title = "Sipariþ alýndý", IsCompleted = true },
                        new OrderStepDto { Title = "Hazýrlanýyor", IsCompleted = true },
                        new OrderStepDto { Title = "Kurye yolda", IsCompleted = false },
                        new OrderStepDto { Title = "Teslim edildi", IsCompleted = false }
                    }
                });
            }

            throw new NotFoundException("ORDER_NOT_FOUND", "Order not found");
        }

        public Task SubmitRatingAsync(string userId, string orderId, SubmitRatingRequest request)
        {
            // Mock: rating stored (no-op for now)
            return Task.CompletedTask;
        }
    }
}
