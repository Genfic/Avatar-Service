using System.Diagnostics;

namespace ImageService.Middleware;

public class RequestTimingMiddleware(RequestDelegate next)
{
    public Task Invoke(HttpContext context)
    {
        var watch = new Stopwatch();
        watch.Start();
        
        context.Response.OnStarting(() =>
        {
            watch.Stop();
            context.Response.Headers.Append("X-Response-Time", $"{watch.ElapsedMilliseconds} ms");
            return Task.CompletedTask;
        });

        return next(context);
    }
}