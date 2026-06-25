using Microsoft.Extensions.Options;
using POS.Api.Configuration;
using POS.Api.Contracts;
using POS.Api.Infrastructure.Data;
using POS.Api.Infrastructure.Security;

namespace POS.Api.Application;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILegacyPasswordVerifier _legacyPasswordVerifier;
    private readonly IPermissionProvider _permissionProvider;
    private readonly ITokenService _tokenService;
    private readonly AuthenticationOptions _authOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILegacyPasswordVerifier legacyPasswordVerifier,
        IPermissionProvider permissionProvider,
        ITokenService tokenService,
        IOptions<AuthenticationOptions> authOptions,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _legacyPasswordVerifier = legacyPasswordVerifier;
        _permissionProvider = permissionProvider;
        _tokenService = tokenService;
        _authOptions = authOptions.Value;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return AuthResult.Failed(AuthFailureReason.InvalidRequest);
        }

        var user = await _userRepository.FindByUsernameAsync(request.Username.Trim(), cancellationToken);
        if (user is null || user.Active == false || IsTemporarilyLocked(user.LockedUntilUtc))
        {
            return AuthResult.Failed(AuthFailureReason.InvalidCredentials);
        }

        var passwordIsValid = false;
        var shouldUpgradeLegacyHash = false;

        if (!string.IsNullOrWhiteSpace(user.ModernPasswordHash))
        {
            passwordIsValid = _passwordHasher.Verify(request.Password, user.ModernPasswordHash);
        }
        else
        {
            passwordIsValid = _legacyPasswordVerifier.Verify(request.Password, user.LegacyPasswordHash);
            shouldUpgradeLegacyHash = passwordIsValid && _authOptions.EnableLegacyHashUpgrade;
        }

        if (!passwordIsValid)
        {
            return AuthResult.Failed(AuthFailureReason.InvalidCredentials);
        }

        var permissions = _permissionProvider.GetPermissions(user.Role);
        if (permissions.Count == 0)
        {
            return AuthResult.Failed(AuthFailureReason.InvalidCredentials);
        }

        if (shouldUpgradeLegacyHash)
        {
            var modernHash = _passwordHasher.HashPassword(request.Password);
            var upgraded = await _userRepository.TryUpgradeLegacyPasswordHashAsync(user.Id, modernHash, cancellationToken);
            if (!upgraded)
            {
                _logger.LogInformation("Legacy password upgrade was requested but not applied.");
            }
        }

        if (!_tokenService.CanIssueTokens())
        {
            return AuthResult.Failed(AuthFailureReason.AuthConfigurationUnavailable);
        }

        var token = _tokenService.CreateToken(user, permissions);
        return AuthResult.Success(new LoginResponse(
            token.AccessToken,
            token.ExpiresAtUtc,
            new LoginUserResponse(user.Id, user.Username, user.Role, permissions)));
    }

    private static bool IsTemporarilyLocked(DateTimeOffset? lockedUntilUtc)
    {
        return lockedUntilUtc.HasValue && lockedUntilUtc.Value > DateTimeOffset.UtcNow;
    }
}
