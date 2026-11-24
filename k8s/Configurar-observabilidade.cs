#:property PublishAot=false
#:package Npgsql@10.0.0

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;

const string zabbixUrl = "http://zabbix-web:8080/api_jsonrpc.php";
const string grafanaUrl = "http://grafana:3000";
const string grupo = "ConexaoSolidaria";
const string host = "ConexaoSolidaria API";
const string usuarioLeitura = "grafana_conexao";
const string bancoAplicacao = "conexaosolidaria";
const string bancoZabbix = "zabbix";

var fonteAplicacao = new { type = "postgres", uid = "postgres-conexao" };
var fonteZabbix = new { type = "postgres", uid = "postgres-zabbix-conexao" };
var fontePrometheus = new { type = "prometheus", uid = "prometheus-conexao" };
var prometheusUrl = Environment.GetEnvironmentVariable("PROMETHEUS_URL");
const string namespaceMetricas = "conexaosolidaria";
var metricas = new[]
{
    (Campo: "requisicoes", Titulo: "Requisições HTTP"),
    (Campo: "erros5xx", Titulo: "Erros HTTP 5xx")
};

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

var senhaGrafana = Environment.GetEnvironmentVariable("GRAFANA_ADMIN_PASSWORD")
    ?? throw new InvalidOperationException("Defina GRAFANA_ADMIN_PASSWORD.");
var credenciaisGrafana = Convert.ToBase64String(Encoding.UTF8.GetBytes($"admin:{senhaGrafana}"));

await AguardarAsync("API do Zabbix", () => ZabbixAsync("apiinfo.version", Array.Empty<object>()));
await AguardarAsync("API do Grafana", () => GrafanaAsync(HttpMethod.Get, "/api/health"));

var sessao = (await ZabbixAsync("user.login", new { username = "Admin", password = "zabbix" }))
    .GetString()!;
try
{
    await ConfigurarZabbixAsync(sessao);
}
finally
{
    await ZabbixAsync("user.logout", Array.Empty<object>(), sessao);
}

var senhaLeitura = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
await ConfigurarPostgresAsync(senhaLeitura);
await ConfigurarFonteAsync(fonteAplicacao.uid, bancoAplicacao, "PostgreSQL Conexão Solidária", senhaLeitura);
await ConfigurarFonteAsync(fonteZabbix.uid, bancoZabbix, "Histórico Zabbix Conexão Solidária", senhaLeitura);
if (!string.IsNullOrWhiteSpace(prometheusUrl))
{
    await SalvarFonteAsync(fontePrometheus.uid, new
    {
        uid = fontePrometheus.uid,
        type = fontePrometheus.type,
        name = "Prometheus Kubernetes",
        access = "proxy",
        url = prometheusUrl,
        jsonData = new { timeInterval = "15s" }
    });
}
await ConfigurarDashboardAsync();

Console.WriteLine("Coleta Zabbix e dashboard Conexão Solidária configurados.");

