using Atlas.Modules.Wiki.Domain.Enums;
using FluentValidation;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

public class CreateWikiPageCommandValidator : AbstractValidator<CreateWikiPageCommand>
{
    public CreateWikiPageCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık boş olamaz.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("İçerik boş olamaz.");

        // DepartmentName için ARTIK bir kural YOK - normal bir kullanıcı için bu
        // alanın istemciden gelen değeri zaten Handler tarafından tamamen yok
        // sayılıyor (departman her zaman JWT'den geliyor, bkz.
        // CreateWikiPageCommandHandler). Burada NotEmpty şart koşsaydık,
        // departmanı Handler'da otomatik atanacak normal bir kullanıcının boş
        // gönderdiği (ki göndermesi GEREKEN) istek, Handler'a hiç ulaşmadan bu
        // validasyona takılırdı. Departmanın GERÇEKTEN boş olma durumu (departmansız
        // bir kullanıcı) zaten Handler'da kendi anlaşılır hatasıyla (ArgumentException)
        // ele alınıyor - burada tekrar kontrol etmeye gerek yok.

        // Domain'deki Enum.Parse çağrısı geçersiz bir değerde exception fırlatıp
        // 400 yerine yanlışlıkla 500'e düşebilirdi - burada erken, alan bazlı bir
        // hata mesajıyla kesiyoruz.
        RuleFor(x => x.Visibility)
            .NotEmpty().WithMessage("Görünürlük boş olamaz.")
            .Must(v => Enum.TryParse<WikiVisibility>(v, ignoreCase: true, out _))
            .WithMessage("Görünürlük 'Public' ya da 'DepartmentOnly' olmalı.");

        // Ham, normalize edilmemiş kullanıcı girdisi üzerinde bir üst sınır -
        // asıl normalizasyon (trim/küçük harf/tekrarsız) Domain'de (bkz.
        // WikiPage.NormalizeTags), burada sadece anlamsız derecede uzun bir
        // string'in DB sütununu (300) aşıp 500'e düşmesini erken kesiyoruz.
        RuleFor(x => x.Tags)
            .MaximumLength(300).WithMessage("Etiketler 300 karakteri geçemez.");
    }
}
