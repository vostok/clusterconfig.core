using System;
using System.Collections.Generic;
using Vostok.ClusterConfig.Core.Patching;
using Vostok.Commons.Binary;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal class AnyNodeSerializerV2 : BaseSettingsNodeSerializerV2<ISettingsNode>
    {
        private readonly ObjectNodeSerializerV2 objectSerializer;
        private readonly ArrayNodeSerializerV2 arraySerializer;
        private readonly ValueNodeSerializerV2 valueSerializer;
        private readonly DeleteNodeSerializerV2 deleteSerializer;

        public AnyNodeSerializerV2()
        {
            objectSerializer = new ObjectNodeSerializerV2(this);
            arraySerializer = new ArrayNodeSerializerV2(this);
            valueSerializer = new ValueNodeSerializerV2();
            deleteSerializer = new DeleteNodeSerializerV2();
        }
        
        public override void Serialize(ISettingsNode node, IBinaryWriter writer)
        {
            switch (node)
            {
                case ObjectNode obj: 
                    objectSerializer.Serialize(obj, writer);
                    break;
                case ArrayNode array:
                    arraySerializer.Serialize(array, writer);
                    break;
                case ValueNode value:
                    valueSerializer.Serialize(value, writer);
                    break;
                case DeleteNode delete: 
                    deleteSerializer.Serialize(delete, writer);
                    break;
                default: throw new InvalidOperationException($"Unknown node type {node?.GetType().Name ?? "null"}");
            }
        }

        public override ISettingsNode Deserialize(IBinaryReader reader, string name)
        {
            PeekHeader(reader, out var type, out _);

            switch (type)
            {
                case NodeType.Object: return objectSerializer.Deserialize(reader, name); 
                case NodeType.Array: return arraySerializer.Deserialize(reader, name); 
                case NodeType.Value: return valueSerializer.Deserialize(reader, name);
                case NodeType.Delete: return deleteSerializer.Deserialize(reader, name);
                default: throw new InvalidOperationException($"Unknown node type {type}");
            }
        }

        public override ISettingsNode Deserialize(IBinaryReader reader, IEnumerator<string> path, string name)
        {
            PeekHeader(reader, out var type, out _);

            switch (type)
            {
                case NodeType.Object: return objectSerializer.Deserialize(reader, path, name); 
                case NodeType.Array: return arraySerializer.Deserialize(reader, path, name); 
                case NodeType.Value: return valueSerializer.Deserialize(reader, path, name);
                case NodeType.Delete: return deleteSerializer.Deserialize(reader, path, name);
                default: throw new InvalidOperationException($"Unknown node type {type}");
            }
        }

        public override void ApplyPatch(BinaryBufferReader settings, BinaryBufferReader patch, IBinaryWriter result)
        {
            PeekHeader(settings, out var settingsType, out _);
            PeekHeader(patch, out var patchType, out _);

            if (patchType == NodeType.Delete)
            {
                SkipNode(settings);
                SkipNode(patch);
            }
            else if (settingsType != patchType)
            {
                SkipNode(settings);
                CopyNode(patch, result);
            }
            else
            {
                switch (patchType)
                {
                    case NodeType.Object:
                        objectSerializer.ApplyPatch(settings, patch, result);
                        break;
                    case NodeType.Array:
                        arraySerializer.ApplyPatch(settings, patch, result);
                        break;
                    case NodeType.Value:
                        valueSerializer.ApplyPatch(settings, patch, result);
                        break;
                    default: 
                        throw new InvalidOperationException($"Unknown node type {patchType}");
                }
            }
        }

        public override void GetBinPatch(BinPatchContext context, BinPatchWriter writer)
        {
            if (IsEnded(context.Old))
            {
                writer.WriteAppend(context.New.Buffer, context.New.Position, context.New.BytesRemaining);
            }
            else if (IsEnded(context.New))
            {
                writer.WriteDelete(context.New.BytesRemaining);
            }
            else
            {
                PeekHeader(context.Old, out var type, out _);

                switch (type)
                {
                    case NodeType.Delete:
                        deleteSerializer.GetBinPatch(context, writer);
                        break;
                    case NodeType.Value:
                        valueSerializer.GetBinPatch(context, writer);
                        break;
                    case NodeType.Array:
                        arraySerializer.GetBinPatch(context, writer);
                        break;
                    case NodeType.Object:
                        objectSerializer.GetBinPatch(context, writer);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown node type {type}");
                }
            }
        }

        private bool IsEnded(BinaryBufferReader reader) => reader.Position >= reader.Buffer.Length;
    }
}