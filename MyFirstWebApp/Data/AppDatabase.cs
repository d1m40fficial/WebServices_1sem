using System.Data;
using Microsoft.Data.Sqlite;

namespace MyFirstWebApp.Data;

/// <summary>
/// Предоставляет подключение к файлу базы данных SQLite и создаёт его структуру
/// при первом запуске приложения.
/// </summary>
public class AppDatabase
{
    private readonly string _connectionString;

    public AppDatabase(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "В appsettings.json не задана строка подключения DefaultConnection.");

        EnsureDataFolderExists(_connectionString);
    }

    /// <summary>
    /// SQLite не создаёт каталог для файла базы данных сам — делаем это на старте.
    /// </summary>
    private static void EnsureDataFolderExists(string connectionString)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;

        if (!string.IsNullOrWhiteSpace(dataSource))
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath(dataSource));

            if (!string.IsNullOrWhiteSpace(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
    }

    /// <summary>Создаёт и возвращает новое подключение к базе данных.</summary>
    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Создаёт таблицы (если их ещё нет) и заполняет базу начальными данными.
    /// </summary>
    public void Initialize()
    {
        using var connection = CreateConnection();

        Execute(connection, """
            CREATE TABLE IF NOT EXISTS ContactGroups
            (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Name        TEXT NOT NULL UNIQUE,
                Description TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS Contacts
            (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                FullName TEXT    NOT NULL,
                Phone    TEXT    NULL,
                Email    TEXT    NULL,
                Position TEXT    NULL,
                GroupId  INTEGER NULL REFERENCES ContactGroups (Id) ON DELETE SET NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Contacts_GroupId ON Contacts (GroupId);
            CREATE INDEX IF NOT EXISTS IX_Contacts_FullName ON Contacts (FullName);
            """);

        Seed(connection);
    }

    private static void Seed(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM ContactGroups;";
        var groupsCount = Convert.ToInt64(command.ExecuteScalar());

        if (groupsCount > 0)
        {
            return;
        }

        Execute(connection, """
            INSERT INTO ContactGroups (Name, Description) VALUES
                ('Администрация',   'Руководство компании и административный персонал'),
                ('Отдел продаж',    'Менеджеры по продажам и работа с клиентами'),
                ('Бухгалтерия',     'Бухгалтеры и экономисты'),
                ('Разработка',      'Инженеры-программисты и тестировщики'),
                ('Техническая поддержка', 'Специалисты службы поддержки');

            INSERT INTO Contacts (FullName, Phone, Email, Position, GroupId) VALUES
                ('Иванов Иван Иванович',      '+7 (900) 111-22-33', 'ivanov@example.com',    'Генеральный директор', 1),
                ('Петрова Мария Сергеевна',   '+7 (900) 222-33-44', 'petrova@example.com',   'Главный бухгалтер',    3),
                ('Сидоров Алексей Петрович',  '+7 (900) 333-44-55', 'sidorov@example.com',   'Менеджер по продажам', 2),
                ('Кузнецова Ольга Владимировна', '+7 (900) 444-55-66', 'kuznetsova@example.com', 'Бухгалтер',         3),
                ('Волков Дмитрий Сергеевич',  '+7 (900) 555-66-77', 'volkov@example.com',    'Инженер-программист', 4),
                ('Морозов Артём Андреевич',  '+7 (900) 666-77-88', 'morozov@example.com',   'QA-инженер',          4),
                ('Соколова Екатерина Игоревна','+7 (900) 777-88-99', 'sokolova@example.com',  'Специалист поддержки',5),
                ('Лебедев Николай Павлович',  '+7 (900) 888-99-00', 'lebedev@example.com',   'Специалист поддержки', 5),
                ('Новикова Анна Дмитриевна',  '+7 (900) 999-00-11', 'novikova@example.com',  'Менеджер по продажам', 2),
                ('Зайцев Роман Викторович',   '+7 (900) 000-11-22', 'zaytsev@example.com',   'Инженер-программист', 4);
            """);
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
