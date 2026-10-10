using Dapper;
using Microsoft.AspNetCore.Mvc;
using MyFirstWebApp.Data;
using MyFirstWebApp.Models;

namespace MyFirstWebApp.Controllers;

/// <summary>
/// CRUD-операции над справочником контактов:
/// выборка всех элементов, выборка по условию (фильтрация),
/// добавление, редактирование и удаление.
/// </summary>
public class ContactsController : Controller
{
    private readonly AppDatabase _database;

    public ContactsController(AppDatabase database)
    {
        _database = database;
    }

    // Базовый SELECT со сбором названия группы через LEFT JOIN.
    private const string SelectContactsSql = """
        SELECT  c.Id,
                c.FullName,
                c.Phone,
                c.Email,
                c.Position,
                c.GroupId,
                g.Name AS GroupName
        FROM    Contacts AS c
        LEFT JOIN ContactGroups AS g ON g.Id = c.GroupId
        """;

    /// <summary>
    /// Выборка всех элементов. Если заданы условия фильтрации (поисковая строка
    /// и/или группа), выполняется выборка по условию.
    /// </summary>
    public async Task<IActionResult> Index(string? search, int? groupId)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        // Условие 1: поиск по ФИО, телефону, e-mail или должности.
        if (!string.IsNullOrWhiteSpace(search))
        {
            conditions.Add("""
                (c.FullName  LIKE @search
                 OR IFNULL(c.Phone, '')    LIKE @search
                 OR IFNULL(c.Email, '')    LIKE @search
                 OR IFNULL(c.Position, '') LIKE @search)
                """);
            parameters.Add("search", $"%{search.Trim()}%");
        }

        // Условие 2: принадлежность к конкретной группе.
        if (groupId is > 0)
        {
            conditions.Add("c.GroupId = @groupId");
            parameters.Add("groupId", groupId.Value);
        }

        var where = conditions.Count > 0
            ? " WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        var sql = SelectContactsSql + where + " ORDER BY c.FullName;";

        using var connection = _database.CreateConnection();
        var contacts = (await connection.QueryAsync<Contact>(sql, parameters)).ToList();

        ViewData["Search"] = search;
        ViewData["GroupId"] = groupId;
        ViewData["Groups"] = await GetGroupsAsync();

        return View(contacts);
    }

    /// <summary>Выборка одного элемента по идентификатору.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var contact = await GetContactAsync(id);

        if (contact is null)
        {
            return NotFound();
        }

        return View(contact);
    }

    /// <summary>Форма добавления элемента.</summary>
    public async Task<IActionResult> Create()
    {
        await FillGroupsAsync();

        return View(new Contact());
    }

    /// <summary>Добавление элемента в базу данных.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Contact contact)
    {
        if (!ModelState.IsValid)
        {
            await FillGroupsAsync();
            return View(contact);
        }

        const string sql = """
            INSERT INTO Contacts (FullName, Phone, Email, Position, GroupId)
            VALUES (@FullName, @Phone, @Email, @Position, @GroupId);
            """;

        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(sql, contact);

        TempData["Success"] = $"Контакт «{contact.FullName}» добавлен.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Форма редактирования элемента.</summary>
    public async Task<IActionResult> Edit(int id)
    {
        var contact = await GetContactAsync(id);

        if (contact is null)
        {
            return NotFound();
        }

        await FillGroupsAsync();

        return View(contact);
    }

    /// <summary>Сохранение изменений элемента в базе данных.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Contact contact)
    {
        if (id != contact.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await FillGroupsAsync();
            return View(contact);
        }

        const string sql = """
            UPDATE Contacts
            SET    FullName = @FullName,
                   Phone    = @Phone,
                   Email    = @Email,
                   Position = @Position,
                   GroupId  = @GroupId
            WHERE  Id = @Id;
            """;

        using var connection = _database.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, contact);

        if (affected == 0)
        {
            return NotFound();
        }

        TempData["Success"] = $"Контакт «{contact.FullName}» обновлён.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Форма подтверждения удаления.</summary>
    public async Task<IActionResult> Delete(int id)
    {
        var contact = await GetContactAsync(id);

        if (contact is null)
        {
            return NotFound();
        }

        return View(contact);
    }

    /// <summary>Удаление элемента из базы данных.</summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        const string sql = "DELETE FROM Contacts WHERE Id = @id;";

        using var connection = _database.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, new { id });

        if (affected == 0)
        {
            return NotFound();
        }

        TempData["Success"] = "Контакт удалён.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<Contact?> GetContactAsync(int id)
    {
        const string sql = SelectContactsSql + " WHERE c.Id = @id;";

        using var connection = _database.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Contact>(sql, new { id });
    }

    private async Task<List<ContactGroup>> GetGroupsAsync()
    {
        const string sql = "SELECT Id, Name, Description FROM ContactGroups ORDER BY Name;";

        using var connection = _database.CreateConnection();
        var groups = (await connection.QueryAsync<ContactGroup>(sql)).ToList();

        return groups;
    }

    private async Task FillGroupsAsync()
    {
        ViewData["Groups"] = await GetGroupsAsync();
    }
}
