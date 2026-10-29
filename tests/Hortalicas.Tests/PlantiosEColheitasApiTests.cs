using System.Net;
using System.Net.Http.Json;
using Hortalicas.Api.Dtos;
using Hortalicas.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Hortalicas.Tests;

public class PlantiosEColheitasApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public PlantiosEColheitasApiTests(ApiFactory factory) => _client = factory.CreateClient();

    private async Task<(EstufaResponse Estufa, PlantioResponse Plantio)> PlantioAsync(int cicloDias = 45, int diasAtras = 50)
    {
        var estufa = await _client.CriarEstufaAsync();
        var hortalica = await _client.CriarHortalicaAsync(cicloDias);
        var plantio = await _client.PlantarNoCanteiroAsync(estufa, 1, hortalica, CadernoDeCampo.DiasAtras(diasAtras));
        return (estufa, plantio);
    }

    [Fact]
    public async Task Plantio_calcula_a_colheita_prevista_pelo_ciclo_da_hortalica()
    {
        var (_, plantio) = await PlantioAsync(cicloDias: 45, diasAtras: 10);

        Assert.Equal(new DateOnly(2026, 10, 18), plantio.DataPlantio);
        Assert.Equal(new DateOnly(2026, 12, 2), plantio.DataPrevistaColheita);
        Assert.Equal(SituacaoPlantio.EmAndamento, plantio.Situacao);
    }

    [Fact]
    public async Task Plantar_em_canteiro_ocupado_devolve_409()
    {
        var (estufa, plantio) = await PlantioAsync();
        var outra = await _client.CriarHortalicaAsync();

        var resposta = await _client.PlantarAsync(plantio.CanteiroId, outra.Id, ApiFactory.Hoje);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var problema = await resposta.LerAsync<ProblemDetails>(HttpStatusCode.Conflict);
        Assert.Contains($"canteiro 1 da {estufa.Nome}", problema.Detail);
    }

    [Fact]
    public async Task Plantio_com_data_futura_devolve_400()
    {
        var estufa = await _client.CriarEstufaAsync();
        var hortalica = await _client.CriarHortalicaAsync();
        var canteiro = (await _client.MapaAsync(estufa.Id)).Canteiros[0];

        var problema = await (await _client.PlantarAsync(canteiro.Id, hortalica.Id, ApiFactory.Hoje.AddDays(1)))
            .LerAsync<ValidationProblemDetails>(HttpStatusCode.BadRequest);

        Assert.Contains("DataPlantio", problema.Errors.Keys);
    }

    [Fact]
    public async Task Colheita_parcial_mantem_o_plantio_e_o_canteiro_fica_em_colheita()
    {
        var (estufa, plantio) = await PlantioAsync();

        var colheita = await (await _client.ColherAsync(plantio.Id, ApiFactory.Hoje, 1200, encerra: false))
            .LerAsync<ColheitaResponse>(HttpStatusCode.Created);

        Assert.Matches(@"^20261028-[0-9A-F]{6}$", colheita.Lote);
        var canteiro = (await _client.MapaAsync(estufa.Id)).Canteiros.Single(c => c.Numero == 1);
        Assert.Equal(SituacaoCanteiro.EmColheita, canteiro.Situacao);
        Assert.Equal(1200, canteiro.Plantio!.ColhidoG);
    }

    [Fact]
    public async Task Colheita_final_encerra_o_plantio_e_libera_o_canteiro_para_novo_plantio()
    {
        var (estufa, plantio) = await PlantioAsync();
        await (await _client.ColherAsync(plantio.Id, CadernoDeCampo.DiasAtras(1), 800, encerra: false))
            .LerAsync<ColheitaResponse>(HttpStatusCode.Created);

        await (await _client.ColherAsync(plantio.Id, ApiFactory.Hoje, 15000, encerra: true, descartadoG: 500, motivo: "Pendoamento"))
            .LerAsync<ColheitaResponse>(HttpStatusCode.Created);

        var encerrado = await _client.GetAsync<PlantioResponse>($"/api/plantios/{plantio.Id}");
        Assert.Equal(SituacaoPlantio.Encerrado, encerrado.Situacao);
        Assert.Equal(ApiFactory.Hoje, encerrado.DataEncerramento);
        Assert.Equal(15800, encerrado.ColhidoG);
        Assert.Equal(500, encerrado.DescartadoG);
        Assert.Equal(SituacaoCanteiro.Vazio, (await _client.MapaAsync(estufa.Id)).Canteiros.Single(c => c.Numero == 1).Situacao);

        var replantio = await _client.PlantarAsync(plantio.CanteiroId, plantio.HortalicaId, ApiFactory.Hoje);
        Assert.Equal(HttpStatusCode.Created, replantio.StatusCode);
    }

    [Fact]
    public async Task Colher_plantio_encerrado_devolve_409()
    {
        var (_, plantio) = await PlantioAsync();
        await (await _client.ColherAsync(plantio.Id, ApiFactory.Hoje, 1000, encerra: true))
            .LerAsync<ColheitaResponse>(HttpStatusCode.Created);

        var resposta = await _client.ColherAsync(plantio.Id, ApiFactory.Hoje, 1000, encerra: false);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Theory]
    [InlineData(1000, 300, null, "MotivoDescarte")]
    [InlineData(0, 0, null, "PesoAproveitadoG")]
    public async Task Colheita_com_peso_invalido_devolve_400(int aproveitado, int descartado, string? motivo, string campo)
    {
        var (_, plantio) = await PlantioAsync();

        var problema = await (await _client.ColherAsync(plantio.Id, ApiFactory.Hoje, aproveitado, false, descartado, motivo))
            .LerAsync<ValidationProblemDetails>(HttpStatusCode.BadRequest);

        Assert.Contains(campo, problema.Errors.Keys);
    }

    [Fact]
    public async Task Colheita_antes_do_plantio_devolve_400()
    {
        var (_, plantio) = await PlantioAsync(diasAtras: 10);

        var problema = await (await _client.ColherAsync(plantio.Id, CadernoDeCampo.DiasAtras(11), 1000, encerra: false))
            .LerAsync<ValidationProblemDetails>(HttpStatusCode.BadRequest);

        Assert.Contains("Data", problema.Errors.Keys);
    }

    [Fact]
    public async Task Excluir_colheita_final_reabre_o_plantio()
    {
        var (_, plantio) = await PlantioAsync();
        var colheita = await (await _client.ColherAsync(plantio.Id, ApiFactory.Hoje, 1000, encerra: true))
            .LerAsync<ColheitaResponse>(HttpStatusCode.Created);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/colheitas/{colheita.Id}")).StatusCode);

        var reaberto = await _client.GetAsync<PlantioResponse>($"/api/plantios/{plantio.Id}");
        Assert.Equal(SituacaoPlantio.EmAndamento, reaberto.Situacao);
        Assert.Null(reaberto.DataEncerramento);
    }

    [Fact]
    public async Task Excluir_colheita_final_com_canteiro_replantado_devolve_409()
    {
        var (_, plantio) = await PlantioAsync();
        var colheita = await (await _client.ColherAsync(plantio.Id, ApiFactory.Hoje, 1000, encerra: true))
            .LerAsync<ColheitaResponse>(HttpStatusCode.Created);
        await (await _client.PlantarAsync(plantio.CanteiroId, plantio.HortalicaId, ApiFactory.Hoje))
            .LerAsync<PlantioResponse>(HttpStatusCode.Created);

        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/api/colheitas/{colheita.Id}")).StatusCode);
    }

    [Fact]
    public async Task Encerrar_plantio_por_perda_libera_o_canteiro_e_exige_motivo()
    {
        var (estufa, plantio) = await PlantioAsync();

        var semMotivo = await _client.PostAsJsonAsync($"/api/plantios/{plantio.Id}/encerrar", new EncerrarPlantioRequest { Data = ApiFactory.Hoje });
        Assert.Equal(HttpStatusCode.BadRequest, semMotivo.StatusCode);

        var encerrado = await (await _client.PostAsJsonAsync($"/api/plantios/{plantio.Id}/encerrar",
                new EncerrarPlantioRequest { Data = ApiFactory.Hoje, Motivo = "Ataque de pulgão" }))
            .LerAsync<PlantioResponse>(HttpStatusCode.OK);

        Assert.Equal("Ataque de pulgão", encerrado.MotivoEncerramento);
        Assert.Equal(SituacaoCanteiro.Vazio, (await _client.MapaAsync(estufa.Id)).Canteiros.Single(c => c.Numero == 1).Situacao);
    }

    [Fact]
    public async Task Excluir_plantio_com_colheita_devolve_409_e_sem_colheita_devolve_204()
    {
        var (_, colhido) = await PlantioAsync();
        await (await _client.ColherAsync(colhido.Id, ApiFactory.Hoje, 500, encerra: false)).LerAsync<ColheitaResponse>(HttpStatusCode.Created);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/api/plantios/{colhido.Id}")).StatusCode);

        var (_, engano) = await PlantioAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/plantios/{engano.Id}")).StatusCode);
    }

    [Fact]
    public async Task Irrigacao_valida_duracao_e_data_futura()
    {
        var estufa = await _client.CriarEstufaAsync();

        var futura = await (await _client.IrrigarAsync(estufa.Id, ApiFactory.Hoje.AddDays(1)))
            .LerAsync<ValidationProblemDetails>(HttpStatusCode.BadRequest);
        Assert.Contains("Data", futura.Errors.Keys);

        var semDuracao = await (await _client.IrrigarAsync(estufa.Id, ApiFactory.Hoje, minutos: 0))
            .LerAsync<ValidationProblemDetails>(HttpStatusCode.BadRequest);
        Assert.Contains("DuracaoMinutos", semDuracao.Errors.Keys);
    }
}
