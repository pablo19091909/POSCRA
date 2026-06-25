using POS.Api.Domain;

namespace POS.Api.Application;

public interface ITokenService
{
    bool CanIssueTokens();
    TokenResult CreateToken(UserAccount user, IReadOnlyCollection<string> permissions);
}
