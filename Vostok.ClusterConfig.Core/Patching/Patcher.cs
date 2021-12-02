using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Patching
{
    public class Patcher : IPatcher
    {
        public ISettingsNode GetPatch([NotNull] ISettingsNode oldSettings, [NotNull] ISettingsNode newSettings)
        {
            if (oldSettings is DeleteNode)
                throw new ArgumentException(nameof(oldSettings), $"{nameof(DeleteNode)} is not supported for patch calculation");
            
            if (newSettings is DeleteNode)
                throw new ArgumentException(nameof(newSettings), $"{nameof(DeleteNode)} is not supported for patch calculation");

            if (oldSettings.GetType() != newSettings.GetType() || oldSettings.Name != newSettings.Name)
                return newSettings;

            if (oldSettings is ValueNode || oldSettings is ArrayNode)
                return GetReplacePatch(oldSettings, newSettings);

            if (oldSettings is ObjectNode)
                return GetPatch((ObjectNode) oldSettings, (ObjectNode) newSettings);
            
            throw new NotSupportedException($"Unknown node type {oldSettings.GetType().Name}");
        }

        [CanBeNull]
        public ISettingsNode ApplyPatch([NotNull] ISettingsNode oldSettings, [CanBeNull] ISettingsNode patch)
        {
            if (oldSettings is DeleteNode)
                throw new ArgumentException(nameof(oldSettings), $"{nameof(DeleteNode)} is not supported for patch calculation");

            if (patch == null)
                return oldSettings;

            if (patch is DeleteNode)
                return null;
            
            if (oldSettings.GetType() != patch.GetType() || oldSettings.Name != patch.Name)
                return patch;

            if (oldSettings is ValueNode || oldSettings is ArrayNode)
                return patch;

            if (oldSettings is ObjectNode)
                return ApplyPatch((ObjectNode)oldSettings, (ObjectNode)patch);
            
            throw new NotSupportedException($"Unknown node type {oldSettings.GetType().Name}");
        }

        #region GetPatch

        [CanBeNull]
        private ISettingsNode GetReplacePatch(ISettingsNode oldSettings, ISettingsNode newSettings)
        {
            return oldSettings.Equals(newSettings) ? null : newSettings;
        }

        [CanBeNull] 
        private ISettingsNode GetPatch(ObjectNode oldObject, ObjectNode newObject)
        {
            var differentChild = new List<ISettingsNode>();

            foreach (var key in oldObject.Children.Concat(newObject.Children).Select(c => c.Name).Distinct())
            {
                var oldChild = oldObject[key];
                var newChild = newObject[key];

                ISettingsNode diff = default;
                
                if (oldChild != null && newChild != null) diff = GetPatch(oldChild, newChild);
                else if (oldChild != null) diff = new DeleteNode(key);
                else if (newChild != null) diff = newChild;
                
                if (diff != null)
                    differentChild.Add(diff);
            }

            return differentChild.Any()
                ? new ObjectNode(oldObject.Name, differentChild)
                : null;
        }

        #endregion

        #region ApplyPatch

        public ISettingsNode ApplyPatch(ObjectNode oldSettings, ObjectNode patch)
        {
            var children = oldSettings.Children.ToDictionary(c => c.Name);

            foreach (var patchChild in patch.Children)
            {
                var key = patchChild.Name ?? throw new InvalidOperationException("Key can't be null");
                
                if (patchChild is DeleteNode)
                    children.Remove(key);
                else if (children.TryGetValue(key, out var oldChild))
                    children[key] = ApplyPatch(oldChild, patchChild);
                else
                    children[key] = patchChild;
            }

            return new ObjectNode(oldSettings.Name, children.Values);
        }

        #endregion
    }
}