using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Wrappers;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Middlewares
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public ErrorHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                var response = context.Response;
                response.ContentType = "application/json";

                StandardErrorResponse errorResponse;

                switch (error)
                {
                    case NotFoundException e:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        errorResponse = StandardErrorResponse.Create(e.ErrorCode, e.Message, (int)HttpStatusCode.NotFound);
                        break;
                    case Core.Exceptions.ApiException e:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        errorResponse = StandardErrorResponse.Create("BAD_REQUEST", e.Message, (int)HttpStatusCode.BadRequest);
                        break;
                    case ValidationException e:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        errorResponse = StandardErrorResponse.Create("VALIDATION_ERROR", "Some validation errors occurred.", (int)HttpStatusCode.BadRequest);
                        break;
                    case KeyNotFoundException e:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        errorResponse = StandardErrorResponse.Create("NOT_FOUND", e.Message ?? "Resource not found", (int)HttpStatusCode.NotFound);
                        break;
                    case UnauthorizedAccessException e:
                        response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        errorResponse = StandardErrorResponse.Create("UNAUTHORIZED", "You are not authorized.", (int)HttpStatusCode.Unauthorized);
                        break;
                    default:
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        errorResponse = StandardErrorResponse.Create("INTERNAL_SERVER_ERROR", error.ToString(), (int)HttpStatusCode.InternalServerError);
                        break;
                }

                var result = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await response.WriteAsync(result);
            }
        }
    }
}
