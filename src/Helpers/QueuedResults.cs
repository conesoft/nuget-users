using System.Collections.Generic;

namespace Conesoft.Users.Helpers;

class QueuedResults : IResult
{
    readonly Queue<IResult> results = new();

    public static QueuedResults Queue() => new();

    public QueuedResults Push(IResult result)
    {
        results.Enqueue(result);
        return this;
    }

    public QueuedResults PushIfTrue(bool check, Func<IResult> resultGenerator)
    {
        if (check) results.Enqueue(resultGenerator());
        return this;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        foreach (var result in results)
        {
            await result.ExecuteAsync(httpContext);
        }
    }
}