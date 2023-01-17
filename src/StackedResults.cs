using System.Collections.Generic;

namespace Conesoft.Users;

class StackedResults : IResult
{
    readonly Stack<IResult> results = new();

    public static StackedResults Stack() => new();

    public StackedResults Push(IResult result)
    {
        results.Push(result);
        return this;
    }

    public StackedResults PushIfTrue(bool check, IResult result)
    {
        if(check) results.Push(result);
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