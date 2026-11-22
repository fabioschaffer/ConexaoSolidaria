using Application.Doacoes;
using Infra.Mensageria;
using Infra.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var conexao = builder.Configuration.GetConnectionString("ConexaoSolidaria")
    ?? throw new InvalidOperationException("Configure a conexão ConnectionStrings:ConexaoSolidaria.");

builder.Services.AddDbContext<ConexaoSolidariaContext>(options => options.UseNpgsql(conexao));
builder.Services.AddScoped<IDoacaoRepositorio, DoacaoRepositorio>();
builder.Services.AddScoped<ProcessarDoacao>();
builder.Services.AddOptions<RabbitMqOptions>().BindConfiguration("RabbitMq");
builder.Services.AddHostedService<RabbitMqDoacaoRecebidaConsumidor>();

await builder.Build().RunAsync();
