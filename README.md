# Практическая работа №2 — Клиент-серверное приложение «Справочник контактов»

Решение на ASP.NET Core Web API, демонстрирующее разработку веб-сервиса на основе
архитектуры **REST** и клиентского приложения, которое этот сервис использует.

Решение состоит из **двух проектов**:

- **`Server`** — серверная часть (ASP.NET Core Web API): REST-сервис, хранение данных
  в SQLite через Entity Framework Core, интерактивная документация Swagger/OpenAPI;
- **`Client`** — клиентская часть (ASP.NET Core MVC): веб-приложение, которое
  выполняет все операции исключительно через HTTP-запросы к серверному API.

> Работа выполняется в ветке **`ПР2`** того же репозитория.

## Содержание

- [Анализ первой работы](#анализ-первой-работы)
- [Выполнение требований](#выполнение-требований)
- [Архитектура решения](#архитектура-решения)
- [Стек технологий](#стек-технологий)
- [Структура решения](#структура-решения)
- [Модель данных](#модель-данных)
- [REST API](#rest-api)
- [Интерактивная документация Swagger](#интерактивная-документация-swagger)
- [Клиентская часть](#клиентская-часть)
- [Запуск](#запуск)
- [История коммитов](#история-коммитов)
- [Проверка работоспособности](#проверка-работоспособности)
- [Выводы](#выводы)

## Анализ первой работы

В первой практической работе (ветка `main`) было создано **монолитное** приложение
ASP.NET Core MVC: контроллеры, представления и доступ к базе данных SQLite (через
Dapper) находились в одном проекте. Данная работа развивает ту же предметную область
в **клиент-серверную** архитектуру.

| Аспект | ПР1 (монолит MVC) | ПР2 (клиент-сервер) |
|--------|-------------------|---------------------|
| Количество проектов | 1 | 2 (Server, Client) |
| Доступ к БД | Dapper, SQL внутри контроллеров | EF Core, `AppDbContext` |
| Интерфейс | Razor-представления в том же процессе | Отдельный клиентский проект |
| Обмен данными | прямой вызов методов репозитория | HTTP + JSON через REST API |
| Контракт | неявный (общие модели) | явный (DTO + OpenAPI-спецификация) |
| Документация | нет | Swagger UI (`/swagger`) |
| Связь сущностей | `LEFT JOIN` в SQL | навигационные свойства EF Core |
| Масштабирование | единый процесс | сервер и клиент отдельно |

Ключевые выводы из первой работы, которые учтены здесь:

1. Модель «контакт — группа» со связью «один-ко-многим» хорошо подходит для
   демонстрации CRUD и переносится в API без изменений.
2. В ПР1 вся логика и доступ к данным были слиты в контроллерах — здесь они
   разделены: сервер отвечает за данные и правила, клиент — только за интерфейс.
3. В ПР1 серверная валидация возвращала страницу с ошибками; в API валидация
   реализована через `[ApiController]` и `ProblemDetails`, а клиент показывает
   полученные ошибки в формах.

## Выполнение требований

| Требование | Реализация |
|------------|------------|
| 2 проекта: клиент и сервер | `Server` (Web API), `Client` (MVC) в решении `Praktika2.slnx` |
| ≥2 связанных сущности | `ContactGroup` 1 → ∞ `Contact` (внешний ключ `GroupId`) |
| ≥2 контроллера API | `GroupsController`, `ContactsController` |
| Выборка всех | `GET /api/groups`, `GET /api/contacts` |
| Выборка по условию (фильтрация) | query-параметры `search`, `groupId` |
| Удаление | `DELETE /api/{resource}/{id}` |
| Добавление | `POST /api/{resource}` |
| Редактирование | `PUT /api/{resource}/{id}` |
| Интерактивная документация | Swagger UI + OpenAPI 3.0 (`/swagger`) |
| Реляционная СУБД | SQLite (файл `Server/App_Data/contacts.db`) |
| Клиент — реализация API | MVC-клиент вызывает все методы API через `HttpClient` |
| Отдельные коммиты по этапам | 7 коммитов (см. [историю](#история-коммитов)) |

## Архитектура решения

```
                 HTTP + JSON (REST)
┌────────────────┐  GET/POST/PUT/DELETE  ┌──────────────────┐
│  Client (MVC)  │ ────────────────────► │  Server (Web API)│
│                │ ◄──────────────────── │                  │
│ • Контроллеры  │      JSON-ответы      │ • Контроллеры    │
│ • Razor-вью    │                       │ • EF Core        │
│ • ContactsApi  │                       │ • SQLite         │
│   Client       │                       │ • Swagger UI     │
└────────────────┘                       └────────┬─────────┘
     :5211                               :5210    │
                                                  ▼
                                          ┌──────────────┐
                                          │  contacts.db │
                                          └──────────────┘
     
```

Клиент **не имеет** доступа к базе данных: он видит только REST API. Любая операция
(список, фильтр, добавление, изменение, удаление) превращается в HTTP-запрос к серверу.

## Стек технологий

| Компонент | Версия / описание |
|-----------|-------------------|
| Framework | .NET 10 (`net10.0`) |
| Сервер | ASP.NET Core Web API (`[ApiController]`, REST) |
| Клиент | ASP.NET Core MVC (`HttpClient`) |
| ORM | Entity Framework Core 10 (`Microsoft.EntityFrameworkCore.Sqlite`) |
| СУБД | SQLite (файл `Server/App_Data/contacts.db`) |
| Документация | Swashbuckle.AspNetCore 10 (Swagger UI + OpenAPI) |
| Сериализация | System.Text.Json |
| Frontend | Bootstrap 5, Razor |

## Структура решения

```
Praktika2.slnx
├── Server/                                  серверная часть (Web API)
│   ├── Controllers/
│   │   ├── GroupsController.cs              REST CRUD групп + фильтрация
│   │   └── ContactsController.cs            REST CRUD контактов + фильтрация
│   ├── Data/
│   │   └── AppDbContext.cs                  контекст EF Core, схема, сидинг
│   ├── Dtos/Dtos.cs                         контракты API (запросы и ответы)
│   ├── Models/                              сущности Contact, ContactGroup
│   ├── Program.cs                           регистрация сервисов, Swagger, БД
│   └── appsettings.json                     строка подключения
└── Client/                                  клиентская часть (MVC)
    ├── Controllers/                         Contacts, Groups, Home
    ├── Services/ContactsApiClient.cs        типизированный HttpClient
    ├── Models/                              модели и результат вызова API
    ├── Views/                               Razor-представления (CRUD)
    ├── Program.cs                           регистрация HttpClient
    └── appsettings.json                     ApiBaseUrl
```

## Модель данных

Связь **«один-ко-многим»**: у одной группы много контактов; у каждого контакта
не более одной группы.

```
ContactGroups (1) ──────────< Contacts (∞)
        Id                      Id
        Name (UNIQUE)           FullName
        Description             Phone
                                Email
                                Position
                                GroupId ──┐ FK, ON DELETE SET NULL
```

Схема создаётся EF Core (`Database.EnsureCreated()`) при первом запуске и
заполняется начальными данными: **5 групп** и **10 контактов**.

Ключевые настройки модели (`Server/Data/AppDbContext.cs`):

```csharp
entity.HasIndex(g => g.Name).IsUnique();          // название группы уникально

entity.HasOne(c => c.Group)                        // связь «один-ко-многим»
      .WithMany(g => g.Contacts)
      .HasForeignKey(c => c.GroupId)
      .OnDelete(DeleteBehavior.SetNull);           // контакты не удаляются с группой

entity.HasIndex(c => c.FullName);                  // индекс для поиска/сортировки
```

## REST API

Базовый адрес: `http://localhost:5210`. Формат — JSON.

### Группы — `GroupsController`

| Метод и URL | Назначение | Ответы |
|-------------|------------|--------|
| `GET /api/groups?search=текст` | выборка всех групп; при `search` — по условию (название/описание) | `200` |
| `GET /api/groups/{id}` | выборка одной группы | `200`, `404` |
| `GET /api/groups/{id}/contacts` | контакты группы (демонстрация связи) | `200`, `404` |
| `POST /api/groups` | добавление группы | `201`, `400` |
| `PUT /api/groups/{id}` | редактирование группы | `204`, `400`, `404` |
| `DELETE /api/groups/{id}` | удаление группы | `204`, `404` |

### Контакты — `ContactsController`

| Метод и URL | Назначение | Ответы |
|-------------|------------|--------|
| `GET /api/contacts?search=текст&groupId=5` | выборка всех; при параметрах — по условию | `200` |
| `GET /api/contacts/{id}` | выборка одного контакта | `200`, `404` |
| `POST /api/contacts` | добавление контакта | `201`, `400` |
| `PUT /api/contacts/{id}` | редактирование контакта | `204`, `400`, `404` |
| `DELETE /api/contacts/{id}` | удаление контакта | `204`, `404` |

### Особенности реализации

- **DTO вместо сущностей.** API не отдаёт EF-сущности напрямую: для ответов
  используются `ContactDto` / `ContactGroupDto`, для запросов — `ContactWriteDto` /
  `ContactGroupWriteDto`. Это исключает циклическую сериализацию и «лишние» поля.
- **Валидация.** Атрибуты `[Required]`, `[StringLength]`, `[EmailAddress]` на DTO
  плюс `[ApiController]` → автоматический ответ `400` с `ProblemDetails`.
- **Бизнес-правила.** Проверяется уникальность названия группы и существование
  указанной группы у контакта (иначе `400`).
- **Коды состояния.** Корректные REST-коды: `201 Created` с заголовком `Location`
  при создании, `204 No Content` при изменении/удалении, `404` для отсутствующих id.
- **Асинхронность.** Все действия — `async`/`await` поверх EF Core.

Пример фрагмента фильтрации (`Server/Controllers/ContactsController.cs`):

```csharp
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
```

## Интерактивная документация Swagger

Swagger UI доступен по адресу **<http://localhost:5210/swagger>**.
OpenAPI-спецификация — `/swagger/v1/swagger.json` (OpenAPI 3.0).

Из документации можно выполнять запросы прямо из браузера (кнопка *Try it out*),
что позволяет проверить все методы API без сторонних инструментов.

В документацию попадают описания из XML-комментариев к контроллерам и DTO
(в проекте включена генерация XML-документации).

## Клиентская часть

Клиент — ASP.NET Core MVC приложение (порт `5211`). Оно **не работает с БД напрямую**:
вся логика построена на вызовах REST API.

- **`Services/ContactsApiClient.cs`** — типизированный `HttpClient`, зарегистрированный
  через `AddHttpClient<ContactsApiClient>()`. Инкапсулирует адреса и форматы всех
  запросов, возвращает `ApiResult` / `ApiResult<T>`.
- **Контроллеры `ContactsController` и `GroupsController`** — повторяют набор операций
  API: список, фильтрация, просмотр, добавление, изменение, удаление.
- **Представления Razor** — формы с клиентской и серверной валидацией; ошибки,
  возвращённые API (`ProblemDetails`), отображаются прямо в формах.

Пример вызова API из клиента:

```csharp
// выборка с фильтрацией
var contacts = await _api.GetContactsAsync(search, groupId);

// создание с обработкой ошибок валидации, пришедших от API
var result = await _api.CreateContactAsync(input);
if (!result.Success)
{
    AddApiErrors(result);   // ошибки API -> ModelState -> отображение в форме
    return View(input);
}
```

Адрес серверной части задаётся в `Client/appsettings.json`:

```json
{ "ApiBaseUrl": "http://localhost:5210/" }
```

## Запуск

Требуется [.NET SDK 10.0](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/d1m40fficial/WebServices_1sem.git
cd WebServices_1sem
git checkout ПР2

dotnet restore
```

Затем запустить **оба** проекта (в двух окнах терминала):

```bash
# окно 1 — серверная часть (Web API + Swagger)
dotnet run --project Server        # http://localhost:5210, Swagger: /swagger

# окно 2 — клиентская часть
dotnet run --project Client        # http://localhost:5211
```

- Клиент: <http://localhost:5211>
- Swagger UI: <http://localhost:5210/swagger>
- База данных `Server/App_Data/contacts.db` создаётся автоматически при первом
  запуске сервера и не попадает в репозиторий (исключена в `.gitignore`).

## История коммитов

Каждый этап работы выделен в отдельный commit (ветка `ПР2`):

```bash
git log --oneline
```

| Commit | Сообщение | Содержание |
|--------|-----------|------------|
| `2233adc` | `PR2: create solution with server (Web API) and client (MVC) projects` | Скелет решения из двух проектов |
| `e9ed4a3` | `PR2: data model (Contact, ContactGroup) with EF Core + SQLite, schema, relationship and seed data` | Модель данных, `AppDbContext`, связь, сидинг |
| `19788f5` | `PR2: REST controllers Groups and Contacts with select all, filtering, create, update, delete` | Два REST-контроллера со всеми операциями |
| `de434f3` | `PR2: interactive API documentation (Swagger UI + OpenAPI) with XML comments` | Swagger UI и OpenAPI-спецификация |
| `6d4ca99` | `PR2: client API layer - typed HttpClient, models and result types` | HTTP-клиент, модели, `ApiResult` |
| `323cd4f` | `PR2: client MVC controllers and Razor views implementing CRUD through the API` | Контроллеры и представления клиента |
| `…` | `PR2: add README report for practical work #2` | Настоящий отчёт (последний коммит ветки) |

## Проверка работоспособности

Приложение было запущено (сервер + клиент), все сценарии проверены HTTP-запросами.

### Серверное API (15 проверок)

| № | Проверка | Результат |
|---|----------|-----------|
| 1 | `GET /api/groups` — выборка всех (5) | ✅ |
| 2 | Группа содержит `ContactsCount` | ✅ |
| 3 | `GET /api/groups?search=Бух` — фильтрация (1) | ✅ |
| 4 | `GET /api/contacts` — выборка всех (10) | ✅ |
| 5 | Контакт содержит `GroupName` (по связи) | ✅ |
| 6 | `GET /api/contacts?groupId=4` — фильтрация (3) | ✅ |
| 7 | `GET /api/contacts/1` — выборка одной | ✅ |
| 8 | `GET /api/contacts/9999` → `404` | ✅ |
| 9 | `GET /api/groups/3/contacts` — контакты группы (2) | ✅ |
| 10 | `POST /api/groups` → `201` | ✅ |
| 11 | `POST` дубликата группы → `400` | ✅ |
| 12 | `POST /api/contacts` → `201` | ✅ |
| 13 | `POST` с некорректным e-mail / пустым ФИО → `400` | ✅ |
| 14 | `PUT /api/groups/{id}` и `PUT /api/contacts/{id}` → `204` | ✅ |
| 15 | `DELETE` группы и контакта → `204`, `404` для отсутствующих | ✅ |

### Клиентское приложение (17 проверок)

| № | Проверка | Результат |
|---|----------|-----------|
| 1 | Swagger UI открывается (`200`) | ✅ |
| 2 | Список контактов через клиент (10) | ✅ |
| 3 | Фильтр по строке поиска (1) | ✅ |
| 4 | Фильтр по группе (3) | ✅ |
| 5 | Список групп (5) и фильтр групп (1) | ✅ |
| 6 | Карточка контакта и карточка группы с контактами | ✅ |
| 7 | Несуществующий id → `404` (а не `500`) | ✅ |
| 8 | Создание контакта через форму (→ API `POST`) | ✅ |
| 9 | Редактирование контакта через форму (→ API `PUT`) | ✅ |
| 10 | Удаление контакта через форму (→ API `DELETE`) | ✅ |
| 11 | Создание и удаление группы через формы | ✅ |
| 12 | Дубликат группы → ошибка валидации API видна в форме | ✅ |

## Выводы

В ходе работы были получены следующие навыки:

1. **Проектирование REST API** — выбор ресурсов, методов, форм URL и корректных
   кодов состояния (`200/201/204/400/404`), разделение контракта (DTO) и модели
   предметной области.
2. **Клиент-серверная архитектура** — разделение решения на два независимых
   проекта, обмен данными по HTTP/JSON, отсутствие прямого доступа клиента к БД.
3. **Entity Framework Core** — конфигурация сущностей через Fluent API, связь
   «один-ко-многим», поведение при удалении (`SetNull`), уникальный индекс,
   начальные данные, асинхронные запросы.
4. **Интерактивная документация** — подключение Swagger/OpenAPI и XML-комментариев,
   проверка API прямо из браузера.
5. **Валидация и обработка ошибок** — серверная валидация DTO, ответы
   `ProblemDetails`, проброс и отображение ошибок API в формах клиента.
6. **Работа с HTTP из .NET** — типизированный `HttpClient`, `System.Text.Json`,
   обработка кодов состояния и разбор `ProblemDetails` на клиенте.
7. **Дисциплина разработки** — поэтапная фиксация изменений в Git в отдельной ветке.

Практическая работа показывает переход от монолитного MVC-приложения (ПР1) к
клиент-серверному решению с чётким REST-контрактом, что является типовой
архитектурой современных веб-систем.
