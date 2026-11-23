#:property PublishAot=false

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

const string zabbixUrl = "http://zabbix-web:8080/api_jsonrpc.php";
const string grafanaUrl = "http://grafana:3000";
const string grupo = "ConexaoSolidaria";
const string host = "ConexaoSolidaria API";
const string grupoGrafana = "Grafana ConexaoSolidaria";
const string usuarioGrafana = "grafana-conexao";
const string plugin = "alexanderzobnin-zabbix-app";

var fonte = new { type = "alexanderzobnin-zabbix-datasource", uid = "zabbix-conexao" };
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
await AguardarAsync("Plugin Zabbix no Grafana",
    () => GrafanaAsync(HttpMethod.Post, $"/api/plugins/{plugin}/settings", new { enabled = true }));

var sessao = (await ZabbixAsync("user.login", new { username = "Admin", password = "zabbix" }))
    .GetString()!;
var token = await ConfigurarZabbixAsync(sessao);
await ZabbixAsync("user.logout", Array.Empty<object>(), sessao);
await ConfigurarGrafanaAsync(token);

Console.WriteLine("Coleta Zabbix e dashboard Conexão Solidária configurados.");

async Task<string> ConfigurarZabbixAsync(string admin)
{
    var grupoId = await ObterOuCriarAsync(admin, "hostgroup",
        new { filter = new { name = grupo } },
        new { name = grupo }, "groupid", "groupids");

    var hostId = await ObterOuCriarAsync(admin, "host",
        new { filter = new { host } },
        new { host, groups = new[] { new { groupid = grupoId } } },
        "hostid", "hostids");

    var itemPrincipalId = await ObterOuCriarAsync(admin, "item",
        new { hostids = new[] { hostId }, filter = new { key_ = "http.metrics" } },
        new
        {
            hostid = hostId,
            key_ = "http.metrics",
            name = "Métricas HTTP",
            type = 19,      // HTTP agent
            value_type = 4, // Texto
            delay = "15s",
            history = "0",
            url = "http://host.docker.internal:5033/metrics",
            status_codes = "200"
        }, "itemid", "itemids");

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

    var grupoGrafanaId = await ObterOuCriarAsync(admin, "usergroup",
        new { filter = new { name = grupoGrafana } },
        new
        {
            name = grupoGrafana,
            hostgroup_rights = new[] { new { id = grupoId, permission = 2 } }
        }, "usrgrpid", "usrgrpids");

    var perfis = await ZabbixAsync("role.get", new
    {
        output = new[] { "roleid" },
        filter = new { type = 1 }
    }, admin);

    if (perfis.GetArrayLength() == 0)
        throw new InvalidOperationException("Perfil de usuário do Zabbix indisponível.");

    var usuarioId = await ObterOuCriarAsync(admin, "user",
        new { filter = new { username = usuarioGrafana } },
        new
        {
            username = usuarioGrafana,
            passwd = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)) + "a!",
            roleid = perfis[0].GetProperty("roleid").GetString(),
            usrgrps = new[] { new { usrgrpid = grupoGrafanaId } }
        }, "userid", "userids");

    var tokenId = await ObterOuCriarAsync(admin, "token",
        new { userids = new[] { usuarioId }, filter = new { name = grupoGrafana } },
        new { name = grupoGrafana, userid = usuarioId },
        "tokenid", "tokenids");

    var tokens = await ZabbixAsync("token.generate", new[] { tokenId }, admin);
    return tokens[0].GetProperty("token").GetString()!;
}

async Task ConfigurarGrafanaAsync(string token)
{
    var configuracao = new
    {
        fonte.uid,
        fonte.type,
        name = "Zabbix ConexaoSolidaria",
        access = "proxy",
        url = zabbixUrl,
        jsonData = new { authType = "token", trends = false },
        secureJsonData = new { apiToken = token }
    };

    var rotaFonte = $"/api/datasources/uid/{fonte.uid}";
    var fonteExiste = true;
    try
    {
        await GrafanaAsync(HttpMethod.Get, rotaFonte);
    }
    catch (HttpRequestException erro) when (erro.StatusCode == HttpStatusCode.NotFound)
    {
        fonteExiste = false;
    }

    await (fonteExiste
        ? GrafanaAsync(HttpMethod.Put, rotaFonte, configuracao)
        : GrafanaAsync(HttpMethod.Post, "/api/datasources", configuracao));

    var paineis = metricas.Select((metrica, indice) => new
    {
        id = indice + 1,
        title = metrica.Titulo,
        type = "timeseries",
        datasource = fonte,
        gridPos = new { x = indice * 12, y = 0, w = 12, h = 8 },
        targets = new[]
        {
            new
            {
                refId = "A",
                datasource = fonte,
                queryType = "0",
                group = new { filter = grupo },
                host = new { filter = host },
                item = new { filter = metrica.Titulo },
                functions = Array.Empty<object>(),
                options = new { }
            }
        }
    }).ToArray();

    await GrafanaAsync(HttpMethod.Post, "/api/dashboards/db", new
    {
        overwrite = true,
        dashboard = new
        {
            uid = "conexao-solidaria",
            title = "Conexão Solidária",
            schemaVersion = 41,
            refresh = "15s",
            time = new { from = "now-15m", to = "now" },
            panels = paineis
        }
    });
}

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
