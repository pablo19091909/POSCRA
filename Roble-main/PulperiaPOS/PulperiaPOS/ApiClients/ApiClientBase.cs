using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PulperiaPOS.Configuration;
using PulperiaPOS.Models.Api;
using PulperiaPOS.Models.Auth;

namespace PulperiaPOS.ApiClients
{
    public abstract class ApiClientBase : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly HttpClient httpClient;
        private readonly bool ownsHttpClient;
        private bool disposed;

        protected ApiClientBase()
            : this(CreateDefaultHttpClient(), ownsHttpClient: true)
        {
        }

        protected ApiClientBase(HttpClient httpClient, bool ownsHttpClient = false)
        {
            this.httpClient = httpClient;
            this.ownsHttpClient = ownsHttpClient;
        }

        protected HttpClient HttpClient => httpClient;

        protected async Task<ApiRequestResult<T>> SendAsync<T>(
            HttpMethod method,
            string relativePath,
            bool requiresAuthentication,
            object? body = null,
            CancellationToken cancellationToken = default)
        {
            if (requiresAuthentication && !TryApplyBearerToken())
            {
                ApiSessionCoordinator.NotifySessionExpired("La sesión expiró. Inicia sesión nuevamente.");
                return ApiRequestResult<T>.Failed(ApiErrorType.SessionExpired, "La sesión expiró. Inicia sesión nuevamente.");
            }

            try
            {
                using var request = new HttpRequestMessage(method, relativePath);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (body is not null)
                {
                    request.Content = JsonContent.Create(body, options: JsonOptions);
                }

                using var response = await HttpClient.SendAsync(request, cancellationToken);
                return await CreateResultAsync<T>(response, cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return ApiRequestResult<T>.Failed(ApiErrorType.Timeout, ApiSafeMessages.NetworkUnavailable);
            }
            catch (HttpRequestException)
            {
                return ApiRequestResult<T>.Failed(ApiErrorType.Network, ApiSafeMessages.NetworkUnavailable);
            }
            catch (JsonException)
            {
                return ApiRequestResult<T>.Failed(ApiErrorType.InvalidResponse, ApiSafeMessages.InvalidResponse);
            }
            catch (NotSupportedException)
            {
                return ApiRequestResult<T>.Failed(ApiErrorType.InvalidResponse, ApiSafeMessages.InvalidResponse);
            }
        }

        private bool TryApplyBearerToken()
        {
            HttpClient.DefaultRequestHeaders.Authorization = null;

            if (!UserSession.IsApiAuthenticated ||
                string.IsNullOrWhiteSpace(UserSession.AccessToken) ||
                !UserSession.TokenExpiresAtUtc.HasValue ||
                UserSession.TokenExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
            {
                return false;
            }

            HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", UserSession.AccessToken);
            return true;
        }

        private static async Task<ApiRequestResult<T>> CreateResultAsync<T>(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                if (response.Content.Headers.ContentLength == 0 ||
                    response.StatusCode == HttpStatusCode.NoContent)
                {
                    return ApiRequestResult<T>.Succeeded(default);
                }

                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                return ApiRequestResult<T>.Succeeded(data);
            }

            var traceId = await TryReadTraceIdAsync(response, cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest => ApiRequestResult<T>.Failed(
                    ApiErrorType.BadRequest,
                    ApiSafeMessages.BadRequest,
                    traceId),
                HttpStatusCode.Unauthorized => HandleUnauthorized<T>(traceId),
                HttpStatusCode.Forbidden => ApiRequestResult<T>.Failed(
                    ApiErrorType.Forbidden,
                    ApiSafeMessages.Forbidden,
                    traceId),
                HttpStatusCode.Conflict => ApiRequestResult<T>.Failed(
                    ApiErrorType.Conflict,
                    "La venta ya está en proceso o la solicitud no coincide con el reintento anterior.",
                    traceId),
                (HttpStatusCode)429 => ApiRequestResult<T>.Failed(
                    ApiErrorType.RateLimited,
                    ApiSafeMessages.RateLimited,
                    traceId),
                HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable => ApiRequestResult<T>.Failed(
                    ApiErrorType.ServiceError,
                    ApiSafeMessages.ServiceError,
                    traceId),
                _ => ApiRequestResult<T>.Failed(
                    ApiErrorType.ServiceError,
                    ApiSafeMessages.ServiceError,
                    traceId)
            };
        }

        private static ApiRequestResult<T> HandleUnauthorized<T>(string? traceId)
        {
            ApiSessionCoordinator.NotifySessionExpired("La sesión expiró. Inicia sesión nuevamente.");
            return ApiRequestResult<T>.Failed(ApiErrorType.Unauthorized, ApiSafeMessages.SessionExpired, traceId);
        }

        private static async Task<string?> TryReadTraceIdAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            try
            {
                var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions, cancellationToken);
                return string.IsNullOrWhiteSpace(error?.TraceId) ? null : error.TraceId;
            }
            catch
            {
                return null;
            }
        }

        private static HttpClient CreateDefaultHttpClient()
        {
            var httpClient = new HttpClient
            {
                BaseAddress = ResolveBaseAddress(),
                Timeout = ResolveTimeout()
            };

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }

        private static Uri ResolveBaseAddress()
        {
            var configuredBaseUrl = AppConfiguration.Current["Api:BaseUrl"];
            if (string.IsNullOrWhiteSpace(configuredBaseUrl))
            {
                throw new InvalidOperationException("La URL base de POS.Api no esta configurada.");
            }

            if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("La URL base de POS.Api debe usar HTTPS.");
            }

            return uri;
        }

        private static TimeSpan ResolveTimeout()
        {
            var configuredTimeout = AppConfiguration.Current["Api:RequestTimeoutSeconds"];
            return int.TryParse(configuredTimeout, out var seconds) && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : TimeSpan.FromSeconds(30);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (ownsHttpClient)
            {
                httpClient.Dispose();
            }

            disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
