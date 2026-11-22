# ConexaoSolidaria

## Executar a API de campanhas

Com Docker, o SDK .NET 10 e a ferramenta `dotnet-ef` 10.x disponíveis, execute os comandos abaixo na raiz da solução. A API usa a conexão local definida em `Api/appsettings.Development.json`.

```powershell
docker compose up -d --wait
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Jwt__Chave = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet ef database update --project Infra --startup-project Api
dotnet run --project Api
```

Use [Api.http](Api/Api.http) para executar as requisições. O `DELETE` cancela a campanha e mantém o registro disponível para consulta.

## Autenticação JWT

Configure `Jwt__Chave` na API com pelo menos 32 bytes aleatórios codificados em Base64. Mantenha a chave em um gerenciador de segredos no ambiente de implantação e sirva a API via HTTPS. Os parâmetros opcionais `Jwt__Emissor` e `Jwt__Audiencia` têm os valores padrão `ConexaoSolidaria` e `ConexaoSolidaria.Api`.

1. Cadastre um doador em `POST /api/criar-usuario` ou use uma conta existente.
2. Envie `email` e `senha` a `POST /api/autenticacao/login`. A resposta contém `accessToken`, `tokenType` e `expiresIn` (28.800 segundos, ou 8 horas).
3. Envie `Authorization: Bearer <accessToken>` nas rotas protegidas. No Swagger, use **Authorize** e cole o token.

O cadastro público recebe nome, e-mail, CPF e senha e atribui o perfil `Doador` automaticamente. Todas as operações em `/api/usuarios` exigem um JWT com o perfil `GestorONG`. O gestor pode cadastrar, listar, consultar, editar e excluir usuários. A edição preserva o perfil existente.

Todas as operações em `/api/campanhas` exigem um JWT com o perfil `GestorONG`. Para executar os exemplos de gestão em `Api.http`, informe as credenciais de um gestor existente na requisição de login. O endpoint público `GET /api/transparencia/campanhas` lista as campanhas ativas com título, meta financeira e o total das doações processadas.

## Recriar o banco local com IDs inteiros

Se o banco local ainda usa IDs UUID, recrie-o com a migration inicial. O comando `drop` apaga o banco e todos os seus dados. Verifique se a conexão aponta para o PostgreSQL local antes de executar.

```powershell
docker compose up -d --wait
dotnet ef database drop --force --project Infra --startup-project Api -- --environment Development
dotnet ef database update --project Infra --startup-project Api -- --environment Development
```
