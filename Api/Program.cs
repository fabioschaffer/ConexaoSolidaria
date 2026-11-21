using System.Text.Json.Serialization;
using Application.Campanhas;
using Infra.Persistencia;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.RespectRequiredConstructorParameters = true;
});
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var conexao = builder.Configuration.GetConnectionString("ConexaoSolidaria")
    ?? throw new InvalidOperationException("Configure a conexão ConnectionStrings:ConexaoSolidaria.");

builder.Services.AddDbContext<ConexaoSolidariaContext>(options => options.UseNpgsql(conexao));
builder.Services.AddScoped<ICampanhaRepositorio, CampanhaRepositorio>();
builder.Services.AddScoped<CampanhaService>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Conexão Solidária v1");
    });
}

app.MapControllers();
app.Run();
