using MediatR;

namespace Atlas.Modules.Auth.Application.Users.Commands;

/// <summary>
/// Query'lerden farkına dikkat et: Command'lar genelde "bir şeyi değiştir" der,
/// dönüş değeri de genelde ya hiç yoktur ya da sadece "id" gibi minimal bir şeydir -
/// Query gibi zengin bir veri seti döndürmez. Burada geriye sadece yeni kullanıcının
/// Id'sini döndürüyoruz.
/// </summary>
public record RegisterUserCommand(string Email, string FullName, string Password, string? Department = null) : IRequest<Guid>;