async Task ConfigurarZabbixAsync(string admin)
{
    var grupoId = await ObterOuCriarAsync(admin, "hostgroup",
        new { filter = new { name = grupo } },
        new { name = grupo }, "groupid", "groupids");

    var hostId = await ObterOuCriarAsync(admin, "host",
        new { filter = new { host } },
        new { host, groups = new[] { new { groupid = grupoId } } },
        "hostid", "hostids");

    var itemPrincipalId = await ConfigurarItemHttpAsync(admin,
        new { hostids = new[] { hostId }, filter = new { key_ = "http.metrics" } },
        new
        {
            hostid = hostId,
            key_ = "http.metrics",
            name = "Métricas HTTP",
            type = 19,      // HTTP agent
            value_type = 4, // Texto
            delay = "15s",
            history = "7d",
            url = Environment.GetEnvironmentVariable("API_METRICS_URL")
                ?? "http://host.docker.internal:5033/metrics",
            status_codes = "200"
        });

    foreach (var metrica in metricas)
    {
        var chave = $"http.metrics.{metrica.Campo}";
        await ObterOuCriarAsync(admin, "item",
            new { hostids = new[] { hostId }, filter = new { key_ = chave } },
            new
            {
                hostid = hostId,
                key_ = chave,
                name = metrica.Titulo,
                type = 18,      // Item dependente
                value_type = 3, // Inteiro sem sinal
                master_itemid = itemPrincipalId,
                preprocessing = new[]
                {
                    new { type = 12, @params = $"$.{metrica.Campo}", error_handler = 0, error_handler_params = "" },
                    new { type = 9, @params = "", error_handler = 0, error_handler_params = "" }
                }
            }, "itemid", "itemids");
    }

    var fila = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "doacoes.recebidas";
    var vhost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/";
    await ConfigurarItemHttpAsync(admin,
        new { hostids = new[] { hostId }, filter = new { key_ = "rabbitmq.doacoes" } },
        new
        {
            hostid = hostId,
            key_ = "rabbitmq.doacoes",
            name = "Fila de doações",
            type = 19,
            value_type = 4,
            delay = "15s",
            history = "7d",
            url = $"http://rabbitmq:15672/api/queues/{Uri.EscapeDataString(vhost)}/{Uri.EscapeDataString(fila)}",
            status_codes = "200",
            authtype = 1,
            username = Ambiente("RABBITMQ_USER"),
            password = Ambiente("RABBITMQ_PASSWORD"),
            preprocessing = new[]
            {
                new
                {
                    type = 21,
                    @params = """
                        var fila = JSON.parse(value);
                        return JSON.stringify({
                            prontas: fila.messages_ready,
                            semConfirmacao: fila.messages_unacknowledged,
                            consumidores: fila.consumers
                        });
                        """,
                    error_handler = 0,
                    error_handler_params = ""
                }
            }
        });
}

async Task<string> ConfigurarItemHttpAsync(string admin, object filtro, object valores)
{
    var encontrados = await ZabbixAsync("item.get", filtro, admin);
    if (encontrados.GetArrayLength() == 0)
    {
        var criado = await ZabbixAsync("item.create", valores, admin);
        return criado.GetProperty("itemids")[0].GetString()!;
    }

    var itemId = encontrados[0].GetProperty("itemid").GetString()!;
    var atualizacao = JsonSerializer.Deserialize<Dictionary<string, object>>(
        JsonSerializer.Serialize(valores))!;
    atualizacao.Remove("hostid");
    atualizacao["itemid"] = itemId;
    await ZabbixAsync("item.update", atualizacao, admin);
    return itemId;
}

async Task ConfigurarPostgresAsync(string senha)
{
    var conexao = new NpgsqlConnectionStringBuilder
    {
        Host = "postgres",
        Database = bancoAplicacao,
        Username = "postgres",
        Password = Ambiente("POSTGRES_PASSWORD")
    };

    await using (var aplicacao = await AbrirAsync(conexao))
    {
        // O nome da role é fixo e a senha contém apenas caracteres hexadecimais.
        await ExecutarSqlAsync(aplicacao, $"""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '{usuarioLeitura}') THEN
                    CREATE ROLE {usuarioLeitura} LOGIN;
                END IF;
            END $$;
            ALTER ROLE {usuarioLeitura} PASSWORD '{senha}';
            """);

        // As funções também podem ser instaladas antes da migration inicial da API.
        await ExecutarSqlAsync(aplicacao, """
            CREATE SCHEMA IF NOT EXISTS grafana_conexao;

            CREATE OR REPLACE FUNCTION grafana_conexao.campanhas()
            RETURNS TABLE (id integer, titulo text, meta numeric, arrecadado numeric)
            LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path = pg_catalog AS $$
            BEGIN
                IF to_regclass('public."Campanhas"') IS NOT NULL THEN
                    RETURN QUERY SELECT "Id", "Titulo"::text, "MetaFinanceira", "ValorTotalArrecadado"
                    FROM public."Campanhas";
                END IF;
            END $$;

            CREATE OR REPLACE FUNCTION grafana_conexao.pendencias()
            RETURNS TABLE (quantidade bigint, idade_segundos numeric)
            LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path = pg_catalog AS $$
            BEGIN
                IF to_regclass('public."Doacoes"') IS NOT NULL THEN
                    RETURN QUERY SELECT count(*), extract(epoch FROM now() - min("DataRecebimento"))
                    FROM public."Doacoes" WHERE "Status" = 1;
                END IF;
            END $$;

            CREATE OR REPLACE FUNCTION grafana_conexao.processadas(inicio timestamptz, fim timestamptz)
            RETURNS TABLE (processada_em timestamptz, valor numeric, duracao_segundos double precision)
            LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path = pg_catalog AS $$
            BEGIN
                IF to_regclass('public."Doacoes"') IS NOT NULL THEN
                    RETURN QUERY SELECT "DataProcessamento", "Valor",
                        extract(epoch FROM "DataProcessamento" - "DataRecebimento")::double precision
                    FROM public."Doacoes"
                    WHERE "Status" = 2 AND "DataProcessamento" BETWEEN inicio AND fim;
                END IF;
            END $$;

            REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA grafana_conexao FROM PUBLIC;
            GRANT USAGE ON SCHEMA grafana_conexao TO grafana_conexao;
            GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA grafana_conexao TO grafana_conexao;
            """);
    }

    conexao.Database = bancoZabbix;
    await using var zabbix = await AbrirAsync(conexao);
    await ExecutarSqlAsync(zabbix, """
        CREATE SCHEMA IF NOT EXISTS grafana_conexao;

        CREATE OR REPLACE VIEW grafana_conexao.amostras WITH (security_barrier = true) AS
        SELECT i.key_ AS metrica, h.clock AS segundo,
               to_timestamp(h.clock + h.ns / 1000000000.0) AS instante,
               h.value::jsonb AS dados
        FROM public.history_text h
        JOIN public.items i ON i.itemid = h.itemid
        JOIN public.hosts host ON host.hostid = i.hostid
        WHERE host.host = 'ConexaoSolidaria API'
          AND i.key_ IN ('http.metrics', 'rabbitmq.doacoes');

        GRANT USAGE ON SCHEMA grafana_conexao TO grafana_conexao;
        GRANT SELECT ON grafana_conexao.amostras TO grafana_conexao;
        """);
}

