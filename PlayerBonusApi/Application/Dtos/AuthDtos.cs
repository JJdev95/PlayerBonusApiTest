namespace PlayerBonusApi.Application.Dtos;

public sealed record DevTokenRequest(
    string UserId,
    string UserName,
    string? Role = null
);

public sealed record DevTokenResponse(
    string AccessToken,
    DateTime ExpiresAtUtc
);
