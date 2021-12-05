using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vostok.ClusterConfig.Core.Utils;
using Vostok.Commons.Binary;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal class ObjectNodeSerializerV2 : BaseSettingsNodeSerializerV2<ObjectNode>
    {
        private static readonly Encoding Encoding = Encoding.UTF8;

        private readonly BaseSettingsNodeSerializerV2<ISettingsNode> any;

        public ObjectNodeSerializerV2(BaseSettingsNodeSerializerV2<ISettingsNode> any) => this.any = any;

        public override void Serialize(ObjectNode node, IBinaryWriter writer)
        {
            using (BeginNode(writer, NodeType.Object))
            {
                writer.Write(node.ChildrenCount);

                foreach (var child in node.Children.OrderBy(n => n.Name, Comparers.NodeNameComparer))
                {
                    WriteKey(child.Name, writer);
                    any.Serialize(child, writer);
                }
            }
        }

        public override ObjectNode Deserialize(IBinaryReader reader, string name)
        {
            EnsureType(reader, NodeType.Object);
            
            var enumerator = new ObjectEnumerator<IBinaryReader>(reader);
            var children = new List<ISettingsNode>(enumerator.Count);
            while (enumerator.MoveNext())
                children.Add(any.Deserialize(reader, enumerator.CurrentKey));

            return new ObjectNode(name, children);
        }

        public override ISettingsNode Deserialize(IBinaryReader reader, IEnumerator<string> path, string name)
        {
            if (!path.MoveNext())
                return Deserialize(reader, name);

            EnsureType(reader, NodeType.Object);

            var enumerator = new ObjectEnumerator<IBinaryReader>(reader);
            while (enumerator.MoveNext())
            {
                if (Comparers.NodeName.Equals(path.Current, enumerator.CurrentKey))
                    return any.Deserialize(reader, path, enumerator.CurrentKey);
                
                SkipNode(reader);
            }
            
            return null;
        }

        public override void ApplyPatch(BinaryBufferReader settings, BinaryBufferReader patch, IBinaryWriter result)
        {
            EnsureType(settings, NodeType.Object);
            EnsureType(patch, NodeType.Object);

            using (BeginNode(result, NodeType.Object))
            {
                var childCountVariable = result.AddIntVariable();

                var childCount = MergeChildren(new ObjectEnumerator<BinaryBufferReader>(settings), new ObjectEnumerator<BinaryBufferReader>(patch), result);
                
                childCountVariable.Set(childCount);
            }
        }

        private int MergeChildren(ObjectEnumerator<BinaryBufferReader> settings, ObjectEnumerator<BinaryBufferReader> patch, IBinaryWriter result)
        {
            var count = 0;

            var settingsKey = default(string);
            var patchKey = default(string);

            while (true)
            {
                settingsKey = settingsKey ?? (settings.MoveNext() ? settings.CurrentKey : null);
                patchKey = patchKey ?? (patch.MoveNext() ? patch.CurrentKey : null);

                if (settingsKey == null && patchKey == null)
                    break;
                
                if (patchKey == null)
                {
                    CopyChildFromSettings();
                    continue;
                }

                if (settingsKey == null)
                {
                    CopyChildFromPatch();
                    continue;
                }

                var comparsion = Comparers.NodeNameComparer.Compare(settingsKey, patchKey);
                if (comparsion == 0) MergeChilds();
                else if (comparsion < 0) CopyChildFromSettings();
                else CopyChildFromPatch();
            }
            
            return count;

            void CopyChildFromSettings()
            {
                WriteKey(settingsKey, result);
                CopyNode(settings.Reader, result);
                count++;
                settingsKey = null;
            }

            void CopyChildFromPatch()
            {
                WriteKey(patchKey, result);
                CopyNode(patch.Reader, result);
                count++;
                patchKey = null;
            }
            
            void MergeChilds()
            {
                PeekHeader(patch.Reader, out var patchType, out _);
                if (patchType == NodeType.Delete)
                {
                    SkipNode(settings.Reader);
                    SkipNode(patch.Reader);
                }
                else
                {
                    WriteKey(patchKey, result);
                    any.ApplyPatch(settings.Reader, patch.Reader, result);
                    count++;
                }

                settingsKey = null;
                patchKey = null;
            }
        }


        private static void WriteKey(string key, IBinaryWriter writer) =>
            writer.WriteWithLength(key ?? throw new InvalidOperationException("Key can't be null"));

        private class ObjectEnumerator<TReader>
            where TReader : IBinaryReader
        {
            public int Index = -1;
            public readonly TReader Reader;

            public ObjectEnumerator(TReader reader)
            {
                Reader = reader;
                Count = reader.ReadInt32();
            }

            public string CurrentKey { get; private set; }
            public int Count { get; }

            public bool MoveNext()
            {
                if (++Index >= Count)
                    return false;

                CurrentKey = Reader.ReadString();

                return true;
            }
        }
    }
}