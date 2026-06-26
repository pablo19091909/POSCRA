using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PulperiaPOS.Models.Auth;

namespace PulperiaPOS.ApiClients
{
    public sealed class AuthApiClient : ApiClientBase
    {
        public async Task<AuthApiResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new LoginRequest(username, password);
                using var response = await HttpClient.PostAsJsonAsync("api/auth/login", request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
                    if (loginResponse?.User is null ||
                        string.IsNullOrWhiteSpace(loginResponse.AccessToken) ||
                        string.IsNullOrWhiteSpace(loginResponse.User.Role))
                    {
                        return AuthApiResult.Failed(AuthApiFailure.InvalidResponse);
                    }

                    return AuthApiResult.Succeeded(loginResponse);
                }

                return response.StatusCode switch
                {
                    HttpStatusCode.BadRequest => AuthApiResult.Failed(AuthApiFailure.InvalidCredentials),
                    HttpStatusCode.Unauthorized => AuthApiResult.Failed(AuthApiFailure.InvalidCredentials),
                    HttpStatusCode.TooManyRequests => AuthApiResult.Failed(AuthApiFailure.RateLimited),
                    HttpStatusCode.ServiceUnavailable => AuthApiResult.Failed(AuthApiFailure.ServiceUnavailable),
                    _ => AuthApiResult.Failed(AuthApiFailure.ServiceUnavailable)
                };
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return AuthApiResult.Failed(AuthApiFailure.NetworkError);
            }
            catch (HttpRequestException)
            {
                return AuthApiResult.Failed(AuthApiFailure.NetworkError);
            }
            catch (InvalidOperationException)
            {
                return AuthApiResult.Failed(AuthApiFailure.ConfigurationError);
            }
            catch (JsonException)
            {
                return AuthApiResult.Failed(AuthApiFailure.InvalidResponse);
            }
            catch (NotSupportedException)
            {
                return AuthApiResult.Failed(AuthApiFailure.InvalidResponse);
            }
        }
    }
}
