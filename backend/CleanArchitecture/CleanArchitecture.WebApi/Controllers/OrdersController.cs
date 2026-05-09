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
    [Route("api/v1/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly RabbitMqConnectionService _rabbitMq;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public OrdersController(
            IOrderService orderService,
            RabbitMqConnectionService rabbitMq)
        {
            _orderService = orderService;
            _rabbitMq = rabbitMq;
        }

        /// <summary>
        /// Retrieves the current user's order history with pagination and status filtering.
        /// </summary>
        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders([FromQuery] string status, [FromQuery] int page = 0, [FromQuery] int size = 10)
        {
            var userId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            return Ok(await _orderService.GetMyOrdersAsync(userId, status, page, size));
        }

        /// <summary>
        /// Retrieves detailed information about a specific order.
        /// </summary>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(string id)
        {
            var userId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            return Ok(await _orderService.GetOrderByIdAsync(userId, id));
        }

        /// <summary>
        /// Cancels a pending or held order via asynchronous SAGA Orchestrator.
        /// Publishes saga.command.cancel to RabbitMQ and returns 202 Accepted.
        /// </summary>
        [Authorize]
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(string id)
        {
            var userId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var command = new SagaCommand
            {
                CommandType = SagaBackgroundService.CMD_CANCEL,
                OrderId     = id,
                UserId      = userId,
                AuthToken   = Request.Headers["Authorization"].ToString()
            };

            await PublishCommandAsync(SagaBackgroundService.CMD_CANCEL, command);

            return Accepted(new
            {
                orderId = id,
                status = "QUEUED",
                message = "İptal komutu kuyruğa eklendi. Durumu sorgulamak için GET /saga/orders/{orderId} kullanın.",
                pollingUrl = $"/api/v1/saga/orders/{id}"
            });
        }

        /// <summary>
        /// Re-populates the basket with items from a previous order.
        /// </summary>
        [Authorize]
        [HttpPost("{id}/reorder")]
        public async Task<IActionResult> Reorder(string id)
        {
            var userId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            return Ok(await _orderService.ReorderAsync(userId, id));
        }

        /// <summary>
        /// Initiates a refund request for a paid or delivered order.
        /// </summary>
        [Authorize]
        [HttpPost("{id}/request-refund")]
        public async Task<IActionResult> RequestRefund(string id)
        {
            var userId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            return Ok(await _orderService.RequestRefundAsync(userId, id));
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
                "[Orders] 📤 Komut kuyruğa yayınlandı: {RoutingKey} | CommandId={CommandId}",
                routingKey, command.CommandId);
        }
    }
}
