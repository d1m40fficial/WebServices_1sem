using System.ComponentModel.DataAnnotations;

namespace Server.Dtos;

/// <summary>Группа в ответе API: включает количество контактов в группе.</summary>
public record ContactGroupDto(int Id, string Name, string? Description, int ContactsCount);

/// <summary>Данные для создания или изменения группы.</summary>
public class ContactGroupWriteDto
{
    [Required(ErrorMessage = "Название группы обязательно.")]
    [StringLength(50, ErrorMessage = "Название не должно превышать 50 символов.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Описание не должно превышать 200 символов.")]
    public string? Description { get; set; }
}

/// <summary>Контакт в ответе API: включает название группы, полученное по связи.</summary>
public record ContactDto(
    int Id,
    string FullName,
    string? Phone,
    string? Email,
    string? Position,
    int? GroupId,
    string? GroupName);

/// <summary>Данные для создания или изменения контакта.</summary>
public class ContactWriteDto
{
    [Required(ErrorMessage = "ФИО обязательно.")]
    [StringLength(100, ErrorMessage = "ФИО не должно превышать 100 символов.")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(25, ErrorMessage = "Телефон не должен превышать 25 символов.")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    [StringLength(100, ErrorMessage = "E-mail не должен превышать 100 символов.")]
    public string? Email { get; set; }

    [StringLength(100, ErrorMessage = "Должность не должна превышать 100 символов.")]
    public string? Position { get; set; }

    public int? GroupId { get; set; }
}
