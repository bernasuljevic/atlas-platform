using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Application.Documents;
using FluentValidation;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

// UploadDocumentCommandValidator'ın dosyayla ilgili kurallarının AYNISI
// (AllowedDocumentExtensions/boyut sınırı paylaşılıyor) - Title/Visibility
// kuralları YOK çünkü bu Command o alanları hiç taşımıyor.
public class UploadNewDocumentVersionCommandValidator : AbstractValidator<UploadNewDocumentVersionCommand>
{
    public UploadNewDocumentVersionCommandValidator(FileStorageOptions storageOptions)
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.OriginalFileName).NotEmpty().WithMessage("Dosya adı boş olamaz.");
        RuleFor(x => x.ContentType).NotEmpty().WithMessage("İçerik tipi boş olamaz.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("Boş bir dosya yüklenemez.")
            .LessThanOrEqualTo(storageOptions.MaxFileSizeBytes)
            .WithMessage($"Dosya boyutu izin verilen üst sınırı ({storageOptions.MaxFileSizeBytes / (1024 * 1024)} MB) aşıyor.");

        RuleFor(x => x.OriginalFileName)
            .Must(name => AllowedDocumentExtensions.Values.Contains(Path.GetExtension(name).TrimStart('.')))
            .WithMessage("Desteklenmeyen dosya uzantısı.");
    }
}
