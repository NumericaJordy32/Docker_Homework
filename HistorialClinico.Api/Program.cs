using HistorialClinico.Api.Data;
using HistorialClinico.Api.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddScoped<IHistorialRepository, HistorialRepository>();
builder.Services.AddRabbitMqMessaging(builder.Configuration);

var app = builder.Build();
app.UseStaticFiles();
app.MapControllers();
app.MapGet("/swagger", () => Results.Redirect("/swagger/index.html"));
app.MapGet("/health", () => Results.Ok(new { service = "historial-clinico", status = "ok" }));
app.Run();
