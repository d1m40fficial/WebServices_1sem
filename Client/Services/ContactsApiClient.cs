using System.Net;
using System.Net.Http.Json;
using Client.Models;
using Microsoft.AspNetCore.Mvc;

namespace Client.Services;

/// <summary>
/// Типизированный HTTP-клиент для обращения к Web API «Справочник контактов».
/// Инкапсулирует все адреса и форматы REST-запросов.
/// </summary>
public class ContactsApiClient
{
    private readonly HttpClient _http;

    public ContactsApiClient(HttpClient http)
    {
        _http = http;
    }

    // ---------------------------------------------------------------------
    // Группы
    // ---------------------------------------------------------------------

    /// <summary>Выборка всех групп либо поиск по подстроке.</summary>
    public async Task<List<ContactGroupModel>> GetGroupsAsync(string? search = null)
    {
        var url = string.IsNullOrWhiteSpace(search)
            ? "api/groups"
            : $"api/groups?search={Uri.EscapeDataString(search)}";

        return await _http.GetFromJsonAsync<List<ContactGroupModel>>(url) ?? new List<ContactGroupModel>();
    }

    /// <summary>Выборка одной группы.</summary>
    public Task<ContactGroupModel?> GetGroupAsync(int id) =>
        _http.GetFromJsonAsync<ContactGroupModel>($"api/groups/{id}");

    /// <summary>Выборка контактов указанной группы.</summary>
    public async Task<List<ContactModel>> GetGroupContactsAsync(int id) =>
        await _http.GetFromJsonAsync<List<ContactModel>>($"api/groups/{id}/contacts") ?? new List<ContactModel>();

    public Task<ApiResult<ContactGroupModel>> CreateGroupAsync(ContactGroupInput input) =>
        SendAsync<ContactGroupModel>(HttpMethod.Post, "api/groups", input);

    public Task<ApiResult> UpdateGroupAsync(int id, ContactGroupInput input) =>
        SendAsync(HttpMethod.Put, $"api/groups/{id}", input);

    public Task<ApiResult> DeleteGroupAsync(int id) =>
        SendAsync(HttpMethod.Delete, $"api/groups/{id}", null);

    // ---------------------------------------------------------------------
    // Контакты
    // ---------------------------------------------------------------------

    /// <summary>Выборка всех контактов с необязательной фильтрацией.</summary>
    public async Task<List<ContactModel>> GetContactsAsync(string? search = null, int? groupId = null)
    {
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search)}");
        }

        if (groupId is > 0)
        {
            query.Add($"groupId={groupId}");
        }

        var url = "api/contacts" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);

        return await _http.GetFromJsonAsync<List<ContactModel>>(url) ?? new List<ContactModel>();
    }

    /// <summary>Выборка одного контакта.</summary>
    public Task<ContactModel?> GetContactAsync(int id) =>
        _http.GetFromJsonAsync<ContactModel>($"api/contacts/{id}");

    public Task<ApiResult<ContactModel>> CreateContactAsync(ContactInput input) =>
        SendAsync<ContactModel>(HttpMethod.Post, "api/contacts", input);

    public Task<ApiResult> UpdateContactAsync(int id, ContactInput input) =>
        SendAsync(HttpMethod.Put, $"api/contacts/{id}", input);

    public Task<ApiResult> DeleteContactAsync(int id) =>
        SendAsync(HttpMethod.Delete, $"api/contacts/{id}", null);

    // ---------------------------------------------------------------------
    // Служебные методы
    // ---------------------------------------------------------------------

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body)
    {
        using var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await _http.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return ApiResult<T>.Ok(default!);
            }

            var value = await response.Content.ReadFromJsonAsync<T>();
            return ApiResult<T>.Ok(value!);
        }

        return ApiResult<T>.Fail(await ReadErrorsAsync(response));
    }

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body)
    {
        using var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await _http.SendAsync(request);

        return response.IsSuccessStatusCode
            ? ApiResult.Ok()
            : ApiResult.Fail(await ReadErrorsAsync(response));
    }

    /// <summary>Разбирает ответ об ошибке (ProblemDetails) в словарь сообщений.</summary>
    private static async Task<Dictionary<string, string[]>> ReadErrorsAsync(HttpResponseMessage response)
    {
        var errors = new Dictionary<string, string[]>();

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            if (problem?.Errors is { Count: > 0 })
            {
                foreach (var pair in problem.Errors)
                {
                    errors[pair.Key] = pair.Value;
                }
            }
            else if (!string.IsNullOrWhiteSpace(problem?.Title))
            {
                errors[string.Empty] = new[] { problem.Title };
            }
        }
        catch
        {
            // Ответ не является ProblemDetails — покажем общее сообщение.
        }

        if (errors.Count == 0)
        {
            errors[string.Empty] = new[] { $"Ошибка выполнения запроса к API (код {(int)response.StatusCode})." };
        }

        return errors;
    }
}
