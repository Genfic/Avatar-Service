using System.Diagnostics;

namespace ImageService.Middleware;

public class RequestTimingMiddleware
{
    private RequestDelegate _next;
    public RequestTimingMiddleware(RequestDelegate next) => _next = next;

    public Task Invoke(HttpContext context)
    {
        var watch = new Stopwatch();
        watch.Start();
        
        context.Response.OnStarting(() =>
        {
            watch.Stop();
            context.Response.Headers.Add("X-Response-Time", $"{watch.ElapsedMilliseconds} ms");
            return Task.CompletedTask;
        });

        return _next(context);
    }
}