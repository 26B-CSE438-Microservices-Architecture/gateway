using CleanArchitecture.Core.DTOs.Order;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace CleanArchitecture.WebApi.Controllers
{
    /// <summary>
    /// Restoran sipariş yönetimi controller'ı.
    /// Confirm/Reject işlemleri RabbitMQ üzerinden asenkron SAGA olarak çalışır.
    /// </summary>
    [Route("api/v1/orders/restaurant")]
    [ApiController]
    [Authorize(Roles = "RestaurantOwner,restaurant_owner")]
    public class RestaurantOrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly RabbitMqConnectionService _rabbitMq;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public RestaurantOrdersController(
            IOrderService orderService,
            RabbitMqConnectionService rabbitMq)
        {
            _orderService = orderService;
            _rabbitMq = rabbitMq;
        }

        /// <summary>
        /// Retrieves paginated orders belonging to the restaurant.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRestaurantOrders([FromQuery] string status, [FromQuery] int page = 0, [FromQuery] int size = 20)
        {
            var restaurantId = User.FindFirstValue("restaurant_id");
            if (string.IsNullOrEmpty(restaurantId)) return BadRequest("User is not associated with any restaurant.");
            return Ok(await _orderService.GetRestaurantOrdersAsync(restaurantId, status, page, size));
        }

        /// <summary>
        /// Confirms a held order, triggering the payment capture process via asynchronous SAGA.
        /// Publishes saga.command.confirm to RabbitMQ and returns 202 Accepted.
        /// </summary>
        [HttpPatch("{id}/confirm")]
        public async Task<IActionResult> ConfirmOrder(string id)
        {
            var restaurantId = User.FindFirstValue("restaurant_id");
            if (string.IsNullOrEmpty(restaurantId)) return BadRequest("User is not associated with any restaurant.");

            var command = new SagaCommand
            {
                CommandType = SagaBackgroundService.CMD_CONFIRM,
                OrderId     = id,
                AuthToken   = Request.Headers["Authorization"].ToString(),
                Payload     = JsonSerializer.SerializeToElement(new RestaurantActionData
                {
                    RestaurantId = restaurantId
                }, _jsonOpts)
            };

            await PublishCommandAsync(SagaBackgroundService.CMD_CONFIRM, command);

            return Accepted(new
            {
                orderId = id,
                status = "QUEUED",
                message = "Restoran onay komutu kuyruğa eklendi. Durumu sorgulamak için GET /saga/orders/{orderId} kullanın.",
                pollingUrl = $"/api/v1/saga/orders/{id}"
            });
        }

        /// <summary>
        /// Rejects a held order, triggering a payment hold release via asynchronous SAGA.
        /// Publishes saga.command.reject to RabbitMQ and returns 202 Accepted.
        /// </summary>
        [HttpPatch("{id}/reject")]
        public async Task<IActionResult> RejectOrder(string id)
        {
            var restaurantId = User.FindFirstValue("restaurant_id");
            if (string.IsNullOrEmpty(restaurantId)) return BadRequest("User is not associated with any restaurant.");

            var command = new SagaCommand
            {
                CommandType = SagaBackgroundService.CMD_REJECT,
                OrderId     = id,
                AuthToken   = Request.Headers["Authorization"].ToString(),
                Payload     = JsonSerializer.SerializeToElement(new RestaurantActionData
                {
                    RestaurantId = restaurantId,
                    Reason       = "restaurant_rejected"
                }, _jsonOpts)
            };

            await PublishCommandAsync(SagaBackgroundService.CMD_REJECT, command);

            return Accepted(new
            {
                orderId = id,
                status = "QUEUED",
                message = "Restoran red komutu kuyruğa eklendi.",
                pollingUrl = $"/api/v1/saga/orders/{id}"
            });
        }

        /// <summary>
        /// Manually updates the order status (e.g. PREPARING, DELIVERED).
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateOrderStatusRequest request)
        {
            var restaurantId = User.FindFirstValue("restaurant_id");
            if (string.IsNullOrEmpty(restaurantId)) return BadRequest("User is not associated with any restaurant.");
            return Ok(await _orderService.UpdateOrderStatusAsync(restaurantId, id, request.Status));
        }

        // ─── Yardımcı: RabbitMQ'ya komut yayınla ──────────────────────────────

        private async Task PublishCommandAsync(string routingKey, SagaCommand command)
        {
            var json = JsonSerializer.Serialize(command, _jsonOpts);
            var body = Encoding.UTF8.GetBytes(json);

            var channel = await _rabbitMq.GetChannelAsync();

            var properties = new BasicProperties
            {
                ContentType   = "application/json",
                DeliveryMode  = DeliveryModes.Persistent,
                MessageId     = command.CommandId,
                Timestamp     = new AmqpTimestamp(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Type          = routingKey
            };

            await channel.BasicPublishAsync(
                exchange: RabbitMqConnectionService.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            Serilog.Log.Information(
                "[RestaurantOrders] 📤 Komut kuyruğa yayınlandı: {RoutingKey} | CommandId={CommandId}",
                routingKey, command.CommandId);
        }
    }
}
