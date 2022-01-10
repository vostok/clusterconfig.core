namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal class KeyValuePairsEnumerator
    {
        public int Index = -1;
        public readonly NodeReader Reader;

        public KeyValuePairsEnumerator(NodeReader reader)
        {
            Reader = reader;
            Count = reader.Reader.ReadInt32();
        }

        public string CurrentKey { get; private set; }
        public int Count { get; }

        public bool MoveNext()
        {
            if (++Index >= Count)
                return false;

            CurrentKey = Reader.ReadKey();

            return true;
        }
    }
}