using JetBrains.Annotations;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Patching
{
    internal interface IPatcher
    {
        [CanBeNull]
        ISettingsNode GetPatch([NotNull] ISettingsNode oldSettings, [NotNull] ISettingsNode newSettings);

        [CanBeNull]
        ISettingsNode ApplyPatch([NotNull] ISettingsNode oldSettings, [CanBeNull] ISettingsNode patch);
    }
}