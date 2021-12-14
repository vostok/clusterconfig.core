using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            using (Node(writer, NodeType.Object))
            {
                foreach (var child in node.Children.OrderBy(n => n.Name, Comparers.NodeNameComparer))
                {
                    WriteKey(child.Name, writer);
                    any.Serialize(child, writer);
                }
            }
        }

        public override ObjectNode Deserialize(IBinaryReader reader, string name)
        {
            var enumerator = new ObjectEnumerator<IBinaryReader>(reader);
            var children = new List<ISettingsNode>();
            while (enumerator.MoveNext())
                children.Add(any.Deserialize(reader, enumerator.CurrentKey));

            return new ObjectNode(name, children);
        }

        public override ISettingsNode Deserialize(IBinaryReader reader, IEnumerator<string> path, string name)
        {
            if (!path.MoveNext())
                return Deserialize(reader, name);

            var enumerator = new ObjectEnumerator<IBinaryReader>(reader);
            while (enumerator.MoveNext())
            {
                if (Comparers.NodeName.Equals(path.Current, enumerator.CurrentKey))
                    return any.Deserialize(reader, path, enumerator.CurrentKey);
                
                SkipNode(reader);
            }
            
            return null;
        }

        #region Apply patch
        public override void ApplyPatch(BinaryBufferReader settings, BinaryBufferReader patch, IBinaryWriter result)
        {
            using (Node(result, NodeType.Object))
                MergeChildren(new ObjectEnumerator<BinaryBufferReader>(settings), new ObjectEnumerator<BinaryBufferReader>(patch), result);
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
        #endregion

        public override void GetBinPatch(BinPatchContext context, BinPatchWriter writer)
        {
            if (!EnsureSameType(context, writer, out var oldLength, out var newLength))
                return;

            var oldEnumerator = new ObjectEnumerator<BinaryBufferReader>(context.Old, oldLength);
            var newEnumerator = new ObjectEnumerator<BinaryBufferReader>(context.New, newLength);

            var oldKey = default(string);
            var newKey = default(string);

            while (true)
            {
                oldKey = oldKey ?? (oldEnumerator.MoveNext() ? oldEnumerator.CurrentKey : null);
                newKey = newKey ?? (newEnumerator.MoveNext() ? newEnumerator.CurrentKey : null);

                if (oldKey == null && newKey == null)
                    break;

                var comp = Comparers.NodeNameComparer.Compare(oldKey ?? string.Empty, newKey ?? string.Empty);
                
                if (oldKey == null || comp > 0) // note (a.tolstov, 05.12.2021): В новом объекте появился ключ, которого небыло в старом
                {
                    PeekHeader(context.New, out _, out var length);
                    writer.WriteAppend(context.New.Buffer, context.New.Position - newEnumerator.CurrentKeyLengthBytes, newEnumerator.CurrentKeyLengthBytes + length);
                    context.New.Position += length;
                    
                    newKey = null;
                } 
                else if (newKey == null || comp < 0) // note (a.tolstov, 05.12.2021): В старом объекте есть ключ, который пропал в новом
                {
                    PeekHeader(context.Old, out _, out var length);
                    writer.WriteDelete(oldEnumerator.CurrentKeyLengthBytes + length);
                    context.Old.Position += length;
                    
                    oldKey = null;
                }
                else  // note (a.tolstov, 05.12.2021): Ключ есть и в старом, и в новом объекте
                {
                    if (oldKey != newKey) // note (a.tolstov, 05.12.2021): Вдруг поменялся регистр 
                    {
                        writer.WriteDelete(oldEnumerator.CurrentKeyLengthBytes);
                        writer.WriteAppend(context.New.Buffer, context.New.Position - newEnumerator.CurrentKeyLengthBytes, newEnumerator.CurrentKeyLengthBytes + newLength);
                    }
                    else
                    {
                        writer.WriteNotDifferent(oldEnumerator.CurrentKeyLengthBytes);
                    }

                    any.GetBinPatch(context, writer);
                    
                    oldKey = null;
                    newKey = null;
                }
            }
        }

        private static void WriteKey(string key, IBinaryWriter writer)
        {
            writer.WriteVarlen((ulong)Encoding.GetByteCount(key ?? throw new NullReferenceException("Key can't be null")));
            writer.WriteWithoutLength(Encoding.GetBytes(key));
        }

        private static string ReadKey(IBinaryReader reader)
        {
            var length = (int)reader.ReadVarlenUInt64();
            return Encoding.GetString(reader.ReadByteArray(length));
        }

        private class ObjectEnumerator<TReader>
            where TReader : class, IBinaryReader
        {
            public readonly TReader Reader;

            private readonly long begin;            
            private readonly long end;

            public ObjectEnumerator(TReader reader, long length)
            {
                Reader = reader;

                begin = reader.Position;
                end = begin + length;
            }
            
            public ObjectEnumerator(TReader reader)
            {
                Reader = reader;

                EnsureType(Reader, NodeType.Object, out var length);
                
                begin = reader.Position;
                end = begin + length;
            }

            public string CurrentKey { get; private set; }
            public int CurrentKeyLengthBytes { get; private set; }

            public bool MoveNext()
            {
                if (Reader.Position >= end)
                    return false;

                var beforeReadKey = Reader.Position;
                CurrentKey = ReadKey(Reader);
                CurrentKeyLengthBytes = (int) (Reader.Position - beforeReadKey);

                return true;
            }

            public void Reset()
            {
                Reader.Position = begin;
                CurrentKey = null;
                CurrentKeyLengthBytes = 0;
            }
         }
    }
}