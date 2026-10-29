using System.Net;
using System.Net.Http.Json;
using Hortalicas.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Hortalicas.Tests;

public class UsuariosApiTests : IClassFixture<ApiFactory>
{
    private const string Rota = "/api/usuarios";
    private readonly HttpClient _client;

    public UsuariosApiTests(ApiFactory factory) => _client = factory.CreateClient();

    private static UsuarioRequest NovoUsuario(string cpf, string email) => new()
    {
        Nome = "Maria da Silva",
        Cpf = cpf,
        Email = email,
        Telefone = "(11) 98765-4321"
    };

    private async Task<UsuarioResponse> CriarAsync(UsuarioRequest request)
    {
        var resposta = await _client.PostAsJsonAsync(Rota, request);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<UsuarioResponse>())!;
    }

    [Fact]
    public async Task Cadastrar_devolve_201_com_cpf_e_telefone_normalizados()
    {
        var resposta = await _client.PostAsJsonAsync(Rota, NovoUsuario("529.982.247-25", "Maria@Exemplo.com"));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);
        var usuario = await resposta.Content.ReadFromJsonAsync<UsuarioResponse>();
        Assert.Equal("Maria da Silva", usuario!.Nome);
        Assert.Equal("52998224725", usuario.Cpf);
        Assert.Equal("maria@exemplo.com", usuario.Email);
        Assert.Equal("11987654321", usuario.Telefone);
    }

    [Fact]
    public async Task Cadastrar_sem_campos_obrigatorios_devolve_400_com_erro_em_cada_campo()
    {
        var resposta = await _client.PostAsJsonAsync(Rota, new UsuarioRequest());

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Nome", problema!.Errors.Keys);
        Assert.Contains("Cpf", problema.Errors.Keys);
        Assert.Contains("Email", problema.Errors.Keys);
        Assert.Contains("Telefone", problema.Errors.Keys);
    }

    [Theory]
    [InlineData("Cpf", "123.456.789-00", "joao@exemplo.com", "11987654321")]
    [InlineData("Email", "390.533.447-05", "email-invalido", "11987654321")]
    [InlineData("Telefone", "390.533.447-05", "joao@exemplo.com", "98765-4321")]
    public async Task Cadastrar_com_campo_invalido_devolve_400(string campo, string cpf, string email, string telefone)
    {
        var request = new UsuarioRequest { Nome = "João", Cpf = cpf, Email = email, Telefone = telefone };

        var resposta = await _client.PostAsJsonAsync(Rota, request);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal([campo], problema!.Errors.Keys);
    }

    [Fact]
    public async Task Cadastrar_cpf_ja_existente_devolve_409()
    {
        await CriarAsync(NovoUsuario("111.444.777-35", "primeiro@exemplo.com"));

        var resposta = await _client.PostAsJsonAsync(Rota, NovoUsuario("11144477735", "segundo@exemplo.com"));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("CPF já cadastrado.", problema!.Title);
    }

    [Fact]
    public async Task Cadastrar_email_ja_existente_devolve_409()
    {
        await CriarAsync(NovoUsuario("935.411.347-80", "repetido@exemplo.com"));

        var resposta = await _client.PostAsJsonAsync(Rota, NovoUsuario("714.602.380-01", "REPETIDO@exemplo.com"));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("E-mail já cadastrado.", problema!.Title);
    }

    [Fact]
    public async Task Listar_e_obter_devolvem_usuario_cadastrado()
    {
        var criado = await CriarAsync(NovoUsuario("248.438.034-80", "listar@exemplo.com"));

        var lista = await _client.GetFromJsonAsync<List<UsuarioResponse>>(Rota);
        var obtido = await _client.GetFromJsonAsync<UsuarioResponse>($"{Rota}/{criado.Id}");

        Assert.Contains(lista!, u => u.Id == criado.Id);
        Assert.Equal(criado, obtido);
    }

    [Fact]
    public async Task Obter_inexistente_devolve_404_problem_details()
    {
        var resposta = await _client.GetAsync($"{Rota}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Editar_atualiza_os_dados()
    {
        var criado = await CriarAsync(NovoUsuario("862.427.610-17", "editar@exemplo.com"));
        var alteracao = new UsuarioRequest
        {
            Nome = "Maria Souza",
            Cpf = "862.427.610-17",
            Email = "novo@exemplo.com",
            Telefone = "(21) 3333-4444"
        };

        var resposta = await _client.PutAsJsonAsync($"{Rota}/{criado.Id}", alteracao);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var obtido = await _client.GetFromJsonAsync<UsuarioResponse>($"{Rota}/{criado.Id}");
        Assert.Equal(new UsuarioResponse(criado.Id, "Maria Souza", "86242761017", "novo@exemplo.com", "2133334444"), obtido);
    }

    [Fact]
    public async Task Editar_com_email_de_outro_usuario_devolve_409()
    {
        await CriarAsync(NovoUsuario("357.282.680-21", "dono@exemplo.com"));
        var outro = await CriarAsync(NovoUsuario("496.367.910-82", "outro@exemplo.com"));

        var resposta = await _client.PutAsJsonAsync($"{Rota}/{outro.Id}", NovoUsuario("496.367.910-82", "dono@exemplo.com"));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Editar_com_dado_invalido_devolve_400()
    {
        var criado = await CriarAsync(NovoUsuario("608.394.250-07", "editar-invalido@exemplo.com"));

        var resposta = await _client.PutAsJsonAsync($"{Rota}/{criado.Id}", NovoUsuario("000.000.000-00", "editar-invalido@exemplo.com"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Editar_inexistente_devolve_404()
    {
        var resposta = await _client.PutAsJsonAsync($"{Rota}/{Guid.NewGuid()}", NovoUsuario("529.982.247-25", "x@exemplo.com"));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Excluir_remove_o_usuario()
    {
        var criado = await CriarAsync(NovoUsuario("738.212.120-80", "excluir@exemplo.com"));

        var resposta = await _client.DeleteAsync($"{Rota}/{criado.Id}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"{Rota}/{criado.Id}")).StatusCode);
    }

    [Fact]
    public async Task Excluir_inexistente_devolve_404()
    {
        var resposta = await _client.DeleteAsync($"{Rota}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Cors_libera_front_local()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, Rota);
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "DELETE");

        var resposta = await _client.SendAsync(request);

        Assert.Equal("http://localhost:4200", resposta.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
