using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hortalicas.Api.Dtos;

namespace Hortalicas.Tests;

/// <summary>Atalhos para montar o cenário dos testes pela própria API (estufa, hortaliça, plantio, irrigação, colheita).</summary>
internal static class CadernoDeCampo
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static DateOnly DiasAtras(int dias) => ApiFactory.Hoje.AddDays(-dias);

    public static async Task<T> LerAsync<T>(this HttpResponseMessage resposta, HttpStatusCode esperado)
    {
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.True(resposta.StatusCode == esperado, $"Esperado {esperado}, veio {resposta.StatusCode}: {corpo}");
        return JsonSerializer.Deserialize<T>(corpo, Json)!;
    }

    public static async Task<T> GetAsync<T>(this HttpClient client, string rota) =>
        await (await client.GetAsync(rota)).LerAsync<T>(HttpStatusCode.OK);

    public static async Task<EstufaResponse> CriarEstufaAsync(
        this HttpClient client, int canteiros = 4, int diasMaxSemIrrigacao = 2, double comprimento = 50, double largura = 8) =>
        await (await client.PostAsJsonAsync("/api/estufas", new EstufaRequest
        {
            Nome = $"Estufa {Guid.NewGuid():N}"[..20],
            ComprimentoM = comprimento,
            LarguraM = largura,
            QuantidadeCanteiros = canteiros,
            DiasMaxSemIrrigacao = diasMaxSemIrrigacao,
        })).LerAsync<EstufaResponse>(HttpStatusCode.Created);

    public static async Task<HortalicaResponse> CriarHortalicaAsync(this HttpClient client, int cicloDias = 45, string? nome = null) =>
        await (await client.PostAsJsonAsync("/api/hortalicas", new HortalicaRequest
        {
            Nome = nome ?? $"Alface {Guid.NewGuid():N}"[..20],
            CicloDias = cicloDias,
        })).LerAsync<HortalicaResponse>(HttpStatusCode.Created);

    public static async Task<MapaEstufaResponse> MapaAsync(this HttpClient client, Guid estufaId) =>
        await client.GetAsync<MapaEstufaResponse>($"/api/estufas/{estufaId}/mapa");

    public static Task<HttpResponseMessage> PlantarAsync(this HttpClient client, Guid canteiroId, Guid hortalicaId, DateOnly data) =>
        client.PostAsJsonAsync("/api/plantios", new PlantioRequest
        {
            CanteiroId = canteiroId,
            HortalicaId = hortalicaId,
            DataPlantio = data,
        });

    public static async Task<PlantioResponse> PlantarNoCanteiroAsync(
        this HttpClient client, EstufaResponse estufa, int numero, HortalicaResponse hortalica, DateOnly data)
    {
        var canteiro = (await client.MapaAsync(estufa.Id)).Canteiros.Single(c => c.Numero == numero);
        return await (await client.PlantarAsync(canteiro.Id, hortalica.Id, data)).LerAsync<PlantioResponse>(HttpStatusCode.Created);
    }

    public static Task<HttpResponseMessage> ColherAsync(
        this HttpClient client, Guid plantioId, DateOnly data, int aproveitadoG, bool encerra,
        int descartadoG = 0, string? motivo = null) =>
        client.PostAsJsonAsync("/api/colheitas", new ColheitaRequest
        {
            PlantioId = plantioId,
            Data = data,
            PesoAproveitadoG = aproveitadoG,
            PesoDescartadoG = descartadoG,
            MotivoDescarte = motivo,
            EncerraCiclo = encerra,
        });

    public static Task<HttpResponseMessage> IrrigarAsync(this HttpClient client, Guid estufaId, DateOnly data, int minutos = 30) =>
        client.PostAsJsonAsync("/api/irrigacoes", new IrrigacaoRequest
        {
            EstufaId = estufaId,
            Data = data,
            DuracaoMinutos = minutos,
        });
}
