using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace Rvnx.CRM.API.Helpers;

/// <summary>
/// Applies a JSON merge patch (RFC 7396) to an existing object.
/// Only properties present in the JSON document are overwritten;
/// absent properties are left unchanged.
/// </summary>
public static class JsonMergePatchHelper
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache = new();

    private static Dictionary<string, PropertyInfo> GetPropertyMap(Type type)
    {
        return PropertyCache.GetOrAdd(type, t => t
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && !string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Merges the properties present in <paramref name="patch"/> into <paramref name="target"/>.
    /// Returns the mutated <paramref name="target"/>.
    /// </summary>
    public static T ApplyPatch<T>(T target, JsonElement patch) where T : class
    {
        if (patch.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Patch body must be a JSON object.");
        }

        Dictionary<string, PropertyInfo> propertyMap = GetPropertyMap(typeof(T));

        foreach (JsonProperty jsonProp in patch.EnumerateObject())
        {
            if (!propertyMap.TryGetValue(jsonProp.Name, out PropertyInfo? prop))
            {
                continue;
            }

            object? value = JsonSerializer.Deserialize(
                jsonProp.Value.GetRawText(),
                prop.PropertyType,
                DeserializeOptions);

            prop.SetValue(target, value);
        }

        return target;
    }

    /// <summary>
    /// Validates the patched object using DataAnnotation attributes.
    /// Returns a list of validation errors, or an empty list if valid.
    /// </summary>
    public static List<string> Validate<T>(T target) where T : class
    {
        ValidationContext context = new(target);
        List<ValidationResult> results = [];
        Validator.TryValidateObject(target, context, results, validateAllProperties: true);
        return results
            .Where(r => !string.IsNullOrEmpty(r.ErrorMessage))
            .Select(r => r.ErrorMessage!)
            .ToList();
    }
}
