using System.Net;
using Hortalicas.Api.Dtos;
using Hortalicas.Api.Models;
using Hortalicas.Api.Services;

namespace Hortalicas.Tests;

public class SituacaoCanteirosTests
{
    private static readonly DateOnly Hoje = ApiFactory.Hoje;

    private static Plantio Plantio(int previstaEmDias, int colheitas = 0) => new()
    {
        DataPlantio = Hoje.AddDays(-30),
        DataPrevistaColheita = Hoje.AddDays(previstaEmDias),
        Colheitas = Enumerable.Range(0, colheitas).Select(_ => new Colheita()).ToList(),
    };

    [Fact]
    public void Sem_plantio_o_canteiro_esta_vazio() =>
        Assert.Equal(SituacaoCanteiro.Vazio, SituacaoCanteiros.Calcular(null, Hoje));

    [Theory]
    [InlineData(1, SituacaoCanteiro.EmCrescimento)]
    [InlineData(0, SituacaoCanteiro.ProntoParaColher)]
    [InlineData(-3, SituacaoCanteiro.ProntoParaColher)]
    [InlineData(-4, SituacaoCanteiro.Atrasado)]
    public void Situacao_segue_a_data_prevista_com_tolerancia_de_3_dias(int previstaEmDias, SituacaoCanteiro esperada) =>
        Assert.Equal(esperada, SituacaoCanteiros.Calcular(Plantio(previstaEmDias), Hoje));

    [Fact]
    public void Com_colheita_parcial_o_canteiro_fica_em_colheita_mesmo_atrasado() =>
        Assert.Equal(SituacaoCanteiro.EmColheita, SituacaoCanteiros.Calcular(Plantio(-10, colheitas: 1), Hoje));
}

