using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace BtmI2p.MiscUtils
{
    public static class LinqAsyncExtensions
    {
        public static async Task<IList<TItemTo>> SelectAsync<TItemFrom,TItemTo>(
            this IEnumerable<TItemFrom> enumerable,
            Func<TItemFrom,Task<TItemTo>> selectFunc
        )
        {
            Assert.NotNull(enumerable);
            Assert.NotNull(selectFunc);
            var result = new List<TItemTo>();
            foreach (var itemFrom in enumerable)
            {
                result.Add(await selectFunc(itemFrom).ConfigureAwait(false));
            }
            return result;
        }

        public static async Task<IList<TItem>> WhereAsync<TItem>(
            this IEnumerable<TItem> enumerable,
            Func<TItem, Task<bool>> whereFunc
            )
        {
            Assert.NotNull(enumerable);
            Assert.NotNull(whereFunc);
            var result = new List<TItem>();
            foreach (var item in enumerable)
            {
                if(await whereFunc(item).ConfigureAwait(false))
                    result.Add(item);
            }
            return result;
        }
    }
}