async Task<NpgsqlConnection> AbrirAsync(NpgsqlConnectionStringBuilder configuracao)
{
    var conexao = new NpgsqlConnection(configuracao.ConnectionString);
    try
    {
        await conexao.OpenAsync();
        return conexao;
    }
    catch
    {
        await conexao.DisposeAsync();
        throw;
    }
}

async Task ExecutarSqlAsync(NpgsqlConnection conexao, string sql)
{
    await using var comando = new NpgsqlCommand(sql, conexao);
    await comando.ExecuteNonQueryAsync();
}

async Task ConfigurarFonteAsync(string uid, string banco, string nome, string senha)
{
    var configuracao = new
    {
        uid,
        type = "postgres",
        name = nome,
        access = "proxy",
        url = "postgres:5432",
        user = usuarioLeitura,
        jsonData = new { database = banco, sslmode = "disable", postgresVersion = 1700 },
        secureJsonData = new { password = senha }
    };

    await SalvarFonteAsync(uid, configuracao);
}

async Task SalvarFonteAsync(string uid, object configuracao)
{
    var rota = $"/api/datasources/uid/{uid}";
    try
    {
        await GrafanaAsync(HttpMethod.Get, rota);
    }
    catch (HttpRequestException erro) when (erro.StatusCode == HttpStatusCode.NotFound)
    {
        await GrafanaAsync(HttpMethod.Post, "/api/datasources", configuracao);
        return;
    }

    await GrafanaAsync(HttpMethod.Put, rota, configuracao);
}

