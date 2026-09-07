using System.ComponentModel;
using MarcusRunge.AviationAgent.Core.Properties;
using MarcusRunge.Toolbox.Localization.Core;

namespace MarcusRunge.AviationAgent.Core;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum AgentAction
{
    [LocalizedDescription("AgentActionGetMetar", typeof(Resources))] GetMetar,
    [LocalizedDescription("AgentActionGetTaf", typeof(Resources))] GetTaf,
    [LocalizedDescription("AgentActionGetMetarAndTaf", typeof(Resources))] GetMetarAndTaf,
    [LocalizedDescription("AgentActionExplainPreviousResult", typeof(Resources))] ExplainPreviousResult,
    [LocalizedDescription("AgentActionFilterPreviousResult", typeof(Resources))] FilterPreviousResult,
    [LocalizedDescription("AgentActionUnsupportedOperationalDecision", typeof(Resources))] UnsupportedOperationalDecision,
    [LocalizedDescription("AgentActionUnknown", typeof(Resources))] Unknown,
}
