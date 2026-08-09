using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Tests.Support;

public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        Options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public DbContextOptions<ApartmentsDbContext> Options { get; }

    public ApartmentsDbContext CreateContext() => new(Options);

    public void Dispose() => _connection.Dispose();
}
