using Microsoft.EntityFrameworkCore;
using RealTimeChat.Database;
using RealTimeChat.Server.Configuration;
using RealTimeChat.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<ChatSettings>(builder.Configuration.GetSection(ChatSettings.SectionName));

builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseInMemoryDatabase("ChatDb"));

builder.Services.AddSingleton<MessageCache>();
builder.Services.AddSingleton<ChatWebSocketHandler>();
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddSingleton<MessageMapper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseWebSockets();

app.UseAuthorization();

app.MapControllers();

// WebSocket endpoint
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = context.RequestServices.GetRequiredService<ChatWebSocketHandler>();
        await handler.HandleAsync(webSocket, context.RequestAborted);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
