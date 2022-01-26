using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPad.Extensions
{
    public static class CollectionExtensions
    {
        public static Task ForEachAsync<T>(this IEnumerable<T> collection, int degreeOfParallelism, Func<T, Task> asyncAction)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(collection).GetPartitions(degreeOfParallelism)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await asyncAction(partition.Current);
                })
            );
        }
    }
}