async Task ConfigurarDashboardAsync()
{
    const string amostrasApi = """
        WITH api AS (
            SELECT instante, dados, lag(dados) OVER (ORDER BY instante) AS anterior,
                   extract(epoch FROM instante - lag(instante) OVER (ORDER BY instante)) AS segundos
            FROM grafana_conexao.amostras
            WHERE metrica = 'http.metrics'
              AND segundo BETWEEN $__unixEpochFrom() - 60 AND $__unixEpochTo()
        )
        """;
    const string deltasApi = amostrasApi + """

        , deltas AS (
            SELECT instante,
                   (dados->>'requisicoes')::bigint - CASE
                       WHEN dados->>'instancia' IS NOT DISTINCT FROM anterior->>'instancia'
                            AND (dados->>'requisicoes')::bigint >= (anterior->>'requisicoes')::bigint
                       THEN (anterior->>'requisicoes')::bigint ELSE 0 END AS requisicoes,
                   (dados->>'erros5xx')::bigint - CASE
                       WHEN dados->>'instancia' IS NOT DISTINCT FROM anterior->>'instancia'
                            AND (dados->>'requisicoes')::bigint >= (anterior->>'requisicoes')::bigint
                       THEN (anterior->>'erros5xx')::bigint ELSE 0 END AS erros
            FROM api
            WHERE instante >= $__timeFrom() AND segundos > 0
        )
        """;

    var paineis = new List<object>
    {
        Painel(5, "Requisições HTTP", "timeseries", fonteZabbix, 0, 0, 24, 8,
            deltasApi + """

            SELECT instante AS time, requisicoes AS "Requisições"
            FROM deltas ORDER BY 1
            """, descricao: "Quantidade de requisições HTTP entre as coletas do Zabbix."),
        Painel(7, "Fila Rabbit-MQ", "timeseries", fonteZabbix, 0, 8, 24, 8, """
            SELECT instante AS time, (dados->>'prontas')::int AS "Mensagens prontas",
                   (dados->>'semConfirmacao')::int AS "Aguardando confirmação",
                   (dados->>'consumidores')::int AS "Consumidores"
            FROM grafana_conexao.amostras
            WHERE metrica = 'rabbitmq.doacoes' AND $__unixEpochFilter(segundo)
            ORDER BY 1
            """),
    };

    var linhaErros = 16;
    if (!string.IsNullOrWhiteSpace(prometheusUrl))
    {
        var filtroNamespace = $$"""{namespace="{{namespaceMetricas}}"}""";
        paineis.AddRange([
            PainelPrometheus(10, "Consumo CPU Pods", 0, 16, 12,
                $"1000 * sum by (pod) (rate(pod_cpu_usage_seconds_total{filtroNamespace}[$__rate_interval]))",
                "{{pod}}", "suffix: mCPU", "Uso de CPU por pod em milinúcleos."),
            PainelPrometheus(11, "Consumo Memória Pods", 12, 16, 12,
                $"sum by (pod) (pod_memory_working_set_bytes{filtroNamespace})",
                "{{pod}}", "bytes", "Memória em uso (working set) por pod."),
            PainelPrometheus(12, "Espaço em disco dos volumes persistentes", 0, 24, 24,
                $"max by (persistentvolumeclaim) (100 * kubelet_volume_stats_used_bytes{filtroNamespace} / kubelet_volume_stats_capacity_bytes{filtroNamespace})",
                "{{persistentvolumeclaim}}", "percent", "Percentual ocupado por volume persistente com métricas disponíveis.")
        ]);
        linhaErros = 32;
    }

    paineis.AddRange([
        Painel(6, "Taxa de erros HTTP 5xx", "stat", fonteZabbix, 0, linhaErros, 8, 9,
            deltasApi + """

            SELECT 100 * sum(erros) / nullif(sum(requisicoes), 0) AS "Erros 5xx"
            FROM deltas
            """, unidade: "percent"),
        Painel(9, "Erros HTTP 5xx por endpoint", "table", fonteZabbix, 8, linhaErros, 16, 9,
            amostrasApi + """

            , endpoints AS (
                SELECT atual.metodo, atual.rota,
                       CASE WHEN api.anterior IS NOT NULL THEN
                           atual.erros5xx - CASE
                               WHEN api.dados->>'instancia' = api.anterior->>'instancia'
                               THEN coalesce(anterior.erros5xx, 0) ELSE 0 END
                       END AS erros
                FROM api
                CROSS JOIN LATERAL jsonb_to_recordset(api.dados->'endpoints')
                    AS atual(metodo text, rota text, erros5xx bigint)
                LEFT JOIN LATERAL jsonb_to_recordset(api.anterior->'endpoints')
                    AS anterior(metodo text, rota text, erros5xx bigint)
                    ON anterior.metodo = atual.metodo AND anterior.rota = atual.rota
                WHERE api.instante >= $__timeFrom()
            )
            SELECT metodo AS "Método", rota AS "Rota",
                   sum(erros)::bigint AS "Erros 5xx no período"
            FROM endpoints GROUP BY metodo, rota
            ORDER BY sum(erros) DESC NULLS LAST, metodo, rota
            """,
            descricao: "Total de erros 5xx por método e rota no período selecionado.")
    ]);

    await GrafanaAsync(HttpMethod.Post, "/api/dashboards/db", new
    {
        overwrite = true,
        dashboard = new
        {
            uid = "conexao-solidaria",
            title = "Conexão Solidária",
            schemaVersion = 41,
            timezone = "America/Sao_Paulo",
            refresh = "15s",
            time = new { from = "now-24h", to = "now" },
            panels = paineis
        }
    });
}

