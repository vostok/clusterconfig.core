using JetBrains.Annotations;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Patching
{
    internal interface IPatcher
    {
        [CanBeNull]
        ISettingsNode GetPatch([CanBeNull] ISettingsNode oldSettings, [CanBeNull] ISettingsNode newSettings);

        [CanBeNull]
        ISettingsNode ApplyPatch([CanBeNull] ISettingsNode oldSettings, [CanBeNull] ISettingsNode patch);
    }
}