using System.Net;
using System.Net.Http.Json;
using Hortalicas.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Hortalicas.Tests;

public class EstufasApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public EstufasApiTests(ApiFactory factory) => _client = factory.CreateClient();

    private static EstufaRequest Pedido(string nome, int canteiros = 6) => new()
    {
        Nome = nome,
        ComprimentoM = 50,
        LarguraM = 8,
        QuantidadeCanteiros = canteiros,
        DiasMaxSemIrrigacao = 2,
    };

    [Fact]
    public async Task Cadastrar_cria_os_canteiros_numerados_e_calcula_a_area()
    {
        var estufa = await (await _client.PostAsJsonAsync("/api/estufas", Pedido("Estufa Norte", 6)))
            .LerAsync<EstufaResponse>(HttpStatusCode.Created);

        Assert.Equal(400, estufa.AreaM2);
        Assert.Equal(6, estufa.QuantidadeCanteiros);
        var mapa = await _client.MapaAsync(estufa.Id);
        Assert.Equal([1, 2, 3, 4, 5, 6], mapa.Canteiros.Select(c => c.Numero));
        Assert.All(mapa.Canteiros, c => Assert.Equal(SituacaoCanteiro.Vazio, c.Situacao));
        Assert.All(mapa.Canteiros, c => Assert.Equal(66.7, c.AreaM2));
    }

    [Fact]
    public async Task Cadastrar_sem_campos_obrigatorios_devolve_400_em_cada_campo()
    {
        var problema = await (await _client.PostAsJsonAsync("/api/estufas", new EstufaRequest()))
            .LerAsync<ValidationProblemDetails>(HttpStatusCode.BadRequest);

        Assert.Contains("Nome", problema.Errors.Keys);
        Assert.Contains("ComprimentoM", problema.Errors.Keys);
        Assert.Contains("LarguraM", problema.Errors.Keys);
        Assert.Contains("QuantidadeCanteiros", problema.Errors.Keys);
        Assert.Contains("DiasMaxSemIrrigacao", problema.Errors.Keys);
    }

    [Fact]
    public async Task Cadastrar_nome_repetido_ignorando_maiusculas_devolve_409()
    {
        await (await _client.PostAsJsonAsync("/api/estufas", Pedido("Estufa Sul"))).LerAsync<EstufaResponse>(HttpStatusCode.Created);

        var resposta = await _client.PostAsJsonAsync("/api/estufas", Pedido("estufa sul"));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Editar_aumenta_e_reduz_canteiros_vazios()
    {
        var estufa = await _client.CriarEstufaAsync(canteiros: 4);

        var aumentada = await (await _client.PutAsJsonAsync($"/api/estufas/{estufa.Id}", Pedido(estufa.Nome, 7)))
            .LerAsync<EstufaResponse>(HttpStatusCode.OK);
        Assert.Equal(7, aumentada.QuantidadeCanteiros);

        var reduzida = await (await _client.PutAsJsonAsync($"/api/estufas/{estufa.Id}", Pedido(estufa.Nome, 3)))
            .LerAsync<EstufaResponse>(HttpStatusCode.OK);
        Assert.Equal(3, reduzida.QuantidadeCanteiros);
        Assert.Equal([1, 2, 3], (await _client.MapaAsync(estufa.Id)).Canteiros.Select(c => c.Numero));
    }

    [Fact]
    public async Task Reduzir_canteiros_que_tem_plantio_devolve_409()
    {
        var estufa = await _client.CriarEstufaAsync(canteiros: 4);
        var hortalica = await _client.CriarHortalicaAsync();
        await _client.PlantarNoCanteiroAsync(estufa, 4, hortalica, CadernoDeCampo.DiasAtras(5));

        var resposta = await _client.PutAsJsonAsync($"/api/estufas/{estufa.Id}", Pedido(estufa.Nome, 3));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Excluir_estufa_sem_historico_devolve_204_e_com_irrigacao_devolve_409()
    {
        var vazia = await _client.CriarEstufaAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/estufas/{vazia.Id}")).StatusCode);

        var irrigada = await _client.CriarEstufaAsync();
        await (await _client.IrrigarAsync(irrigada.Id, ApiFactory.Hoje)).LerAsync<IrrigacaoResponse>(HttpStatusCode.Created);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/api/estufas/{irrigada.Id}")).StatusCode);
    }

    [Fact]
    public async Task Mapa_de_estufa_inexistente_devolve_404()
    {
        var resposta = await _client.GetAsync($"/api/estufas/{Guid.NewGuid()}/mapa");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Hortalica_com_plantio_nao_pode_ser_excluida()
    {
        var estufa = await _client.CriarEstufaAsync();
        var plantada = await _client.CriarHortalicaAsync();
        var nova = await _client.CriarHortalicaAsync();
        await _client.PlantarNoCanteiroAsync(estufa, 1, plantada, ApiFactory.Hoje);

        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/api/hortalicas/{plantada.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/hortalicas/{nova.Id}")).StatusCode);
    }
}