object PainelPrometheus(int id, string titulo, int x, int y, int w, string consulta,
    string legenda, string unidade, string descricao) => new
{
    id,
    title = titulo,
    description = descricao,
    type = "timeseries",
    datasource = fontePrometheus,
    gridPos = new { x, y, w, h = 8 },
    fieldConfig = new { defaults = new { unit = unidade, min = 0 } },
    targets = new[]
    {
        new { refId = "A", datasource = fontePrometheus, expr = consulta, legendFormat = legenda, range = true }
    }
};

object Painel(int id, string titulo, string tipo, object fonte, int x, int y, int w, int h,
    string sql, string unidade = "none", string descricao = "") => new
{
    id,
    title = titulo,
    description = descricao,
    type = tipo,
    datasource = fonte,
    gridPos = new { x, y, w, h },
    fieldConfig = new
    {
        defaults = new { unit = unidade }
    },
    targets = new[]
    {
        new { refId = "A", datasource = fonte, rawQuery = true, rawSql = sql,
            format = tipo == "timeseries" ? "time_series" : "table", editorMode = "code" }
    }
};

string Ambiente(string chave) => Environment.GetEnvironmentVariable(chave)
    ?? throw new InvalidOperationException($"Defina {chave}.");

async Task<string> ObterOuCriarAsync(
    string admin, string recurso, object filtro, object valores,
    string campoId, string campoIds)
{
    var encontrados = await ZabbixAsync($"{recurso}.get", filtro, admin);
    if (encontrados.GetArrayLength() > 0)
        return encontrados[0].GetProperty(campoId).GetString()!;

    var criado = await ZabbixAsync($"{recurso}.create", valores, admin);
    return criado.GetProperty(campoIds)[0].GetString()!;
}

async Task<JsonElement> ZabbixAsync(string metodo, object parametros, string? token = null)
{
    var resposta = await EnviarAsync(
        HttpMethod.Post, zabbixUrl,
        new { jsonrpc = "2.0", method = metodo, @params = parametros, id = 1 },
        token is null ? null : new AuthenticationHeaderValue("Bearer", token));

    if (resposta.TryGetProperty("error", out var erro))
        throw new InvalidOperationException($"Zabbix {metodo}: {erro.GetProperty("message").GetString()}");

    return resposta.GetProperty("result");
}

Task<JsonElement> GrafanaAsync(HttpMethod metodo, string caminho, object? corpo = null)
    => EnviarAsync(metodo, grafanaUrl + caminho, corpo,
        new AuthenticationHeaderValue("Basic", credenciaisGrafana));

async Task<JsonElement> EnviarAsync(
    HttpMethod metodo, string url, object? corpo, AuthenticationHeaderValue? autorizacao)
{
    using var requisicao = new HttpRequestMessage(metodo, url);
    requisicao.Headers.Authorization = autorizacao;

    if (corpo is not null)
        requisicao.Content = JsonContent.Create(corpo);

    using var resposta = await http.SendAsync(requisicao);
    resposta.EnsureSuccessStatusCode();
    return await resposta.Content.ReadFromJsonAsync<JsonElement>();
}

async Task AguardarAsync(string servico, Func<Task<JsonElement>> consultar)
{
    var espera = Stopwatch.StartNew();
    while (true)
    {
        try
        {
            await consultar();
            return;
        }
        catch (Exception erro) when (
            erro is HttpRequestException or TaskCanceledException
                or JsonException or InvalidOperationException)
        {
            if (espera.Elapsed >= TimeSpan.FromMinutes(5))
                throw new InvalidOperationException($"{servico} indisponível.", erro);

            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}
