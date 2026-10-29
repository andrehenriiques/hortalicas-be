using Hortalicas.Api.Data;
using Hortalicas.Api.Dtos;
using Hortalicas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hortalicas.Api.Services;

public interface IEstufaService
{
    Task<IReadOnlyList<EstufaResponse>> ListarAsync(CancellationToken ct = default);
    Task<EstufaResponse?> ObterAsync(Guid id, CancellationToken ct = default);
    Task<Resultado<EstufaResponse>> CriarAsync(EstufaRequest request, CancellationToken ct = default);
    Task<Resultado<EstufaResponse>> AtualizarAsync(Guid id, EstufaRequest request, CancellationToken ct = default);
    Task<Falha?> ExcluirAsync(Guid id, CancellationToken ct = default);
}

public class EstufaService(AppDbContext db) : IEstufaService
{
    public async Task<IReadOnlyList<EstufaResponse>> ListarAsync(CancellationToken ct = default)
    {
        var estufas = await db.Estufas.AsNoTracking().Include(e => e.Canteiros).ToListAsync(ct);
        return estufas.OrderBy(e => e.Nome, StringComparer.CurrentCultureIgnoreCase).Select(ParaResponse).ToList();
    }

    public async Task<EstufaResponse?> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var estufa = await db.Estufas.AsNoTracking().Include(e => e.Canteiros).FirstOrDefaultAsync(e => e.Id == id, ct);
        return estufa is null ? null : ParaResponse(estufa);
    }

    public async Task<Resultado<EstufaResponse>> CriarAsync(EstufaRequest request, CancellationToken ct = default)
    {
        var estufa = new Estufa { Id = Guid.NewGuid() };
        Aplicar(estufa, request);

        if (await NomeEmUsoAsync(estufa, ct))
            return NomeDuplicado(estufa.Nome);

        for (var numero = 1; numero <= request.QuantidadeCanteiros!.Value; numero++)
            estufa.Canteiros.Add(new Canteiro { Id = Guid.NewGuid(), Numero = numero });

        db.Estufas.Add(estufa);
        await db.SaveChangesAsync(ct);
        return ParaResponse(estufa);
    }

    public async Task<Resultado<EstufaResponse>> AtualizarAsync(Guid id, EstufaRequest request, CancellationToken ct = default)
    {
        var estufa = await db.Estufas.Include(e => e.Canteiros).FirstOrDefaultAsync(e => e.Id == id, ct);
        if (estufa is null)
            return NaoEncontrada();

        Aplicar(estufa, request);
        if (await NomeEmUsoAsync(estufa, ct))
            return NomeDuplicado(estufa.Nome);

        var quantidade = request.QuantidadeCanteiros!.Value;
        var atual = estufa.Canteiros.Count;
        if (quantidade > atual)
        {
            // Add explícito: com o Id já preenchido, o EF trataria o canteiro novo como existente.
            for (var numero = atual + 1; numero <= quantidade; numero++)
                db.Canteiros.Add(new Canteiro { Id = Guid.NewGuid(), EstufaId = estufa.Id, Numero = numero });
        }
        else if (quantidade < atual)
        {
            var retirar = estufa.Canteiros.Where(c => c.Numero > quantidade).ToList();
            var ids = retirar.Select(c => c.Id).ToList();
            if (await db.Plantios.AnyAsync(p => ids.Contains(p.CanteiroId), ct))
                return Falha.Conflito(
                    "Não dá para reduzir os canteiros.",
                    $"Os canteiros {quantidade + 1} a {atual} já têm plantios registrados.");
            db.Canteiros.RemoveRange(retirar);
        }

        await db.SaveChangesAsync(ct);
        return ParaResponse(estufa);
    }

    public async Task<Falha?> ExcluirAsync(Guid id, CancellationToken ct = default)
    {
        var estufa = await db.Estufas.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (estufa is null)
            return NaoEncontrada();

        var temHistorico = await db.Plantios.AnyAsync(p => p.Canteiro.EstufaId == id, ct)
                           || await db.Irrigacoes.AnyAsync(i => i.EstufaId == id, ct);
        if (temHistorico)
            return Falha.Conflito(
                "A estufa tem histórico.",
                "Não dá para excluir uma estufa com plantios ou irrigações registrados: eles fazem parte do caderno de campo.");

        db.Estufas.Remove(estufa);
        await db.SaveChangesAsync(ct);
        return null;
    }

    private Task<bool> NomeEmUsoAsync(Estufa estufa, CancellationToken ct)
    {
        var nome = estufa.Nome.ToLower();
        return db.Estufas.AnyAsync(e => e.Id != estufa.Id && e.Nome.ToLower() == nome, ct);
    }

    private static void Aplicar(Estufa estufa, EstufaRequest request)
    {
        estufa.Nome = request.Nome!.Trim();
        estufa.ComprimentoM = request.ComprimentoM!.Value;
        estufa.LarguraM = request.LarguraM!.Value;
        estufa.DiasMaxSemIrrigacao = request.DiasMaxSemIrrigacao!.Value;
        estufa.Observacao = string.IsNullOrWhiteSpace(request.Observacao) ? null : request.Observacao.Trim();
    }

    private static Falha NaoEncontrada() => Falha.NaoEncontrado("Estufa não encontrada.");

    private static Falha NomeDuplicado(string nome) =>
        Falha.Conflito("Nome já cadastrado.", $"Já existe uma estufa chamada \"{nome}\".");

    public static EstufaResponse ParaResponse(Estufa e) => new(
        e.Id, e.Nome, e.ComprimentoM, e.LarguraM, e.AreaM2, e.Canteiros.Count, e.DiasMaxSemIrrigacao, e.Observacao);
}
