namespace CleanArchitecture.Core.Interfaces
{
    /// <summary>
    /// SAGA arka plan işlemleri için kimlik ve bağlam taşıyıcı.
    /// 
    /// Problem: SagaBackgroundService HTTP pipeline dışında çalışır,
    /// dolayısıyla IHttpContextAccessor.HttpContext her zaman null'dır.
    /// Bu arayüz, SagaController'dan gelen auth bilgisini
    /// BackgroundService → Orchestrator → OrderService/PaymentService
    /// zincirine taşımak için kullanılır.
    /// 
    /// Yaşam Döngüsü: Scoped — her SAGA komutu işlenirken
    /// yeni bir scope oluşturulur ve bu accessor set edilir.
    /// </summary>
    public interface ISagaContextAccessor
    {
        /// <summary>
        /// Orijinal HTTP isteğindeki Authorization header değeri.
        /// Örn: "Bearer eyJ..."
        /// </summary>
        string AuthToken { get; set; }

        /// <summary>
        /// SAGA'yı başlatan kullanıcının ID'si.
        /// </summary>
        string UserId { get; set; }

        /// <summary>
        /// Bağlam mevcut mu? (BackgroundService tarafından set edildi mi?)
        /// </summary>
        bool HasContext { get; }
    }
}
