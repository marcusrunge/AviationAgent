using System.ComponentModel;
using MarcusRunge.AviationAgent.Core.Properties;
using MarcusRunge.Toolbox.Localization.Core;

namespace MarcusRunge.AviationAgent.Core;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum AgentFocus
{
    [LocalizedDescription("AgentFocusFull", typeof(Resources))] Full,
    [LocalizedDescription("AgentFocusWind", typeof(Resources))] Wind,
    [LocalizedDescription("AgentFocusVisibility", typeof(Resources))] Visibility,
    [LocalizedDescription("AgentFocusClouds", typeof(Resources))] Clouds,
    [LocalizedDescription("AgentFocusWeather", typeof(Resources))] Weather,
    [LocalizedDescription("AgentFocusTemperature", typeof(Resources))] Temperature,
    [LocalizedDescription("AgentFocusPressure", typeof(Resources))] Pressure,
    [LocalizedDescription("AgentFocusValidity", typeof(Resources))] Validity,
    [LocalizedDescription("AgentFocusChanges", typeof(Resources))] Changes,
    [LocalizedDescription("AgentFocusWorstConditions", typeof(Resources))] WorstConditions,
    [LocalizedDescription("AgentFocusNone", typeof(Resources))] None,
}
