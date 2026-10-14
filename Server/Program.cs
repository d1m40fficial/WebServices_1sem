using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Server.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Интерактивная документация OpenAPI/Swagger.
builder.Services.AddOpenApi();

// Регистрируем контекст EF Core поверх SQLite.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=App_Data/contacts.db";

connectionString = MakeAbsolute(connectionString, builder.Environment.ContentRootPath);

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

// Создаём базу данных, схему и начальные данные при первом запуске.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

// Преобразует относительный путь к файлу БД в абсолютный и создаёт каталог.
static string MakeAbsolute(string connectionString, string contentRoot)
{
    var builder = new SqliteConnectionStringBuilder(connectionString);

    if (!string.IsNullOrWhiteSpace(builder.DataSource) && !Path.IsPathRooted(builder.DataSource))
    {
        builder.DataSource = Path.Combine(contentRoot, builder.DataSource);
    }

    var directory = Path.GetDirectoryName(builder.DataSource);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    return builder.ConnectionString;
}
