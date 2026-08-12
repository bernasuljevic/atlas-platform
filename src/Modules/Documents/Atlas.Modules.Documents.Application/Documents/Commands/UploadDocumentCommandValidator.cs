using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Application.Documents;
using Atlas.Modules.Documents.Domain.Enums;
using FluentValidation;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

public class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    // Kullanıcının orijinal spec'inin "3. Esnek Dosya Formatları" bölümündeki
    // kategorilerin (Documents/Presentations/Spreadsheets/Data-Technical/
    // Images/Video/Audio/Archive) TAMAMI. ZIP içeriği HİÇ AÇILMIYOR/taranmıyor
    // (sadece bir attachment olarak indirilebiliyor) - bu yüzden zip-bomb riski
    // yapısal olarak yok, kapsam dışı bırakmaya bile gerek kalmadı. Tehlikeli
    // uzantı denylist'inin (.exe/.dll/.bat vb.) daha da sıkılaştırılması P7'nin
    // (sertleştirme) konusu - bu liste zaten POZİTİF bir allowlist olduğu için
    // (aksi belirtilmedikçe her şey reddediliyor) o gün eklenecek olan şey yeni
    // bir kısıtlama değil, gözden geçirme/genişletme. Liste artık AllowedDocumentExtensions'ta
    // (P6 Gün 2) - UploadNewDocumentVersionCommandValidator de AYNI listeyi kullanıyor.
    public UploadDocumentCommandValidator(FileStorageOptions storageOptions)
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Başlık boş olamaz.").MaximumLength(200);
        RuleFor(x => x.OriginalFileName).NotEmpty().WithMessage("Dosya adı boş olamaz.");
        RuleFor(x => x.ContentType).NotEmpty().WithMessage("İçerik tipi boş olamaz.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("Boş bir dosya yüklenemez.")
            .LessThanOrEqualTo(storageOptions.MaxFileSizeBytes)
            .WithMessage($"Dosya boyutu izin verilen üst sınırı ({storageOptions.MaxFileSizeBytes / (1024 * 1024)} MB) aşıyor.");

        RuleFor(x => x.Visibility)
            .Must(v => Enum.TryParse<DocumentVisibility>(v, out _))
            .WithMessage("Görünürlük 'Public' ya da 'DepartmentOnly' olmalı.");

        RuleFor(x => x.OriginalFileName)
            .Must(name => AllowedDocumentExtensions.Values.Contains(Path.GetExtension(name).TrimStart('.')))
            .WithMessage("Desteklenmeyen dosya uzantısı.");
    }
}
