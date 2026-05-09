using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    /// <summary>
    /// RabbitMQ bağlantı yönetim servisi.
    /// Singleton olarak register edilir — tüm uygulama boyunca tek bağlantı kullanılır.
    /// 
    /// Connection string: RABBITMQ_URL environment variable'ından okunur.
    /// Varsayılan: amqp://guest:guest@localhost:5672
    /// 
    /// Bağlantı koptuğunda otomatik reconnect dener.
    /// Uygulama kapanırken Dispose ile bağlantı temizlenir.
    /// </summary>
    public class RabbitMqConnectionService : IDisposable
    {
        private readonly ILogger<RabbitMqConnectionService> _logger;
        private readonly string _connectionString;
        private IConnection _connection;
        private IChannel _channel;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly SemaphoreSlim _publishSemaphore = new(1, 1);
        private bool _disposed;
        private bool _exchangeDeclared;

        /// <summary>Exchange adı — tüm gateway event'leri bu exchange üzerinden yayınlanır</summary>
        public const string ExchangeName = "gateway.events";

        public RabbitMqConnectionService(IConfiguration configuration, ILogger<RabbitMqConnectionService> logger)
        {
            _logger = logger;
            _connectionString = configuration["RABBITMQ_URL"]
                             ?? configuration["RabbitMQ:Url"]
                             ?? "amqp://guest:guest@localhost:5672";

            _logger.LogInformation("RabbitMQ bağlantı adresi: {Url}", MaskPassword(_connectionString));
        }

        /// <summary>
        /// Aktif bir RabbitMQ channel döndürür.
        /// Bağlantı yoksa veya kopmuşsa yeniden oluşturur.
        /// </summary>
        public async Task<IChannel> GetChannelAsync()
        {
            if (_channel != null && _channel.IsOpen)
                return _channel;

            await _semaphore.WaitAsync();
            try
            {
                // Double-check pattern
                if (_channel != null && _channel.IsOpen)
                    return _channel;

                await ConnectAsync();
                return _channel;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>Bağlantı açık mı?</summary>
        public bool IsConnected => _connection != null && _connection.IsOpen;

        /// <summary>
        /// Thread-safe mesaj yayınlama metodu.
        /// Tüm controller ve publisher'lar bu metodu kullanmalıdır —
        /// paylaşılan IChannel eş zamanlı BasicPublishAsync çağrılarına karşı güvenli değildir.
        /// </summary>
        public async Task PublishAsync(
            string exchange,
            string routingKey,
            BasicProperties properties,
            byte[] body)
        {
            await _publishSemaphore.WaitAsync();
            try
            {
                var channel = await GetChannelAsync();
                await channel.BasicPublishAsync(
                    exchange: exchange,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);
            }
            finally
            {
                _publishSemaphore.Release();
            }
        }

        private async Task ConnectAsync()
        {
            try
            {
                // Eski bağlantıyı temizle
                if (_channel != null)
                {
                    try { await _channel.CloseAsync(); } catch { }
                    _channel.Dispose();
                    _channel = null;
                }
                if (_connection != null)
                {
                    try { await _connection.CloseAsync(); } catch { }
                    _connection.Dispose();
                    _connection = null;
                }

                var factory = new ConnectionFactory
                {
                    Uri = new Uri(_connectionString),
                    ClientProvidedName = "Gateway.API"
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                // Exchange'i declare et (yoksa oluşturur, varsa skip eder)
                if (!_exchangeDeclared)
                {
                    await _channel.ExchangeDeclareAsync(
                        exchange: ExchangeName,
                        type: ExchangeType.Topic,
                        durable: true,
                        autoDelete: false);
                    _exchangeDeclared = true;
                }

                _logger.LogInformation("✓ RabbitMQ bağlantısı kuruldu. Exchange: {Exchange}", ExchangeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ RabbitMQ bağlantısı kurulamadı: {Message}", ex.Message);
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _channel?.CloseAsync().GetAwaiter().GetResult();
                _channel?.Dispose();
                _connection?.CloseAsync().GetAwaiter().GetResult();
                _connection?.Dispose();
                _logger.LogInformation("RabbitMQ bağlantısı kapatıldı.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ bağlantısı kapatılırken hata oluştu.");
            }
        }

        private static string MaskPassword(string url)
        {
            try
            {
                var uri = new Uri(url);
                return $"{uri.Scheme}://{uri.UserInfo.Split(':')[0]}:****@{uri.Host}:{uri.Port}";
            }
            catch { return url; }
        }
    }
}
