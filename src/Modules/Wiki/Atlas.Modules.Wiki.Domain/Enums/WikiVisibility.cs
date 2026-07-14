namespace Atlas.Modules.Wiki.Domain.Enums;

/// <summary>
/// Dokümandaki "yetki dahilinde herkese açık veya departmana açık" fikrinin
/// Domain karşılığı. Şimdilik iki seçenek yeterli - ileride "sadece belirli
/// kişilere açık" gibi seçenekler eklenirse buraya yeni değer eklenir.
/// </summary>
public enum WikiVisibility
{
    Public = 0,
    DepartmentOnly = 1
}
