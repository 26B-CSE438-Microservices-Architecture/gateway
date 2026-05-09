using CleanArchitecture.Core.Interfaces;

namespace CleanArchitecture.Infrastructure.Services
{
    /// <summary>
    /// ISagaContextAccessor implementasyonu.
    /// Scoped olarak register edilir — her SAGA komutu işlenirken
    /// BackgroundService tarafından set edilir.
    /// </summary>
    public class SagaContextAccessor : ISagaContextAccessor
    {
        public string AuthToken { get; set; }
        public string UserId { get; set; }
        public bool HasContext => !string.IsNullOrEmpty(AuthToken) || !string.IsNullOrEmpty(UserId);
    }
}
