namespace Server.Models;

/// <summary>Контактное лицо, принадлежащее одной группе.</summary>
public class Contact
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Position { get; set; }

    /// <summary>Внешний ключ на группу. Может быть пустым, если контакт без группы.</summary>
    public int? GroupId { get; set; }

    /// <summary>Навигационное свойство: группа контакта (обратная сторона связи).</summary>
    public ContactGroup? Group { get; set; }
}
