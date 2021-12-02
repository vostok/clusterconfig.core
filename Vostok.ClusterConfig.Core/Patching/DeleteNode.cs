using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Vostok.Configuration.Abstractions.Merging;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Patching
{
    public class DeleteNode : ISettingsNode
    {
        /// <summary>
        /// Creates a new <see cref="ValueNode"/> with the given <paramref name="name"/>.
        /// </summary>
        public DeleteNode([CanBeNull] string name = null)
        {
            Name = name;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string Value { get; } = null;

        /// <inheritdoc />
        public ISettingsNode Merge(ISettingsNode other, SettingsMergeOptions options = null)
        {
            options = options ?? SettingsMergeOptions.Default;

            if (options.CustomMerge != null)
            {
                var (ok, merged) = options.CustomMerge(this, other);
                if (ok)
                    return merged;
            }
            
            return other ?? this;
        }

        public override string ToString() => "DELETE";

        IEnumerable<ISettingsNode> ISettingsNode.Children => Enumerable.Empty<ValueNode>();

        ISettingsNode ISettingsNode.this[string name] => null;

        #region Equality

        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as DeleteNode);

        public bool Equals(DeleteNode other) => ReferenceEquals(this, other) || Comparers.NodeName.Equals(Name, other.Name);

        /// <summary>
        /// Returns the hash code of the current <see cref="ObjectNode"/>.
        /// </summary>
        public override int GetHashCode() => Name != null ? Comparers.NodeName.GetHashCode(Name) : 0;

        #endregion
    }
}