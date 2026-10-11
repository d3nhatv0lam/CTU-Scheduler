using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CTUScheduler.Core.Models.Settings;

namespace CTUScheduler.AppServices.Helpers.Json;

public class AppThemeJsonConverter : JsonConverter<AppTheme>
{
    public override AppTheme Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (Enum.TryParse<AppTheme>(value, ignoreCase: true, out var theme)
                && Enum.IsDefined(theme))
            {
                return theme;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number
                 && reader.TryGetInt32(out var number)
                 && Enum.IsDefined(typeof(AppTheme), number))
        {
            return (AppTheme)number;
        }

        return AppTheme.System;
    }

    public override void Write(Utf8JsonWriter writer, AppTheme value, JsonSerializerOptions options)
    {
        var stringValue = Enum.IsDefined(value) ? value.ToString() : nameof(AppTheme.System);
        writer.WriteStringValue(stringValue);
    }
}