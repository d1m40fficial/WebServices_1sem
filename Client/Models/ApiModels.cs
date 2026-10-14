using System.ComponentModel.DataAnnotations;

namespace Client.Models;

/// <summary>Группа, как её возвращает API.</summary>
public class ContactGroupModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ContactsCount { get; set; }
}

/// <summary>Контакт, как его возвращает API.</summary>
public class ContactModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Position { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
}

/// <summary>Данные для создания и изменения группы.</summary>
public class ContactGroupInput
{
    [Required(ErrorMessage = "Название группы обязательно.")]
    [StringLength(50, ErrorMessage = "Название не должно превышать 50 символов.")]
    [Display(Name = "Название группы")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Описание не должно превышать 200 символов.")]
    [Display(Name = "Описание")]
    public string? Description { get; set; }
}

/// <summary>Данные для создания и изменения контакта.</summary>
public class ContactInput
{
    [Required(ErrorMessage = "ФИО обязательно.")]
    [StringLength(100, ErrorMessage = "ФИО не должно превышать 100 символов.")]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(25, ErrorMessage = "Телефон не должен превышать 25 символов.")]
    [Display(Name = "Телефон")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    [StringLength(100, ErrorMessage = "E-mail не должен превышать 100 символов.")]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    [StringLength(100, ErrorMessage = "Должность не должна превышать 100 символов.")]
    [Display(Name = "Должность")]
    public string? Position { get; set; }

    [Display(Name = "Группа")]
    public int? GroupId { get; set; }
}
