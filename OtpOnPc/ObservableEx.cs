using System;
using System.Linq;
using System.Reactive.Linq;

namespace OtpOnPc;

public static class ObservableEx
{
    public static IObservable<(TSource? OldValue, TSource? NewValue)> CombineWithPrevious<TSource>(this IObservable<TSource> source)
    {
        return source.Scan((default(TSource), default(TSource)), (previous, current) => (previous.Item2, current))
            .Select(t => (t.Item1, t.Item2));
    }

    public static IObservable<TResult> CombineWithPrevious<TSource, TResult>(
        this IObservable<TSource> source,
        Func<TSource?, TSource?, TResult> resultSelector)
    {
        return source.Scan((default(TSource), default(TSource)), (previous, current) => (previous.Item2, current))
            .Select(t => resultSelector(t.Item1, t.Item2));
    }
}
