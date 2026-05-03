namespace CleanArchitecture.Core.Settings
{
    public class ServiceSettings
    {
        public string UserService { get; set; } = "http://user-service:8000";
        public string OrderService { get; set; } = "http://order-service:8082";
        public string RestaurantService { get; set; } = "http://restaurant-service:5000";
        public string PaymentService { get; set; } = "http://payment-service:3000";
    }
}
