using Hortalicas.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hortalicas.Tests;

/// <summary>
/// Sobe a API com um banco SQLite em memória exclusivo por instância e o relógio parado em
/// <see cref="Hoje"/> (meio-dia em Brasília), para as regras de prazo serem previsíveis.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly DateOnly Hoje = new(2026, 10, 28);

    private readonly SqliteConnection _conexao = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _conexao.Open();
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_conexao));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new RelogioParado(new DateTimeOffset(2026, 10, 28, 15, 0, 0, TimeSpan.Zero)));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _conexao.Dispose();
    }
}

internal sealed class RelogioParado(DateTimeOffset agora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => agora;
}

internal static class ServiceCollectionExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var descriptor in services.Where(d => d.ServiceType == typeof(T)).ToList())
            services.Remove(descriptor);
    }
}
