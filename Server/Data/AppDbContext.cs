using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data;

/// <summary>
/// Контекст Entity Framework Core для базы данных SQLite.
/// Описывает схему, связи между сущностями и начальные данные.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ContactGroup> ContactGroups => Set<ContactGroup>();

    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContactGroup>(entity =>
        {
            entity.Property(g => g.Name)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.HasIndex(g => g.Name)
                  .IsUnique();

            entity.Property(g => g.Description)
                  .HasMaxLength(200);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.Property(c => c.FullName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(c => c.Phone).HasMaxLength(25);
            entity.Property(c => c.Email).HasMaxLength(100);
            entity.Property(c => c.Position).HasMaxLength(100);

            // Связь «один-ко-многим»: у группы много контактов.
            // При удалении группы контакты не удаляются, а остаются без группы.
            entity.HasOne(c => c.Group)
                  .WithMany(g => g.Contacts)
                  .HasForeignKey(c => c.GroupId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(c => c.FullName);
        });

        Seed(modelBuilder);
    }

    private static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContactGroup>().HasData(
            new ContactGroup { Id = 1, Name = "Администрация", Description = "Руководство компании и административный персонал" },
            new ContactGroup { Id = 2, Name = "Отдел продаж", Description = "Менеджеры по продажам и работа с клиентами" },
            new ContactGroup { Id = 3, Name = "Бухгалтерия", Description = "Бухгалтеры и экономисты" },
            new ContactGroup { Id = 4, Name = "Разработка", Description = "Инженеры-программисты и тестировщики" },
            new ContactGroup { Id = 5, Name = "Техническая поддержка", Description = "Специалисты службы поддержки" });

        modelBuilder.Entity<Contact>().HasData(
            new Contact { Id = 1, FullName = "Иванов Иван Иванович", Phone = "+7 (900) 111-22-33", Email = "ivanov@example.com", Position = "Генеральный директор", GroupId = 1 },
            new Contact { Id = 2, FullName = "Петрова Мария Сергеевна", Phone = "+7 (900) 222-33-44", Email = "petrova@example.com", Position = "Главный бухгалтер", GroupId = 3 },
            new Contact { Id = 3, FullName = "Сидоров Алексей Петрович", Phone = "+7 (900) 333-44-55", Email = "sidorov@example.com", Position = "Менеджер по продажам", GroupId = 2 },
            new Contact { Id = 4, FullName = "Кузнецова Ольга Владимировна", Phone = "+7 (900) 444-55-66", Email = "kuznetsova@example.com", Position = "Бухгалтер", GroupId = 3 },
            new Contact { Id = 5, FullName = "Волков Дмитрий Сергеевич", Phone = "+7 (900) 555-66-77", Email = "volkov@example.com", Position = "Инженер-программист", GroupId = 4 },
            new Contact { Id = 6, FullName = "Морозов Артём Андреевич", Phone = "+7 (900) 666-77-88", Email = "morozov@example.com", Position = "QA-инженер", GroupId = 4 },
            new Contact { Id = 7, FullName = "Соколова Екатерина Игоревна", Phone = "+7 (900) 777-88-99", Email = "sokolova@example.com", Position = "Специалист поддержки", GroupId = 5 },
            new Contact { Id = 8, FullName = "Лебедев Николай Павлович", Phone = "+7 (900) 888-99-00", Email = "lebedev@example.com", Position = "Специалист поддержки", GroupId = 5 },
            new Contact { Id = 9, FullName = "Новикова Анна Дмитриевна", Phone = "+7 (900) 999-00-11", Email = "novikova@example.com", Position = "Менеджер по продажам", GroupId = 2 },
            new Contact { Id = 10, FullName = "Зайцев Роман Викторович", Phone = "+7 (900) 000-11-22", Email = "zaytsev@example.com", Position = "Инженер-программист", GroupId = 4 });
    }
}
