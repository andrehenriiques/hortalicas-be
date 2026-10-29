using Hortalicas.Api.Data;
using Hortalicas.Api.Dtos;
using Hortalicas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hortalicas.Api.Services;

public interface IHortalicaService
{
    Task<IReadOnlyList<HortalicaResponse>> ListarAsync(CancellationToken ct = default);
    Task<HortalicaResponse?> ObterAsync(Guid id, CancellationToken ct = default);
    Task<Resultado<HortalicaResponse>> CriarAsync(HortalicaRequest request, CancellationToken ct = default);
    Task<Resultado<HortalicaResponse>> AtualizarAsync(Guid id, HortalicaRequest request, CancellationToken ct = default);
    Task<Falha?> ExcluirAsync(Guid id, CancellationToken ct = default);
}

public class HortalicaService(AppDbContext db) : IHortalicaService
{
    public async Task<IReadOnlyList<HortalicaResponse>> ListarAsync(CancellationToken ct = default)
    {
        var hortalicas = await db.Hortalicas.AsNoTracking().ToListAsync(ct);
        return hortalicas.OrderBy(h => h.Nome, StringComparer.CurrentCultureIgnoreCase).Select(ParaResponse).ToList();
    }

    public async Task<HortalicaResponse?> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var hortalica = await db.Hortalicas.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id, ct);
        return hortalica is null ? null : ParaResponse(hortalica);
    }

    public async Task<Resultado<HortalicaResponse>> CriarAsync(HortalicaRequest request, CancellationToken ct = default)
    {
        var hortalica = new Hortalica { Id = Guid.NewGuid() };
        Aplicar(hortalica, request);
        if (await NomeEmUsoAsync(hortalica, ct))
            return NomeDuplicado(hortalica.Nome);

        db.Hortalicas.Add(hortalica);
        await db.SaveChangesAsync(ct);
        return ParaResponse(hortalica);
    }

    public async Task<Resultado<HortalicaResponse>> AtualizarAsync(Guid id, HortalicaRequest request, CancellationToken ct = default)
    {
        var hortalica = await db.Hortalicas.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (hortalica is null)
            return NaoEncontrada();

        Aplicar(hortalica, request);
        if (await NomeEmUsoAsync(hortalica, ct))
            return NomeDuplicado(hortalica.Nome);

        await db.SaveChangesAsync(ct);
        return ParaResponse(hortalica);
    }

    public async Task<Falha?> ExcluirAsync(Guid id, CancellationToken ct = default)
    {
        var hortalica = await db.Hortalicas.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (hortalica is null)
            return NaoEncontrada();

        if (await db.Plantios.AnyAsync(p => p.HortalicaId == id, ct))
            return Falha.Conflito(
                "A hortaliça tem plantios.",
                "Não dá para excluir uma hortaliça que já foi plantada: os plantios fazem parte do caderno de campo.");

        db.Hortalicas.Remove(hortalica);
        await db.SaveChangesAsync(ct);
        return null;
    }

    private Task<bool> NomeEmUsoAsync(Hortalica hortalica, CancellationToken ct)
    {
        var nome = hortalica.Nome.ToLower();
        return db.Hortalicas.AnyAsync(h => h.Id != hortalica.Id && h.Nome.ToLower() == nome, ct);
    }

    private static void Aplicar(Hortalica hortalica, HortalicaRequest request)
    {
        hortalica.Nome = request.Nome!.Trim();
        hortalica.CicloDias = request.CicloDias!.Value;
        hortalica.Observacao = string.IsNullOrWhiteSpace(request.Observacao) ? null : request.Observacao.Trim();
    }

    private static Falha NaoEncontrada() => Falha.NaoEncontrado("Hortaliça não encontrada.");

    private static Falha NomeDuplicado(string nome) =>
        Falha.Conflito("Nome já cadastrado.", $"Já existe uma hortaliça chamada \"{nome}\".");

    private static HortalicaResponse ParaResponse(Hortalica h) => new(h.Id, h.Nome, h.CicloDias, h.Observacao);
}
