using System;
using Vostok.ClusterConfig.Core.Utils;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal abstract class ReplaceableNodeSerializerV2<TNode> : BaseSettingsNodeSerializerV2<TNode>
        where TNode : ISettingsNode
    {
        public sealed override void GetBinPatch(BinPatchContext context, BinPatchWriter writer)
        {
            if (!EnsureSameType(context, writer, out var oldLength, out var newLength))
                return;
            
            if (oldLength != newLength || context.Old.Buffer.IsEquals(context.Old.Position, context.New.Buffer, context.New.Position, oldLength))
            {
                writer.WriteDelete(oldLength);
                writer.WriteAppend(context.New.Buffer, context.New.Position, newLength);
            }
            else
            {
                writer.WriteNotDifferent(oldLength);
            }

            context.Old.Position += oldLength;
            context.New.Position += newLength;
        }
    }
}