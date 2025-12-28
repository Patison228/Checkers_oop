using CheckersServer.Hubs;

var builder = WebApplication.CreateBuilder(args);

// SignalR
builder.Services.AddSignalR();

// CORS дл€ локальной разработки
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("https://localhost:7026", "http://localhost:5096", "https://localhost:5001")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost");

// ?? HEALTH CHECK ЁЌƒѕќ»Ќ“
app.MapGet("/", () => "?? Checkers Server работает!");
app.MapGet("/health", () => Results.Ok(new
{
    Status = "OK",
    Timestamp = DateTime.UtcNow,
    Urls = new
    {
        Https = "https://localhost:7026",
        Http = "http://localhost:5096"
    },
    SignalRHub = "https://localhost:7026/checkersHub"
}));

// SignalR Hub
app.MapHub<CheckersHub>("/checkersHub");

app.Run();
