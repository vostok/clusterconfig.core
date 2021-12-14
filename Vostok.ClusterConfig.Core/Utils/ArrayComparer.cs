namespace Vostok.ClusterConfig.Core.Utils
{
    public static class ArrayComparer
    {
        public static bool IsEquals(this byte[] array1, long from1, byte[] array2, long from2, long length)
        {
            //TODO(a.tolstov): Use more efficiency way to compare two arrays
            for (var i = 0; i < length; i++)
            {
                if (array1[from1 + i] != array2[from2 + length])
                    return false;
            }

            return true;
        }
    }
}