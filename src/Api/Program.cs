using Latrovu.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<MailPoller>();

var app = builder.Build();
app.MapGet("/", () => "Latrovu API");
app.Run();
