using System.ComponentModel;
using MarcusRunge.AviationAgent.Core.Properties;
using MarcusRunge.Toolbox.Localization.Core;

namespace MarcusRunge.AviationAgent.Core;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum TafChangeKind
{
    [LocalizedDescription("TafChangeKindBase", typeof(Resources))] Base,
    [LocalizedDescription("TafChangeKindFrom", typeof(Resources))] From,
    [LocalizedDescription("TafChangeKindBecoming", typeof(Resources))] Becoming,
    [LocalizedDescription("TafChangeKindTemporary", typeof(Resources))] Temporary,
    [LocalizedDescription("TafChangeKindProbability", typeof(Resources))] Probability,
    [LocalizedDescription("TafChangeKindProbabilityTemporary", typeof(Resources))] ProbabilityTemporary,
}
