using Pacientes.Api.Data;
using Pacientes.Api.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddScoped<IPacienteRepository, PacienteRepository>();
builder.Services.AddRabbitMqMessaging(builder.Configuration);

var app = builder.Build();
app.UseStaticFiles();
app.MapControllers();
app.MapGet("/swagger", () => Results.Redirect("/swagger/index.html"));
app.MapGet("/health", () => Results.Ok(new { service = "pacientes", status = "ok" }));
app.Run();
