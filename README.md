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

## Recriar o banco local com IDs inteiros

Se o banco local ainda usa IDs UUID, recrie-o com a migration inicial. O comando `drop` apaga o banco e todos os seus dados. Verifique se a conexão aponta para o PostgreSQL local antes de executar.

```powershell
docker compose up -d --wait
dotnet ef database drop --force --project Infra --startup-project Api -- --environment Development
dotnet ef database update --project Infra --startup-project Api -- --environment Development
```
