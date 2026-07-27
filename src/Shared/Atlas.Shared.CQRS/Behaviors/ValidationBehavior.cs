using FluentValidation;
using MediatR;

namespace Atlas.Shared.CQRS.Behaviors;

/// <summary>
/// LoggingBehavior gibi bu da tamamen generic - hiçbir modülü tanımıyor,
/// sadece "TRequest için kayıtlı bir FluentValidation validator'ı var mı?" diye soruyor.
///
/// Neden Domain'deki "if (string.IsNullOrWhiteSpace(...)) throw" mantığının YERİNE değil
/// de ONUN ÖNÜNE geçiyor: Domain validasyonu hâlâ duruyor ve son güvenlik ağı olarak
/// kalmalı (Command her zaman MediatR üzerinden gelmeyebilir - örn. ileride bir test
/// veya başka bir iç servis doğrudan Domain'i çağırabilir). Bu behavior sadece "istek
/// Handler'a hiç ulaşmadan, alan bazlı düzgün bir hata mesajıyla" erken kesiyor.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Bu TRequest için hiç validator yazılmamışsa (örn. GetAllUsersQuery gibi
        // parametresiz Query'ler) döngü hiç çalışmaz, direkt Handler'a geçilir.
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
