using System;
using System.Collections.Generic;
using System.Globalization;
using ACE.Server.Managers;

namespace ACE.Server.Entity.AdminUi;

/// <summary>
/// A server property's current value. Type is bool, long, double or string: the plugin changes it with @modify(type).
/// Value is written the way that command reads it back. A secret's value never goes out in chat: it's null when it's set
/// and "" when it isn't.
/// </summary>
public record AdminUiSetting(string Type, string Key, string Value, bool Secret);

/// <summary>
/// What a server property is for, and its default in DefaultPropertyManager. These don't change while the server is up, so
/// the plugin asks for them once, apart from the values it asks for again after every change.
/// </summary>
public record AdminUiSettingDoc(string Type, string Key, string Default, string Description);

/// <summary>
/// Every server property PropertyManager knows (the ones in DefaultPropertyManager, which are the only ones /modifybool and
/// the rest can change), for the plugin's Server Settings tab.
/// </summary>
public static class AdminUiSettings
{
    public static List<AdminUiSetting> GetValues()
    {
        var settings = new List<AdminUiSetting>();

        foreach (var (key, property) in DefaultPropertyManager.DefaultBooleanProperties)
        {
            settings.Add(Value("bool", key, Format(PropertyManager.GetBool(key, property.Item).Item)));
        }

        foreach (var (key, property) in DefaultPropertyManager.DefaultLongProperties)
        {
            settings.Add(Value("long", key, Format(PropertyManager.GetLong(key, property.Item).Item)));
        }

        foreach (var (key, property) in DefaultPropertyManager.DefaultDoubleProperties)
        {
            settings.Add(Value("double", key, Format(PropertyManager.GetDouble(key, property.Item).Item)));
        }

        foreach (var (key, property) in DefaultPropertyManager.DefaultStringProperties)
        {
            settings.Add(Value("string", key, PropertyManager.GetString(key, property.Item).Item));
        }

        return settings;
    }

    public static List<AdminUiSettingDoc> GetDocs()
    {
        var docs = new List<AdminUiSettingDoc>();

        foreach (var (key, property) in DefaultPropertyManager.DefaultBooleanProperties)
        {
            docs.Add(new AdminUiSettingDoc("bool", key, Format(property.Item), property.Description));
        }

        foreach (var (key, property) in DefaultPropertyManager.DefaultLongProperties)
        {
            docs.Add(new AdminUiSettingDoc("long", key, Format(property.Item), property.Description));
        }

        foreach (var (key, property) in DefaultPropertyManager.DefaultDoubleProperties)
        {
            docs.Add(new AdminUiSettingDoc("double", key, Format(property.Item), property.Description));
        }

        foreach (var (key, property) in DefaultPropertyManager.DefaultStringProperties)
        {
            docs.Add(new AdminUiSettingDoc("string", key, property.Item, property.Description));
        }

        return docs;
    }

    internal static AdminUiSetting Value(string type, string key, string value)
    {
        if (IsSecret(key))
        {
            return new AdminUiSetting(type, key, string.IsNullOrEmpty(value) ? "" : null, true);
        }

        return new AdminUiSetting(type, key, value, false);
    }

    /// <summary>
    /// Keys like proxycheck_api_key. Admins can still set them from the plugin, but their values aren't sent, since chat
    /// lines can end up in logs and other plugins.
    /// </summary>
    internal static bool IsSecret(string key)
    {
        return key.EndsWith("_api_key", StringComparison.Ordinal)
            || key.Contains("password", StringComparison.Ordinal)
            || key.Contains("secret", StringComparison.Ordinal)
            || key.Contains("token", StringComparison.Ordinal);
    }

    // the same text /modifybool, /modifylong and /modifydouble parse, so a value can go straight back into one of them

    internal static string Format(bool value)
    {
        return value ? "true" : "false";
    }

    internal static string Format(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string Format(double value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }
}
