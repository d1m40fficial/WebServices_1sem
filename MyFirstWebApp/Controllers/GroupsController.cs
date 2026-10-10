using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using MyFirstWebApp.Data;
using MyFirstWebApp.Models;

namespace MyFirstWebApp.Controllers;

/// <summary>
/// CRUD-операции над справочником групп.
/// </summary>
public class GroupsController : Controller
{
    private readonly AppDatabase _database;

    public GroupsController(AppDatabase database)
    {
        _database = database;
    }

    // Выборка групп вместе с количеством контактов в каждой из них.
    private const string SelectGroupsSql = """
        SELECT  g.Id,
                g.Name,
                g.Description,
                (SELECT COUNT(*) FROM Contacts AS c WHERE c.GroupId = g.Id) AS ContactsCount
        FROM    ContactGroups AS g
        """;

    /// <summary>
    /// Выборка всех групп. При наличии условия фильтрации — выборка по условию.
    /// </summary>
    public async Task<IActionResult> Index(string? search)
    {
        var parameters = new DynamicParameters();

        var where = string.Empty;
        if (!string.IsNullOrWhiteSpace(search))
        {
            where = " WHERE IFNULL(g.Name, '') LIKE @search OR IFNULL(g.Description, '') LIKE @search";
            parameters.Add("search", $"%{search.Trim()}%");
        }

        var sql = SelectGroupsSql + where + " ORDER BY g.Name;";

        using var connection = _database.CreateConnection();
        var groups = (await connection.QueryAsync<ContactGroup>(sql, parameters)).ToList();

        ViewData["Search"] = search;

        return View(groups);
    }

    /// <summary>Выборка группы и всех её контактов.</summary>
    public async Task<IActionResult> Details(int id)
    {
        const string groupSql = SelectGroupsSql + " WHERE g.Id = @id;";

        const string contactsSql = """
            SELECT  c.Id,
                    c.FullName,
                    c.Phone,
                    c.Email,
                    c.Position,
                    c.GroupId,
                    g.Name AS GroupName
            FROM    Contacts AS c
            LEFT JOIN ContactGroups AS g ON g.Id = c.GroupId
            WHERE   c.GroupId = @id
            ORDER BY c.FullName;
            """;

        using var connection = _database.CreateConnection();

        var group = await connection.QueryFirstOrDefaultAsync<ContactGroup>(groupSql, new { id });
        if (group is null)
        {
            return NotFound();
        }

        var contacts = (await connection.QueryAsync<Contact>(contactsSql, new { id })).ToList();

        ViewData["Contacts"] = contacts;

        return View(group);
    }

    public IActionResult Create()
    {
        return View(new ContactGroup());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContactGroup group)
    {
        if (!ModelState.IsValid)
        {
            return View(group);
        }

        const string sql = """
            INSERT INTO ContactGroups (Name, Description)
            VALUES (@Name, @Description);
            """;

        using var connection = _database.CreateConnection();

        if (!IsGroupNameUnique(connection, group.Name))
        {
            ModelState.AddModelError(nameof(group.Name), "Группа с таким названием уже существует.");
            return View(group);
        }

        await connection.ExecuteAsync(sql, group);

        TempData["Success"] = $"Группа «{group.Name}» добавлена.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var group = await GetGroupAsync(id);

        if (group is null)
        {
            return NotFound();
        }

        return View(group);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ContactGroup group)
    {
        if (id != group.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(group);
        }

        const string sql = """
            UPDATE ContactGroups
            SET    Name        = @Name,
                   Description = @Description
            WHERE  Id = @Id;
            """;

        using var connection = _database.CreateConnection();

        if (!IsGroupNameUnique(connection, group.Name, id))
        {
            ModelState.AddModelError(nameof(group.Name), "Группа с таким названием уже существует.");
            return View(group);
        }

        var affected = await connection.ExecuteAsync(sql, group);

        if (affected == 0)
        {
            return NotFound();
        }

        TempData["Success"] = $"Группа «{group.Name}» обновлена.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var group = await GetGroupAsync(id);

        if (group is null)
        {
            return NotFound();
        }

        return View(group);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // Контакты не удаляются вместе с группой: в схеме БД стоит ON DELETE SET NULL.
        const string sql = "DELETE FROM ContactGroups WHERE Id = @id;";

        using var connection = _database.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, new { id });

        if (affected == 0)
        {
            return NotFound();
        }

        TempData["Success"] = "Группа удалена. Контакты остались в справочнике без группы.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ContactGroup?> GetGroupAsync(int id)
    {
        const string sql = SelectGroupsSql + " WHERE g.Id = @id;";

        using var connection = _database.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ContactGroup>(sql, new { id });
    }

    private static bool IsGroupNameUnique(SqliteConnection connection, string name, int? excludeId = null)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM   ContactGroups
            WHERE  Name = @name
               AND (@excludeId IS NULL OR Id <> @excludeId);
            """;

        var count = connection.ExecuteScalar<long>(sql, new { name, excludeId });
        return count == 0;
    }
}
