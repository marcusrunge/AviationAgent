using System.ComponentModel;
using MarcusRunge.AviationAgent.Core.Properties;
using MarcusRunge.Toolbox.Localization.Core;

namespace MarcusRunge.AviationAgent.Core;

/// <summary>
/// Identifies an aviation weather product requested from a deterministic data source.
/// </summary>
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum WeatherProduct
{
    [LocalizedDescription("WeatherProductMetar", typeof(Resources))]
    Metar,

    [LocalizedDescription("WeatherProductTaf", typeof(Resources))]
    Taf,
}
