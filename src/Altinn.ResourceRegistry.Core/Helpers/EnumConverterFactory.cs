using Altinn.Platform.Storage.Interface.Enums;
using System;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.ResourceRegistry.Core.Helpers
{

    /// <summary>
    /// facotry
    /// </summary>
    public class EnumConverterFactory : JsonConverterFactory
    {
        /// <summary>
        ///  asda
        /// </summary>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsEnum;
        }

        /// <summary>
        ///  asda
        /// </summary>
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Type converterType = typeof(EnumStringValueConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType);
        }

        private class EnumStringValueConverter<TEnum> : JsonConverter<TEnum>
        {
            public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException($"Expected string for {typeToConvert.Name}.");
                }

                string enumString = reader.GetString();

                foreach (var enumValue in Enum.GetValues(typeToConvert))
                {
                    var enumMemberAttribute = typeToConvert.GetField(enumValue.ToString())
                        .GetCustomAttributes(typeof(EnumMemberAttribute), false)
                        .FirstOrDefault() as EnumMemberAttribute;

                    if (enumMemberAttribute != null && enumMemberAttribute.Value == enumString)
                    {
                        return (TEnum)enumValue;
                    }
                }

                throw new JsonException($"Unknown {typeToConvert.Name} value: {enumString}");
            }

            /// <summary>
            /// doc
            /// </summary>
            public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
            {
                Type typeToConvert = value.GetType();
                var enumMemberAttribute = typeToConvert.GetField(value.ToString())
                    .GetCustomAttributes(typeof(EnumMemberAttribute), false)
                    .FirstOrDefault() as EnumMemberAttribute;

                writer.WriteStringValue(enumMemberAttribute?.Value ?? value.ToString());
            }
        }
    }

}
