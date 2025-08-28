// Program.cs — SQLite persistence + Azure storage service
using ABCRetail;
using ABCRetail.Service;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Prefer SQLite (persistent) if a connection string exists, else InMemory (volatile)
var sqliteConn = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(sqliteConn))
{
    builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(sqliteConn));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase("ABCRetailDb"));
}

// Azure/local storage service (you already have Azure creds in appsettings)
builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();

var app = builder.Build();

// Create DB/tables on first run (works for SQLite or InMemory)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


// ======================================================================
// Minimal EF Core DbContext lives below to keep your project simple
// ======================================================================
namespace ABCRetail
{
    using ABCRetail.Models;

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Key config: use common names or create a shadow key "Id"
            ConfigureKey<Product>(modelBuilder, "ProductId", "ProductID", "Id");
            ConfigureKey<Customer>(modelBuilder, "CustomerId", "CustomerID", "Id");
            ConfigureKey<Order>(modelBuilder, "OrderId", "OrderID", "Id");

            // IMPORTANT: ignore Azure Table Storage artifacts if present on your models
            IgnoreAzureTableArtifacts<Product>(modelBuilder);
            IgnoreAzureTableArtifacts<Customer>(modelBuilder);
            IgnoreAzureTableArtifacts<Order>(modelBuilder);
        }

        private static void ConfigureKey<TEntity>(ModelBuilder mb, params string[] commonNames)
            where TEntity : class
        {
            var t = typeof(TEntity);
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;

            var named = commonNames.FirstOrDefault(n => t.GetProperty(n, flags) != null);
            if (named != null) { mb.Entity<TEntity>().HasKey(named); return; }

            var anyId = t.GetProperties(flags).FirstOrDefault(p =>
                p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("ID", StringComparison.OrdinalIgnoreCase));
            if (anyId != null) { mb.Entity<TEntity>().HasKey(anyId.Name); return; }

            // Fallback: shadow key so EF can persist/track
            mb.Entity<TEntity>().Property<int>("Id").ValueGeneratedOnAdd();
            mb.Entity<TEntity>().HasKey("Id");
        }

        private static void IgnoreAzureTableArtifacts<TEntity>(ModelBuilder mb) where TEntity : class
        {
            var entity = mb.Entity<TEntity>();

            void IgnoreIfExists(string propName)
            {
                var p = typeof(TEntity).GetProperty(propName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p != null) entity.Ignore(propName);
            }

            // Common Azure.Data.Tables members that EF can’t map
            IgnoreIfExists("ETag");         // Azure.ETag
            IgnoreIfExists("Timestamp");    // DateTimeOffset?
            IgnoreIfExists("PartitionKey"); // string
            IgnoreIfExists("RowKey");       // string
        }
    }
}
