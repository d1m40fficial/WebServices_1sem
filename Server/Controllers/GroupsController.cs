using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Dtos;
using Server.Models;

namespace Server.Controllers;

/// <summary>
/// REST-контроллер для работы со справочником групп.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class GroupsController : ControllerBase
{
    private readonly AppDbContext _db;

    public GroupsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Выборка всех групп. При передаче параметра search — выборка по условию.</summary>
    /// <param name="search">Подстрока для поиска по названию и описанию группы.</param>
    /// <response code="200">Список групп.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ContactGroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ContactGroupDto>>> GetAll([FromQuery] string? search = null)
    {
        var query = _db.ContactGroups.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = search.Trim();
            query = query.Where(g =>
                g.Name.Contains(pattern) ||
                (g.Description != null && g.Description.Contains(pattern)));
        }

        var groups = await query
            .OrderBy(g => g.Name)
            .Select(g => new ContactGroupDto(g.Id, g.Name, g.Description, g.Contacts.Count))
            .ToListAsync();

        return Ok(groups);
    }

    /// <summary>Выборка одной группы по идентификатору.</summary>
    /// <response code="200">Группа найдена.</response>
    /// <response code="404">Группа не найдена.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ContactGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactGroupDto>> GetById(int id)
    {
        var group = await _db.ContactGroups.AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new ContactGroupDto(g.Id, g.Name, g.Description, g.Contacts.Count))
            .FirstOrDefaultAsync();

        if (group is null)
        {
            return NotFound(new ProblemDetails { Title = $"Группа с идентификатором {id} не найдена.", Status = StatusCodes.Status404NotFound });
        }

        return Ok(group);
    }

    /// <summary>Выборка всех контактов указанной группы.</summary>
    /// <response code="200">Список контактов группы.</response>
    /// <response code="404">Группа не найдена.</response>
    [HttpGet("{id:int}/contacts")]
    [ProducesResponseType(typeof(IEnumerable<ContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ContactDto>>> GetContacts(int id)
    {
        if (!await _db.ContactGroups.AnyAsync(g => g.Id == id))
        {
            return NotFound(new ProblemDetails { Title = $"Группа с идентификатором {id} не найдена.", Status = StatusCodes.Status404NotFound });
        }

        var contacts = await _db.Contacts.AsNoTracking()
            .Where(c => c.GroupId == id)
            .OrderBy(c => c.FullName)
            .Select(c => new ContactDto(c.Id, c.FullName, c.Phone, c.Email, c.Position, c.GroupId, c.Group!.Name))
            .ToListAsync();

        return Ok(contacts);
    }

    /// <summary>Добавление новой группы.</summary>
    /// <response code="201">Группа создана.</response>
    /// <response code="400">Некорректные данные или группа с таким названием уже существует.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ContactGroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContactGroupDto>> Create([FromBody] ContactGroupWriteDto dto)
    {
        if (await _db.ContactGroups.AnyAsync(g => g.Name == dto.Name))
        {
            ModelState.AddModelError(nameof(dto.Name), "Группа с таким названием уже существует.");
            return ValidationProblem(ModelState);
        }

        var group = new ContactGroup
        {
            Name = dto.Name,
            Description = dto.Description
        };

        _db.ContactGroups.Add(group);
        await _db.SaveChangesAsync();

        var result = new ContactGroupDto(group.Id, group.Name, group.Description, 0);

        return CreatedAtAction(nameof(GetById), new { id = group.Id }, result);
    }

    /// <summary>Редактирование существующей группы.</summary>
    /// <response code="204">Группа обновлена.</response>
    /// <response code="400">Некорректные данные или группа с таким названием уже существует.</response>
    /// <response code="404">Группа не найдена.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] ContactGroupWriteDto dto)
    {
        var group = await _db.ContactGroups.FindAsync(id);

        if (group is null)
        {
            return NotFound(new ProblemDetails { Title = $"Группа с идентификатором {id} не найдена.", Status = StatusCodes.Status404NotFound });
        }

        if (await _db.ContactGroups.AnyAsync(g => g.Id != id && g.Name == dto.Name))
        {
            ModelState.AddModelError(nameof(dto.Name), "Группа с таким названием уже существует.");
            return ValidationProblem(ModelState);
        }

        group.Name = dto.Name;
        group.Description = dto.Description;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Удаление группы. Контакты группы остаются в справочнике без группы.</summary>
    /// <response code="204">Группа удалена.</response>
    /// <response code="404">Группа не найдена.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var group = await _db.ContactGroups.FindAsync(id);

        if (group is null)
        {
            return NotFound(new ProblemDetails { Title = $"Группа с идентификатором {id} не найдена.", Status = StatusCodes.Status404NotFound });
        }

        _db.ContactGroups.Remove(group);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
