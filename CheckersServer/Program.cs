using CheckersServer;
using CheckersServer.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<GameManager>();

var app = builder.Build();

app.MapHub<CheckersHub>("/checkersHub");

app.Run();
