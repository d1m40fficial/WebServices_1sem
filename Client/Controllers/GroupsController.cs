using Client.Models;
using Client.Services;
using Microsoft.AspNetCore.Mvc;

namespace Client.Controllers;

/// <summary>
/// Клиентский контроллер справочника групп.
/// Все данные получаются и изменяются через Web API с помощью <see cref="ContactsApiClient"/>.
/// </summary>
public class GroupsController : Controller
{
    private readonly ContactsApiClient _api;

    public GroupsController(ContactsApiClient api)
    {
        _api = api;
    }

    /// <summary>Список групп с фильтрацией по строке поиска.</summary>
    public async Task<IActionResult> Index(string? search)
    {
        var groups = await _api.GetGroupsAsync(search);

        ViewData["Search"] = search;

        return View(groups);
    }

    /// <summary>Карточка группы со списком её контактов.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var group = await _api.GetGroupAsync(id);

        if (group is null)
        {
            return NotFound();
        }

        ViewData["Contacts"] = await _api.GetGroupContactsAsync(id);

        return View(group);
    }

    public IActionResult Create()
    {
        return View(new ContactGroupInput());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContactGroupInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var result = await _api.CreateGroupAsync(input);

        if (!result.Success)
        {
            AddApiErrors(result);
            return View(input);
        }

        TempData["Success"] = $"Группа «{input.Name}» добавлена.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var group = await _api.GetGroupAsync(id);

        if (group is null)
        {
            return NotFound();
        }

        ViewData["Id"] = id;

        return View(new ContactGroupInput
        {
            Name = group.Name,
            Description = group.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ContactGroupInput input)
    {
        ViewData["Id"] = id;

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var result = await _api.UpdateGroupAsync(id, input);

        if (!result.Success)
        {
            AddApiErrors(result);
            return View(input);
        }

        TempData["Success"] = $"Группа «{input.Name}» обновлена.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var group = await _api.GetGroupAsync(id);

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
        var result = await _api.DeleteGroupAsync(id);

        if (!result.Success)
        {
            TempData["Error"] = string.Join(" ", result.Errors.SelectMany(e => e.Value));
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Группа удалена. Контакты остались в справочнике без группы.";
        return RedirectToAction(nameof(Index));
    }

    private void AddApiErrors(ApiResult result)
    {
        foreach (var (field, messages) in result.Errors)
        {
            foreach (var message in messages)
            {
                ModelState.AddModelError(field, message);
            }
        }
    }
}
