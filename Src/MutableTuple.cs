namespace BtmI2p.MiscUtils
{
    public static class MutableTuple
    {
        public static MutableTuple<T1> Create<T1>(
            T1 item1
        )
        {
            return new MutableTuple<T1>()
            {
                Item1 = item1
            };
        }
        public static MutableTuple<T1, T2> Create<T1, T2>(
            T1 item1,
            T2 item2
        )
        {
            return new MutableTuple<T1, T2>()
            {
                Item1 = item1,
                Item2 = item2
            };
        }
        public static MutableTuple<T1, T2, T3> Create<T1, T2, T3>(
            T1 item1,
            T2 item2,
            T3 item3
        )
        {
            return new MutableTuple<T1, T2, T3>()
            {
                Item1 = item1,
                Item2 = item2,
                Item3 = item3
            };
        }
        public static MutableTuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(
            T1 item1,
            T2 item2,
            T3 item3,
            T4 item4
        )
        {
            return new MutableTuple<T1, T2, T3, T4>()
            {
                Item1 = item1,
                Item2 = item2,
                Item3 = item3,
                Item4 = item4
            };
        }
    }
    public class MutableTuple<T1>
    {
        public T1 Item1;
    }
    public class MutableTuple<T1,T2>
    {
		public T1 Item1;
        public T2 Item2;
    }
    public class MutableTuple<T1, T2, T3>
    {
		public T1 Item1;
        public T2 Item2;
        public T3 Item3;
    }
    public class MutableTuple<T1, T2, T3, T4>
    {
		public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
    }
}
