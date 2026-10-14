using Client.Models;
using Client.Services;
using Microsoft.AspNetCore.Mvc;

namespace Client.Controllers;

/// <summary>
/// Клиентский контроллер справочника контактов.
/// Все данные получаются и изменяются через Web API с помощью <see cref="ContactsApiClient"/>.
/// </summary>
public class ContactsController : Controller
{
    private readonly ContactsApiClient _api;

    public ContactsController(ContactsApiClient api)
    {
        _api = api;
    }

    /// <summary>Список контактов с фильтрацией по строке поиска и группе.</summary>
    public async Task<IActionResult> Index(string? search, int? groupId)
    {
        var contacts = await _api.GetContactsAsync(search, groupId);

        ViewData["Groups"] = await _api.GetGroupsAsync();
        ViewData["Search"] = search;
        ViewData["GroupId"] = groupId;

        return View(contacts);
    }

    public async Task<IActionResult> Details(int id)
    {
        var contact = await _api.GetContactAsync(id);

        if (contact is null)
        {
            return NotFound();
        }

        return View(contact);
    }

    public async Task<IActionResult> Create()
    {
        await FillGroupsAsync();

        return View(new ContactInput());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContactInput input)
    {
        if (!ModelState.IsValid)
        {
            await FillGroupsAsync();
            return View(input);
        }

        var result = await _api.CreateContactAsync(input);

        if (!result.Success)
        {
            AddApiErrors(result);
            await FillGroupsAsync();
            return View(input);
        }

        TempData["Success"] = $"Контакт «{input.FullName}» добавлен.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var contact = await _api.GetContactAsync(id);

        if (contact is null)
        {
            return NotFound();
        }

        await FillGroupsAsync();
        ViewData["Id"] = id;

        return View(new ContactInput
        {
            FullName = contact.FullName,
            Phone = contact.Phone,
            Email = contact.Email,
            Position = contact.Position,
            GroupId = contact.GroupId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ContactInput input)
    {
        ViewData["Id"] = id;

        if (!ModelState.IsValid)
        {
            await FillGroupsAsync();
            return View(input);
        }

        var result = await _api.UpdateContactAsync(id, input);

        if (!result.Success)
        {
            AddApiErrors(result);
            await FillGroupsAsync();
            return View(input);
        }

        TempData["Success"] = $"Контакт «{input.FullName}» обновлён.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var contact = await _api.GetContactAsync(id);

        if (contact is null)
        {
            return NotFound();
        }

        return View(contact);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _api.DeleteContactAsync(id);

        if (!result.Success)
        {
            TempData["Error"] = string.Join(" ", result.Errors.SelectMany(e => e.Value));
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Контакт удалён.";
        return RedirectToAction(nameof(Index));
    }

    private async Task FillGroupsAsync()
    {
        ViewData["Groups"] = await _api.GetGroupsAsync();
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
