using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hortalicas.Api.Json;

/// <summary>
/// Aceita hora como "HH:mm" (o que o campo de hora do navegador envia) ou "HH:mm:ss"; devolve sempre "HH:mm:ss".
/// </summary>
public sealed class HoraJsonConverter : JsonConverter<TimeOnly>
{
    private static readonly string[] Formatos = ["HH:mm", "HH:mm:ss", "HH:mm:ss.FFFFFFF"];

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var texto = reader.GetString();
        if (TimeOnly.TryParseExact(texto, Formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora))
            return hora;
        throw new JsonException("Hora inválida. Use HH:mm, ex.: 06:30.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
}
