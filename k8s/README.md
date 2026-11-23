# Conexão Solidária no Kubernetes

## Exclusão (se já existente)

```powershell
kubectl --context docker-desktop delete namespace conexaosolidaria
```

## Implantação

1. Habilite o Kubernetes no Docker Desktop.

2. Crie as imagens da API e do Worker

```powershell
docker build -f Api/Dockerfile -t conexaosolidaria-api:local .
docker build -f Worker/Dockerfile -t conexaosolidaria-worker:local .
```

3. Execute o comando abaixo para criar os recursos no cluster Kubernetes.

```powershell
kubectl --context docker-desktop apply -k k8s
```

## Acesso local

Execute cada port-forward em um terminal e mantenha-o aberto enquanto usar a interface.

| Interface | Comando | Endereço |
| --- | --- | --- |
| API (Swagger) | `kubectl --context docker-desktop -n conexaosolidaria port-forward svc/api 8080:8080` | http://localhost:8080/swagger |
| Grafana | `kubectl --context docker-desktop -n conexaosolidaria port-forward svc/grafana 3000:3000` | http://localhost:3000 |
| Zabbix | `kubectl --context docker-desktop -n conexaosolidaria port-forward svc/zabbix-web 8081:8080` | http://localhost:8081 |
| RabbitMQ | `kubectl --context docker-desktop -n conexaosolidaria port-forward svc/rabbitmq 15672:15672` | http://localhost:15672 |

## Testes

Pausar o worker:
```powershell
kubectl --context docker-desktop -n conexaosolidaria scale deployment/worker --replicas=0
```

Reiniciar o worker:
```powershell
kubectl --context docker-desktop -n conexaosolidaria scale deployment/worker --replicas=1
```