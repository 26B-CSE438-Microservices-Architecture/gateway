using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.Wrappers
{
    public class StandardErrorResponse
    {
        [JsonPropertyName("error")]
        public ErrorDetail Error { get; set; }

        public static StandardErrorResponse Create(string code, string message, int status)
            => new StandardErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = code,
                    Message = message,
                    Status = status
                }
            };
    }

    public class ErrorDetail
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }
    }
}
