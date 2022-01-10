using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Patching
{
    public class Patcher : IPatcher
    {
        public ISettingsNode GetPatch([CanBeNull] ISettingsNode oldSettings, [CanBeNull] ISettingsNode newSettings)
        {
            if (oldSettings == null)
                return newSettings;

            if (newSettings == null)
                return new DeleteNode(oldSettings.Name);
            
            if (oldSettings.GetType() != newSettings.GetType() || !Comparers.NodeName.Equals(oldSettings.Name, newSettings.Name))
                return newSettings;

            if (oldSettings is ValueNode || oldSettings is ArrayNode)
                return oldSettings.Equals(newSettings) ? null : newSettings;

            if (oldSettings is ObjectNode)
                return GetPatch((ObjectNode) oldSettings, (ObjectNode) newSettings);
            
            throw new NotSupportedException($"Unknown node type {oldSettings.GetType().Name} of node '{oldSettings.Name}'");
        }

        [CanBeNull]
        public ISettingsNode ApplyPatch([CanBeNull] ISettingsNode oldSettings, [CanBeNull] ISettingsNode patch)
        {
            if (patch is DeleteNode)
                return null;
            
            if (patch == null)
                return oldSettings;
            
            if (oldSettings == null)
                return patch;

            if (oldSettings.GetType() != patch.GetType() || !Comparers.NodeName.Equals(oldSettings.Name, patch.Name))
                return patch;

            if (oldSettings is ValueNode || oldSettings is ArrayNode)
                return patch;

            if (oldSettings is ObjectNode)
                return ApplyPatch((ObjectNode)oldSettings, (ObjectNode)patch);
            
            throw new NotSupportedException($"Unknown node type {oldSettings.GetType().Name} of node '{oldSettings.Name}'");
        }

        [CanBeNull] 
        private ISettingsNode GetPatch(ObjectNode oldObject, ObjectNode newObject)
        {
            var differentChild = oldObject.Children
                .Concat(newObject.Children)
                .Select(c => c.Name)
                .Distinct(Comparers.NodeName)
                .Select(key => GetPatch(oldObject[key], newObject[key]))
                .Where(diff => diff != null)
                .ToList();

            return differentChild.Any()
                ? new ObjectNode(oldObject.Name, differentChild)
                : null;
        }

        private ISettingsNode ApplyPatch(ObjectNode oldSettings, ObjectNode patch)
        {
            var result = oldSettings.Children.ToDictionary(c => c.Name, Comparers.NodeName);

            foreach (var patchChild in patch.Children)
            {
                var key = patchChild.Name ?? throw new InvalidOperationException("Key can't be null");

                if (ApplyPatch(oldSettings[key], patchChild) is {} patched)
                    result[key] = patched;
                else
                    result.Remove(key);
            }

            return new ObjectNode(oldSettings.Name, result.Values);
        }
    }
}