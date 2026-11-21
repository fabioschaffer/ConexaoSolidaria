# ConexaoSolidaria

## Executar a API de campanhas

Com Docker, o SDK .NET 10 e a ferramenta `dotnet-ef` 10.x disponíveis, execute os comandos abaixo na raiz da solução. A API usa a conexão local definida em `Api/appsettings.Development.json`.

```powershell
docker compose up -d --wait
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project Infra --startup-project Api
dotnet run --project Api
```

Use [Api.http](Api/Api.http) para executar as requisições. O `DELETE` cancela a campanha e mantém o registro disponível para consulta.
