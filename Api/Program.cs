using System.Text.Json.Serialization;
using Api.Observabilidade;
using Api.Seguranca;
using Application.Campanhas;
using Application.Doacoes;
using Application.Usuarios;
using Infra.Mensageria;
using Infra.Seguranca;
using Infra.Persistencia;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.RespectRequiredConstructorParameters = true;
});
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };
        document.Security = [new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        }];
        return Task.CompletedTask;
    });
    options.AddOperationTransformer((operation, context, _) =>
    {
        if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            operation.Security = [];
        }
        return Task.CompletedTask;
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var tokens = new TokenJwt(builder.Configuration, TimeProvider.System);
builder.Services.AddSingleton(tokens);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = tokens.ParametrosValidacao;
});
builder.Services.AddAuthorizationBuilder().SetFallbackPolicy(
    new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var conexao = builder.Configuration.GetConnectionString("ConexaoSolidaria")
    ?? throw new InvalidOperationException("Configure a conexão ConnectionStrings:ConexaoSolidaria.");

builder.Services.AddDbContext<ConexaoSolidariaContext>(options => options.UseNpgsql(conexao));
builder.Services.AddScoped<ICampanhaRepositorio, CampanhaRepositorio>();
builder.Services.AddScoped<CampanhaService>();
builder.Services.AddScoped<IDoacaoRepositorio, DoacaoRepositorio>();
builder.Services.AddOptions<RabbitMqOptions>().BindConfiguration("RabbitMq");
builder.Services.AddSingleton<IDoacaoRecebidaPublicador, RabbitMqDoacaoRecebidaPublicador>();
builder.Services.AddScoped<DoacaoService>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IHashSenha, HashSenha>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ConexaoSolidariaContext>();
    await context.Database.MigrateAsync();
}

app.UseRouting();
app.MapMetricasHttp();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Conexão Solidária v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.Run();

//demonstração workflow