using System.Collections.Generic;

namespace ACE.Common.Extensions;

public static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = ThreadSafeRandom.Next(0, n);
            var value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static float Product(this IList<float> list)
    {
        var totalProduct = 1.0f;
        foreach (var item in list)
        {
            totalProduct *= item;
        }

        return totalProduct;
    }
}
