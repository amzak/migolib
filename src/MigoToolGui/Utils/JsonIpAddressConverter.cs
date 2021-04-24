using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MigoToolGui.Utils
{
    public class JsonIpAddressConverter : JsonConverter<IPAddress>
    {
        public override IPAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new InvalidOperationException("Unable to deserialize IPAddress");
            }

            var token = reader.GetString();
            return IPAddress.Parse(token);
        }

        public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}