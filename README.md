# Практическая работа №1 — Справочник контактов с группами

ASP.NET MVC приложение, демонстрирующее навыки работы с базой данных и реализацию
CRUD-операций (Create, Read, Update, Delete) над реляционной базой данных SQLite.

## Содержание

- [Предметная область](#предметная-область)
- [Стек технологий](#стек-технологий)
- [Структура проекта](#структура-проекта)
- [Схема базы данных](#схема-базы-данных)
- [Реализованные CRUD-операции](#реализованные-crud-операции)
- [Фильтрация (выборка по условию)](#фильтрация-выборка-по-условию)
- [Запуск проекта](#запуск-проекта)
- [История коммитов](#история-коммитов)
- [Проверка работоспособности](#проверка-работоспособности)
- [Выводы](#выводы)

## Предметная область

**Список контактов с группами** — электронный справочник сотрудников организации.

Справочник состоит из двух сущностей:

| Сущность     | Содержит                                                       |
|--------------|----------------------------------------------------------------|
| **Контакт**  | ФИО, телефон, e-mail, должность и ссылку на группу               |
| **Группа**   | Название отдела/группы, её описание и количество контактов в ней  |

Связь между сущностями — **«многие к одному»**: в одной группе может быть много
контактов, при этом у каждого контакта не более одной группы.

## Стек технологий

| Компонент              | Версия / описание                                  |
|------------------------|-----------------------------------------------------|
| Framework              | .NET 10 (`net10.0`)                                 |
| Шаблон                 | ASP.NET Core MVC (`Microsoft.NET.Sdk.Web`)          |
| СУБД                   | **SQLite** (файл `MyFirstWebApp/App_Data/contacts.db`) |
| Провайдер ADO.NET      | `Microsoft.Data.Sqlite 10.0.0`                      |
| Микро-ORM              | `Dapper 2.1.66`                                     |
| Нативный движок SQLite | `SQLitePCLRaw.bundle_e_sqlite3 3.0.5`               |
| Frontend               | Bootstrap 5 (из шаблона Visual Studio), Razor       |

## Структура проекта

```
MyFirstWebApp/
├── Controllers/
│   ├── HomeController.cs        перенаправление на список контактов
│   ├── ContactsController.cs    ← CRUD контактов + фильтрация
│   └── GroupsController.cs      ← CRUD групп
├── Data/
│   └── AppDatabase.cs           подключение к SQLite, создание схемы и начальных данных
├── Models/
│   ├── Contact.cs               модель контакта с атрибутами валидации
│   ├── ContactGroup.cs          модель группы
│   └── ErrorViewModel.cs
├── Views/
│   ├── Contacts/                Index, Create, Edit, Delete, Details
│   ├── Groups/                  Index, Create, Edit, Delete, Details
│   └── Shared/                  _Layout, _ValidationScriptsPartial, _SuccessMessage
├── Program.cs                   регистрация зависимостей, инициализация БД
└── appsettings.json             строка подключения к базе данных
```

## Схема базы данных

```sql
CREATE TABLE ContactGroups
(
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL UNIQUE,
    Description TEXT NULL
);

CREATE TABLE Contacts
(
    Id       INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT    NOT NULL,
    Phone    TEXT    NULL,
    Email    TEXT    NULL,
    Position TEXT    NULL,
    GroupId  INTEGER NULL REFERENCES ContactGroups (Id) ON DELETE SET NULL
);

CREATE INDEX IX_Contacts_GroupId ON Contacts (GroupId);
CREATE INDEX IX_Contacts_FullName ON Contacts (FullName);
```

Особенности схемы:

- **Внешний ключ** `Contacts.GroupId` ссылается на `ContactGroups.Id`.
- `ON DELETE SET NULL` — при удалении группы её контакты **не удаляются**, а остаются
  в справочнике без группы (проверено в тестах).
- Уникальное ограничение `ContactGroups.Name` защищает от создания двух групп
  с одинаковым названием.
- Индексы по `GroupId` и `FullName` ускоряют выборку с фильтром и сортировку.

База данных и начальные данные (5 групп, 10 контактов) создаются автоматически
при первом запуске приложения — в методе `AppDatabase.Initialize()`.

## Реализованные CRUD-операции

### Контакты — `Controllers/ContactsController.cs`

| Операция | Действие контроллера | SQL-запрос |
|----------|----------------------|------------|
| **Read (все)**       | `Index`          | `SELECT ... FROM Contacts c LEFT JOIN ContactGroups g` |
| **Read (по id)**     | `Details`        | `SELECT ... WHERE c.Id = @id` |
| **Read (фильтр)**    | `Index(search, groupId)` | `SELECT ... WHERE ... AND ...` |
| **Create**           | `Create` (GET/POST) | `INSERT INTO Contacts (FullName, Phone, Email, Position, GroupId) VALUES (@...)` |
| **Update**           | `Edit` (GET/POST) | `UPDATE Contacts SET ... WHERE Id = @Id` |
| **Delete**           | `Delete` / `DeleteConfirmed` (POST) | `DELETE FROM Contacts WHERE Id = @id` |

### Группы — `Controllers/GroupsController.cs`

Аналогичный набор операций. Дополнительно:

- при выборке групп считается количество контактов в каждой группе
  (подзапрос в `SELECT`);
- перед добавлением и изменением выполняется проверка уникальности названия;
- на странице группы выводится список всех её контактов.

### Пример ключевых участков кода

Чтение всех элементов и сбор связанных данных через `JOIN`
(`MyFirstWebApp/Controllers/ContactsController.cs:23`):

```csharp
private const string SelectContactsSql = """
    SELECT  c.Id, c.FullName, c.Phone, c.Email, c.Position, c.GroupId,
            g.Name AS GroupName
    FROM    Contacts AS c
    LEFT JOIN ContactGroups AS g ON g.Id = c.GroupId
    """;
```

Добавление элемента с параметрами (без конкатенации строк — используется
параметризация через `DynamicParameters`, что защищает от SQL-инъекций):

```csharp
const string sql = """
    INSERT INTO Contacts (FullName, Phone, Email, Position, GroupId)
    VALUES (@FullName, @Phone, @Email, @Position, @GroupId);
    """;

using var connection = _database.CreateConnection();
await connection.ExecuteAsync(sql, contact);
```

## Фильтрация (выборка по условию)

Фильтрация реализована на странице списка контактов. Условия **комбинируются
логическим И** и подставляются в `WHERE` динамически — лишние условия не добавляются,
если фильтр не задан:

```csharp
var conditions = new List<string>();
var parameters = new DynamicParameters();

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

if (groupId is > 0)
{
    conditions.Add("c.GroupId = @groupId");
    parameters.Add("groupId", groupId.Value);
}

var where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : string.Empty;
```

Доступные фильтры:

| Где              | Параметр   | Условие |
|------------------|------------|---------|
| Контакты         | `search`   | `LIKE '%значение%'` по ФИО, телефону, e-mail, должности |
| Контакты         | `groupId`  | точное совпадение `GroupId` |
| Контакты         | оба вместе | `search AND groupId` |
| Группы           | `search`   | `LIKE '%значение%'` по названию и описанию |

Примеры запросов:

```
/Contacts                                  — все контакты
/Contacts?search=Иванов                     — поиск по ФИО
/Contacts?groupId=4                        — контакты группы «Разработка»
/Contacts?search=Инженер&groupId=4         — комбинированный фильтр
/Groups?search=отдел                       — группы по названию/описанию
```

## Запуск проекта

Требуется [.NET SDK 10.0](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/d1m40fficial/WebServices_1sem.git
cd WebServices_1sem
dotnet restore
dotnet run
```

Приложение будет доступно по адресу **http://localhost:5015** (или по адресу,
указанному в `Properties/launchSettings.json`).

Отдельные команды:

```bash
dotnet build       # сборка проекта
dotnet watch run   # запуск с автоматической перезагрузкой при изменениях
```

Установка дополнительных пакетов (уже выполнена в проекте):

```bash
dotnet add package Dapper
dotnet add package Microsoft.Data.Sqlite
```

Файл базы данных `App_Data/contacts.db` создаётся автоматически при первом запуске
и не попадает в репозиторий (исключён в `.gitignore`). Чтобы начать с чистого
состояния — достаточно удалить этот файл.

## История коммитов

Каждый этап работы выделен в отдельный commit:

| Commit | Сообщение                                                          | Содержание |
|--------|--------------------------------------------------------------------|------------|
| `2d373ac` | `Initial commit: ASP.NET MVC project template from Visual Studio`   | Шаблон проекта из Visual Studio, `.gitignore` |
| `66b8719` | `Add SQLite database setup: Dapper/Microsoft.Data.Sqlite packages, AppDatabase with schema creation and seed data` | Подключение SQLite и Dapper, класс `AppDatabase` со схемой и начальными данными, модели `Contact` и `ContactGroup` |
| `9d81c69` | `Add ContactsController and GroupsController with full CRUD against SQLite` | Контроллеры со всеми CRUD-операциями и фильтрацией |
| `20ff5e6` | `Add Razor views: contact/group lists, filters, CRUD forms and navigation` | Представления (списки, формы, подтверждение удаления), навигация |

## Проверка работоспособности

Приложение было запущено, и все операции проверены HTTP-запросами:

| № | Проверка                                                  | Результат |
|---|-----------------------------------------------------------|-----------|
| 1 | Выборка всех контактов (10 записей)                      | ✅ |
| 2 | Фильтр по поисковой строке «Иванов» (1 запись)           | ✅ |
| 3 | Фильтр по группе (3 записи)                              | ✅ |
| 4 | Комбинированный фильтр «Инженер» + группа (2 записи)     | ✅ |
| 5 | Выборка всех групп (5 записей)                           | ✅ |
| 6 | Фильтр групп по описанию                                 | ✅ |
| 7 | Просмотр карточки контакта (`Contacts/Details/1`)        | ✅ |
| 8 | Просмотр группы со списком её контактов                  | ✅ |
| 9 | Обращение к несуществующему идентификатору → 404         | ✅ |
| 10 | Фильтр, не давший результатов → информационное сообщение | ✅ |
| 11 | Добавление контакта (POST)                               | ✅ |
| 12 | Редактирование контакта (POST)                           | ✅ |
| 13 | Серверная валидация модели (пустое ФИО, некорректный e-mail) | ✅ |
| 14 | Защита от создания группы с дублирующимся названием      | ✅ |
| 15 | Добавление группы (POST)                                 | ✅ |
| 16 | Удаление группы (POST)                                   | ✅ |
| 17 | Контакты удалённой группы остаются в справочнике без группы (`ON DELETE SET NULL`) | ✅ |
| 18 | Удаление контакта (POST)                                 | ✅ |

## Выводы

В ходе работы были получены следующие навыки:

1. **Проектирование реляционной схемы** — создание таблиц, первичных и внешних
   ключей, ограничений уникальности и индексов; выбор стратегии удаления
   связанных записей (`ON DELETE SET NULL`).
2. **Работа с СУБД из приложения** — создание файла SQLite, подключение через
   `Microsoft.Data.Sqlite`, выполнение команд через микро-ORM Dapper.
3. **Основы SQL** — `SELECT` с `LEFT JOIN`, `INSERT`, `UPDATE`, `DELETE`,
   подзапросы, агрегирование, динамическая сборка условий `WHERE`.
4. **Параметризация запросов** — все значения передаются как параметры
   (`DynamicParameters`, анонимные объекты), что защищает приложение от
   SQL-инъекций.
5. **Организация веб-приложения на ASP.NET MVC** — контроллеры, маршрутизация,
   Razor-представления, внедрение зависимостей через `IServiceCollection`,
   `ViewData`/`TempData`, частичные представления и валидация моделей
   (`DataAnnotations`, клиентская и серверная).
6. **Контроль качества** — поэтапная фиксация изменений в системе контроля версий
   Git, что позволяет отследить логику решения задачи по этапам.

Приложение можно использовать как основу для следующих практических работ:
расширение справочника поиском с пагинацией, авторизацией, журналированием
изменений и валидацией на уровне БД.
