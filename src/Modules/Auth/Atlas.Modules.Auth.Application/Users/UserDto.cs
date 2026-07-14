namespace Atlas.Modules.Auth.Application.Users;

/// <summary>
/// API'ye dönecek veri şekli. Domain'deki User'ı direkt vermiyoruz -
/// PasswordHash gibi hassas alanlar asla dışarı sızmasın diye.
/// </summary>
public record UserDto(Guid Id, string Email, string FullName);
