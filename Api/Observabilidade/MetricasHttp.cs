using Microsoft.AspNetCore.Routing;

namespace Api.Observabilidade;

internal static class MetricasHttp
{
    public static void MapMetricasHttp(this WebApplication app)
    {
        var instancia = Guid.NewGuid();
        var endpoints = new Dictionary<(string Metodo, string Rota), long>();
        var sincronizacao = new object();
        long requisicoes = 0;
        long erros5xx = 0;

        app.Use(async (context, next) =>
        {
            var caminho = context.Request.Path.Value?.TrimEnd('/');
            if (!string.Equals(caminho, "/health", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(caminho, "/metrics", StringComparison.OrdinalIgnoreCase))
            {
                var rota = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
                var metodo = context.Request.Method;

                context.Response.OnCompleted(() =>
                {
                    var falhou = context.Response.StatusCode is >= 500 and < 600;
                    lock (sincronizacao)
                    {
                        requisicoes++;
                        if (falhou)
                            erros5xx++;

                        if (falhou && rota is not null)
                        {
                            var chave = (metodo, "/" + rota.TrimStart('/'));
                            endpoints[chave] = endpoints.GetValueOrDefault(chave) + 1;
                        }
                    }

                    return Task.CompletedTask;
                });
            }

            await next(context);
        });

        app.MapGet("/metrics", () =>
        {
            lock (sincronizacao)
            {
                var porEndpoint = endpoints.Select(item => new
                {
                    metodo = item.Key.Metodo,
                    rota = item.Key.Rota,
                    erros5xx = item.Value
                }).ToArray();

                return Results.Ok(new { instancia, requisicoes, erros5xx, endpoints = porEndpoint });
            }
        }).AllowAnonymous().ExcludeFromDescription();
    }
}
