using MyFirstWebApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Подключаем SQLite как хранилище данных.
builder.Services.AddSingleton<AppDatabase>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Создаём файл базы данных, таблицы и наполняем их начальными данными.
using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<AppDatabase>();
    database.Initialize();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
