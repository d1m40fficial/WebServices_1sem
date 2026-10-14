using System.ComponentModel.DataAnnotations;

namespace Server.Dtos;

/// <summary>Группа в ответе API: включает количество контактов в группе.</summary>
public record ContactGroupDto(int Id, string Name, string? Description, int ContactsCount);

/// <summary>Данные для создания или изменения группы.</summary>
public record ContactGroupWriteDto(
    [property: Required(ErrorMessage = "Название группы обязательно.")]
    [property: StringLength(50, ErrorMessage = "Название не должно превышать 50 символов.")]
    string Name,

    [property: StringLength(200, ErrorMessage = "Описание не должно превышать 200 символов.")]
    string? Description);

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
public record ContactWriteDto(
    [property: Required(ErrorMessage = "ФИО обязательно.")]
    [property: StringLength(100, ErrorMessage = "ФИО не должно превышать 100 символов.")]
    string FullName,

    [property: StringLength(25, ErrorMessage = "Телефон не должен превышать 25 символов.")]
    string? Phone,

    [property: EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    [property: StringLength(100, ErrorMessage = "E-mail не должен превышать 100 символов.")]
    string? Email,

    [property: StringLength(100, ErrorMessage = "Должность не должна превышать 100 символов.")]
    string? Position,

    int? GroupId);
