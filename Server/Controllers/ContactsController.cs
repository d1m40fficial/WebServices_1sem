using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Dtos;
using Server.Models;

namespace Server.Controllers;

/// <summary>
/// REST-контроллер для работы со справочником контактов.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ContactsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ContactsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Выборка всех контактов. Если заданы параметры search и/или groupId —
    /// выполняется выборка по условию.
    /// </summary>
    /// <param name="search">Подстрока для поиска по ФИО, телефону, e-mail и должности.</param>
    /// <param name="groupId">Идентификатор группы, по которой нужно отфильтровать контакты.</param>
    /// <response code="200">Список контактов.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ContactDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ContactDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int? groupId = null)
    {
        var query = _db.Contacts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = search.Trim();
            query = query.Where(c =>
                c.FullName.Contains(pattern) ||
                (c.Phone != null && c.Phone.Contains(pattern)) ||
                (c.Email != null && c.Email.Contains(pattern)) ||
                (c.Position != null && c.Position.Contains(pattern)));
        }

        if (groupId is > 0)
        {
            query = query.Where(c => c.GroupId == groupId);
        }

        var contacts = await query
            .OrderBy(c => c.FullName)
            .Select(c => new ContactDto(c.Id, c.FullName, c.Phone, c.Email, c.Position, c.GroupId, c.Group != null ? c.Group.Name : null))
            .ToListAsync();

        return Ok(contacts);
    }

    /// <summary>Выборка одного контакта по идентификатору.</summary>
    /// <response code="200">Контакт найден.</response>
    /// <response code="404">Контакт не найден.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactDto>> GetById(int id)
    {
        var contact = await _db.Contacts.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ContactDto(c.Id, c.FullName, c.Phone, c.Email, c.Position, c.GroupId, c.Group != null ? c.Group.Name : null))
            .FirstOrDefaultAsync();

        if (contact is null)
        {
            return NotFound(new ProblemDetails { Title = $"Контакт с идентификатором {id} не найден.", Status = StatusCodes.Status404NotFound });
        }

        return Ok(contact);
    }

    /// <summary>Добавление нового контакта.</summary>
    /// <response code="201">Контакт создан.</response>
    /// <response code="400">Некорректные данные или указана несуществующая группа.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContactDto>> Create([FromBody] ContactWriteDto dto)
    {
        if (dto.GroupId is int groupId && !await _db.ContactGroups.AnyAsync(g => g.Id == groupId))
        {
            ModelState.AddModelError(nameof(dto.GroupId), $"Группа с идентификатором {groupId} не существует.");
            return ValidationProblem(ModelState);
        }

        var contact = new Contact
        {
            FullName = dto.FullName,
            Phone = dto.Phone,
            Email = dto.Email,
            Position = dto.Position,
            GroupId = dto.GroupId
        };

        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync();

        var result = await BuildDtoAsync(contact.Id);

        return CreatedAtAction(nameof(GetById), new { id = contact.Id }, result);
    }

    /// <summary>Редактирование существующего контакта.</summary>
    /// <response code="204">Контакт обновлён.</response>
    /// <response code="400">Некорректные данные или указана несуществующая группа.</response>
    /// <response code="404">Контакт не найден.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] ContactWriteDto dto)
    {
        var contact = await _db.Contacts.FindAsync(id);

        if (contact is null)
        {
            return NotFound(new ProblemDetails { Title = $"Контакт с идентификатором {id} не найден.", Status = StatusCodes.Status404NotFound });
        }

        if (dto.GroupId is int groupId && !await _db.ContactGroups.AnyAsync(g => g.Id == groupId))
        {
            ModelState.AddModelError(nameof(dto.GroupId), $"Группа с идентификатором {groupId} не существует.");
            return ValidationProblem(ModelState);
        }

        contact.FullName = dto.FullName;
        contact.Phone = dto.Phone;
        contact.Email = dto.Email;
        contact.Position = dto.Position;
        contact.GroupId = dto.GroupId;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Удаление контакта.</summary>
    /// <response code="204">Контакт удалён.</response>
    /// <response code="404">Контакт не найден.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var contact = await _db.Contacts.FindAsync(id);

        if (contact is null)
        {
            return NotFound(new ProblemDetails { Title = $"Контакт с идентификатором {id} не найден.", Status = StatusCodes.Status404NotFound });
        }

        _db.Contacts.Remove(contact);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<ContactDto> BuildDtoAsync(int id)
    {
        return await _db.Contacts.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ContactDto(c.Id, c.FullName, c.Phone, c.Email, c.Position, c.GroupId, c.Group != null ? c.Group.Name : null))
            .FirstAsync();
    }
}
