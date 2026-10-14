using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Server.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Интерактивная документация API: Swagger UI по адресу /swagger.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Справочник контактов — Web API",
        Version = "v1",
        Description = "REST API для работы со справочником контактов и групп (Практическая работа №2)."
    });

    // Подключаем XML-комментарии из исходного кода, чтобы описания были видны в Swagger.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

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

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Справочник контактов API v1");
    options.RoutePrefix = "swagger";
});

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
