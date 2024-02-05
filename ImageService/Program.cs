using ImageService.Endpoints;
using ImageService.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";
        c.InjectStylesheet("https://cdn.genfic.net/file/Ogma-net/swagger-dark.css");
    });
}

// app.UseDefaultFiles();
app.UseFileServer();
app.UseHttpsRedirection();

// Middleware
app.UseMiddleware<RequestTimingMiddleware>();

// Endpoints
app
    .MapGenerateAvatars()
    .MapGenerateCovers()
    .MapGet("ping", () => $"pong {DateTimeOffset.Now}");

app.Run();