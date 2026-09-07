using System.Globalization;
using System.Resources;

namespace MarcusRunge.AviationAgent.Core.Properties;

/// <summary>Provides strongly typed access to localized Aviation Agent resources.</summary>
public static class Resources
{
    private static readonly ResourceManager Manager = new("MarcusRunge.AviationAgent.Core.Properties.Resources", typeof(Resources).Assembly);

    public static CultureInfo? Culture { get; set; }
    public static ResourceManager ResourceManager => Manager;
    public static string GetString(string name) => Manager.GetString(name, Culture) ?? $"[[{name}]]";
}
