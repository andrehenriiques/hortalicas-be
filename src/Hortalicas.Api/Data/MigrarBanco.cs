using Microsoft.EntityFrameworkCore;

namespace Hortalicas.Api.Data;

/// <summary>
/// Cria ou atualiza o banco quando a API inicia. Fica num serviço de inicialização (e não logo após o Build)
/// para ferramentas que só montam a aplicação, como a geração do OpenAPI, não tocarem no banco.
/// </summary>
public sealed class MigrarBanco(IServiceProvider services) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
