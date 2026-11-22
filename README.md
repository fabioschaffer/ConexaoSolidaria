migration:
executar comando abaixo na pasta raíz do projeto:
 dotnet ef database update --project Infra --startup-project Api

criar os containers docker:
executar comando abaixo na pasta raíz do projeto
docker compose up -d --wait

usuário admin:
email: admin@admin.com
senha: 1234

rabbit-MQ:
url: http://localhost:15672/
Usuário: conexaosolidaria
Senha: conexaosolidaria-local