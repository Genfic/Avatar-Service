using AvatarService.Endpoints;
using AvatarService.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";
        c.InjectStylesheet("/css/swagger-dark.css");
    });
}

app.UseHttpsRedirection();

// Middleware
app.UseMiddleware<RequestTimingMiddleware>();

// Endpoints
app.MapGenerateAvatars();

app.Run();