public class PainelApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public PainelApiTests(ApiFactory factory) => _client = factory.CreateClient();

    private async Task<EstufaPainelResponse> CardAsync(Guid estufaId) =>
        (await _client.GetAsync<List<EstufaPainelResponse>>("/api/painel")).Single(e => e.Id == estufaId);

    [Fact]
    public async Task Card_conta_canteiros_por_situacao_e_lista_as_hortalicas()
    {
        var estufa = await _client.CriarEstufaAsync(canteiros: 4);
        var alface = await _client.CriarHortalicaAsync(cicloDias: 45);
        var rucula = await _client.CriarHortalicaAsync(cicloDias: 30);
        await _client.PlantarNoCanteiroAsync(estufa, 1, alface, CadernoDeCampo.DiasAtras(10));
        await _client.PlantarNoCanteiroAsync(estufa, 2, rucula, CadernoDeCampo.DiasAtras(31));
        await _client.PlantarNoCanteiroAsync(estufa, 3, rucula, CadernoDeCampo.DiasAtras(40));

        var card = await CardAsync(estufa.Id);

        Assert.Equal(4, card.TotalCanteiros);
        Assert.Equal(1, card.CanteirosVazios);
        Assert.Equal(1, card.CanteirosEmCrescimento);
        Assert.Equal(1, card.CanteirosProntosParaColher);
        Assert.Equal(1, card.CanteirosAtrasados);
        Assert.Equal(2, card.HortalicasPlantadas.Count);
    }

    [Fact]
    public async Task Estufa_com_plantio_e_sem_irrigacao_alem_do_limite_entra_em_alerta()
    {
        var estufa = await _client.CriarEstufaAsync(diasMaxSemIrrigacao: 2);
        var hortalica = await _client.CriarHortalicaAsync();
        await _client.PlantarNoCanteiroAsync(estufa, 1, hortalica, CadernoDeCampo.DiasAtras(10));
        await (await _client.IrrigarAsync(estufa.Id, CadernoDeCampo.DiasAtras(3), minutos: 40))
            .LerAsync<IrrigacaoResponse>(HttpStatusCode.Created);

        var card = await CardAsync(estufa.Id);
        Assert.True(card.IrrigacaoAtrasada);
        Assert.Equal(3, card.UltimaIrrigacao!.DiasAtras);
        Assert.Equal(40, card.UltimaIrrigacao.DuracaoMinutos);

        var hoje = await _client.GetAsync<HojeResponse>("/api/painel/hoje");
        Assert.Contains(hoje.SemIrrigacao, e => e.EstufaId == estufa.Id);

        await (await _client.IrrigarAsync(estufa.Id, ApiFactory.Hoje)).LerAsync<IrrigacaoResponse>(HttpStatusCode.Created);
        Assert.False((await CardAsync(estufa.Id)).IrrigacaoAtrasada);
    }

    [Fact]
    public async Task Estufa_sem_plantio_nao_entra_em_alerta_de_irrigacao()
    {
        var estufa = await _client.CriarEstufaAsync(diasMaxSemIrrigacao: 1);

        Assert.False((await CardAsync(estufa.Id)).IrrigacaoAtrasada);
    }

    [Fact]
    public async Task Hoje_lista_os_canteiros_para_colher_com_os_atrasados_primeiro()
    {
        var estufa = await _client.CriarEstufaAsync(canteiros: 3);
        var hortalica = await _client.CriarHortalicaAsync(cicloDias: 30);
        await _client.PlantarNoCanteiroAsync(estufa, 1, hortalica, CadernoDeCampo.DiasAtras(30));
        await _client.PlantarNoCanteiroAsync(estufa, 2, hortalica, CadernoDeCampo.DiasAtras(38));
        await _client.PlantarNoCanteiroAsync(estufa, 3, hortalica, CadernoDeCampo.DiasAtras(5));

        var hoje = await _client.GetAsync<HojeResponse>("/api/painel/hoje");
        var daEstufa = hoje.ParaColher.Where(c => c.EstufaId == estufa.Id).ToList();

        Assert.Equal(ApiFactory.Hoje, hoje.Data);
        Assert.Equal([2, 1], daEstufa.Select(c => c.CanteiroNumero));
        Assert.Equal(SituacaoCanteiro.Atrasado, daEstufa[0].Situacao);
        Assert.Equal(8, daEstufa[0].DiasDeAtraso);
    }

    [Fact]
    public async Task Resumo_do_mes_soma_colheitas_descarte_e_irrigacao_por_estufa()
    {
        var estufa = await _client.CriarEstufaAsync(comprimento: 50, largura: 8);
        var hortalica = await _client.CriarHortalicaAsync(cicloDias: 30);
        var plantio = await _client.PlantarNoCanteiroAsync(estufa, 1, hortalica, CadernoDeCampo.DiasAtras(40));
        await (await _client.ColherAsync(plantio.Id, new DateOnly(2026, 10, 2), 6000, false, 1000, "Queimadura"))
            .LerAsync<ColheitaResponse>(HttpStatusCode.Created);
        await (await _client.ColherAsync(plantio.Id, new DateOnly(2026, 10, 20), 4000, true))
            .LerAsync<ColheitaResponse>(HttpStatusCode.Created);
        await (await _client.IrrigarAsync(estufa.Id, new DateOnly(2026, 10, 5), 30)).LerAsync<IrrigacaoResponse>(HttpStatusCode.Created);
        await (await _client.IrrigarAsync(estufa.Id, new DateOnly(2026, 10, 6), 45)).LerAsync<IrrigacaoResponse>(HttpStatusCode.Created);
        await (await _client.IrrigarAsync(estufa.Id, new DateOnly(2026, 9, 30), 60)).LerAsync<IrrigacaoResponse>(HttpStatusCode.Created);

        var resumo = await _client.GetAsync<ResumoMesResponse>("/api/painel/resumo?ano=2026&mes=10");
        var daEstufa = resumo.PorEstufa.Single(e => e.EstufaId == estufa.Id);
        var daHortalica = resumo.PorHortalica.Single(h => h.HortalicaId == hortalica.Id);

        Assert.Equal(10000, daEstufa.AproveitadoG);
        Assert.Equal(1000, daEstufa.DescartadoG);
        Assert.Equal(25, daEstufa.GramasPorM2);
        Assert.Equal(2, daEstufa.Irrigacoes);
        Assert.Equal(75, daEstufa.MinutosIrrigacao);
        Assert.Equal(10000, daHortalica.AproveitadoG);
        Assert.Equal(10000, (await CardAsync(estufa.Id)).ColhidoNoMesG);
    }

    [Fact]
    public async Task Resumo_com_mes_invalido_devolve_400()
    {
        var resposta = await _client.GetAsync("/api/painel/resumo?ano=2026&mes=13");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
}
