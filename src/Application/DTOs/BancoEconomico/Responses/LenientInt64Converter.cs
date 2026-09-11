using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.DTOs.BancoEconomico.Responses;

/// <summary>
/// Convierte enteros de 64 bits tolerando los formatos que envía el banco:
/// número entero, número con decimales (ej. 18459521.0) o texto.
/// </summary>
public sealed class LenientInt64Converter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return (long)reader.GetDecimal();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var text = (reader.GetString() ?? string.Empty).Trim();

            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
            {
                return integer;
            }

            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var numeric))
            {
                return (long)numeric;
            }
        }

        throw new JsonException($"No se pudo convertir el valor JSON a Int64 (token: {reader.TokenType}).");
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);
}
