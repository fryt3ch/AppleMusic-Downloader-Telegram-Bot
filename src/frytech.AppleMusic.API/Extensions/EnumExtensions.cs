using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Extensions;

public static class EnumExtensions
{
    public static string GetValue<T>(this T @enum) where T : IConvertible
    {
        if (!typeof(T).IsEnum)
            throw new ArgumentException("T must be an enumerated type");

        var enumValue = @enum.ToString(CultureInfo.InvariantCulture);
        var fieldInfo = @enum.GetType().GetField(enumValue);
        var attribute = fieldInfo?.GetCustomAttribute<JsonStringEnumMemberNameAttribute>();
        
        return attribute?.Name ?? enumValue;
    }
}