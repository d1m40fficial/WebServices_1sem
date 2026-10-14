namespace Server.Models;

/// <summary>Группа (отдел), в которую входит один или несколько контактов.</summary>
public class ContactGroup
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Навигационное свойство: контакты данной группы (связь «один-ко-многим»).</summary>
    public List<Contact> Contacts { get; set; } = new();
}
