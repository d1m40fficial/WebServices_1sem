using System.ComponentModel.DataAnnotations;

namespace MyFirstWebApp.Models;

/// <summary>Группа (отдел), в которую входит контакт.</summary>
public class ContactGroup
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Название группы")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    /// <summary>Количество контактов в группе. Заполняется при выборке из БД.</summary>
    [Display(Name = "Контактов")]
    public int ContactsCount { get; set; }
}
