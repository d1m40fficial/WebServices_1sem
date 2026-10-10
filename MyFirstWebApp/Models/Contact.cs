using System.ComponentModel.DataAnnotations;

namespace MyFirstWebApp.Models;

/// <summary>Контактное лицо, принадлежащее определённой группе.</summary>
public class Contact
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(25)]
    [Display(Name = "Телефон")]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(100)]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    [StringLength(100)]
    [Display(Name = "Должность")]
    public string? Position { get; set; }

    [Display(Name = "Группа")]
    public int? GroupId { get; set; }

    /// <summary>Название группы. Заполняется при выборке из БД через JOIN.</summary>
    public string? GroupName { get; set; }
}
