namespace Api.Observabilidade;

internal static class MetricasHttp
{
    public static void MapMetricasHttp(this WebApplication app)
    {
        var contadores = new Contadores();

        app.Use(async (context, next) =>
        {
            if (!string.Equals(context.Request.Path.Value?.TrimEnd('/'), "/health", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(context.Request.Path.Value?.TrimEnd('/'), "/metrics", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.OnCompleted(() =>
                {
                    lock (contadores)
                    {
                        contadores.Requisicoes++;
                        if (context.Response.StatusCode >= 500 && context.Response.StatusCode < 600)
                        {
                            contadores.Erros5xx++;
                        }
                    }

                    return Task.CompletedTask;
                });
            }

            await next(context);
        });

        app.MapGet("/metrics", () =>
        {
            lock (contadores)
            {
                return Results.Ok(new { requisicoes = contadores.Requisicoes, erros5xx = contadores.Erros5xx });
            }
        }).AllowAnonymous().ExcludeFromDescription();
    }

    private sealed class Contadores
    {
        public long Requisicoes { get; set; }
        public long Erros5xx { get; set; }
    }
}
