using Hortalicas.Api.Dtos;
using Hortalicas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hortalicas.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController(IUsuarioService service) : ControllerBase
{
    /// <summary>Lista os usuários, ordenados por nome.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UsuarioResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await service.ListarAsync(ct));

    /// <summary>Obtém um usuário pelo id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct)
    {
        var usuario = await service.ObterAsync(id, ct);
        return usuario is null ? NaoEncontrado() : Ok(usuario);
    }

    /// <summary>Cadastra um usuário.</summary>
    [HttpPost]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar(UsuarioRequest request, CancellationToken ct)
    {
        var (resultado, usuario) = await service.CriarAsync(request, ct);
        return resultado == ResultadoUsuario.Ok
            ? CreatedAtAction(nameof(Obter), new { id = usuario!.Id }, usuario)
            : Erro(resultado);
    }

    /// <summary>Edita um usuário.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(Guid id, UsuarioRequest request, CancellationToken ct)
    {
        var (resultado, usuario) = await service.AtualizarAsync(id, request, ct);
        return resultado == ResultadoUsuario.Ok ? Ok(usuario) : Erro(resultado);
    }

    /// <summary>Exclui um usuário.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        var resultado = await service.ExcluirAsync(id, ct);
        return resultado == ResultadoUsuario.Ok ? NoContent() : Erro(resultado);
    }

    private IActionResult Erro(ResultadoUsuario resultado) => resultado switch
    {
        ResultadoUsuario.NaoEncontrado => NaoEncontrado(),
        ResultadoUsuario.CpfDuplicado => Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "CPF já cadastrado.",
            detail: "Já existe um usuário com este CPF."),
        ResultadoUsuario.EmailDuplicado => Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "E-mail já cadastrado.",
            detail: "Já existe um usuário com este e-mail."),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private ObjectResult NaoEncontrado() => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Usuário não encontrado.");
}
