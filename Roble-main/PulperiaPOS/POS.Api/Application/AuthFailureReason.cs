namespace POS.Api.Application;

public enum AuthFailureReason
{
    None,
    InvalidRequest,
    InvalidCredentials,
    AuthConfigurationUnavailable
}
