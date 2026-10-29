using Hortalicas.Api.Data;
using Hortalicas.Api.Dtos;
using Hortalicas.Api.Models;
using Hortalicas.Api.Validation;
using Microsoft.EntityFrameworkCore;

namespace Hortalicas.Api.Services;

public enum ResultadoUsuario
{
    Ok,
    NaoEncontrado,
    CpfDuplicado,
    EmailDuplicado
}

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioResponse>> ListarAsync(CancellationToken ct = default);
    Task<UsuarioResponse?> ObterAsync(Guid id, CancellationToken ct = default);
    Task<(ResultadoUsuario Resultado, UsuarioResponse? Usuario)> CriarAsync(UsuarioRequest request, CancellationToken ct = default);
    Task<(ResultadoUsuario Resultado, UsuarioResponse? Usuario)> AtualizarAsync(Guid id, UsuarioRequest request, CancellationToken ct = default);
    Task<ResultadoUsuario> ExcluirAsync(Guid id, CancellationToken ct = default);
}

public class UsuarioService(AppDbContext db) : IUsuarioService
{
    public async Task<IReadOnlyList<UsuarioResponse>> ListarAsync(CancellationToken ct = default) =>
        await db.Usuarios.AsNoTracking()
            .OrderBy(u => u.Nome)
            .Select(u => ParaResponse(u))
            .ToListAsync(ct);

    public async Task<UsuarioResponse?> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        return usuario is null ? null : ParaResponse(usuario);
    }

    public async Task<(ResultadoUsuario, UsuarioResponse?)> CriarAsync(UsuarioRequest request, CancellationToken ct = default)
    {
        var usuario = new Usuario { Id = Guid.NewGuid() };
        Aplicar(usuario, request);

        var conflito = await VerificarDuplicidadeAsync(usuario, ct);
        if (conflito != ResultadoUsuario.Ok)
            return (conflito, null);

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);
        return (ResultadoUsuario.Ok, ParaResponse(usuario));
    }

    public async Task<(ResultadoUsuario, UsuarioResponse?)> AtualizarAsync(Guid id, UsuarioRequest request, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario is null)
            return (ResultadoUsuario.NaoEncontrado, null);

        Aplicar(usuario, request);

        var conflito = await VerificarDuplicidadeAsync(usuario, ct);
        if (conflito != ResultadoUsuario.Ok)
            return (conflito, null);

        await db.SaveChangesAsync(ct);
        return (ResultadoUsuario.Ok, ParaResponse(usuario));
    }

    public async Task<ResultadoUsuario> ExcluirAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario is null)
            return ResultadoUsuario.NaoEncontrado;

        db.Usuarios.Remove(usuario);
        await db.SaveChangesAsync(ct);
        return ResultadoUsuario.Ok;
    }

    private async Task<ResultadoUsuario> VerificarDuplicidadeAsync(Usuario usuario, CancellationToken ct)
    {
        if (await db.Usuarios.AnyAsync(u => u.Id != usuario.Id && u.Cpf == usuario.Cpf, ct))
            return ResultadoUsuario.CpfDuplicado;

        if (await db.Usuarios.AnyAsync(u => u.Id != usuario.Id && u.Email == usuario.Email, ct))
            return ResultadoUsuario.EmailDuplicado;

        return ResultadoUsuario.Ok;
    }

    private static void Aplicar(Usuario usuario, UsuarioRequest request)
    {
        usuario.Nome = request.Nome!.Trim();
        usuario.Cpf = Documentos.SomenteDigitos(request.Cpf!);
        usuario.Email = request.Email!.Trim().ToLowerInvariant();
        usuario.Telefone = Documentos.SomenteDigitos(request.Telefone!);
    }

    private static UsuarioResponse ParaResponse(Usuario u) => new(u.Id, u.Nome, u.Cpf, u.Email, u.Telefone);
}
