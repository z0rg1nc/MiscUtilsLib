using System;
using System.Collections.Generic;
using System.Linq;

namespace BtmI2p.MiscUtils
{
    public static class LinqExtensions
    {
        public static IEnumerable<Tuple<int, T1>> WithIndex<T1>(
            this IEnumerable<T1> enumerable
            )
        {
            return enumerable.Select((item, i) => Tuple.Create(i, item));
        }
    }
}
