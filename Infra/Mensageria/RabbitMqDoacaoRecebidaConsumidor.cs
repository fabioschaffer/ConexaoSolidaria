using System.Text.Json;
using Application.Doacoes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infra.Mensageria;

public sealed class RabbitMqDoacaoRecebidaConsumidor(
    IOptions<RabbitMqOptions> options,
    IServiceScopeFactory escopos,
    ILogger<RabbitMqDoacaoRecebidaConsumidor> logger) : BackgroundService
{
    private static readonly TimeSpan IntervaloNovaTentativa = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuracao = options.Value;
        var fabrica = new ConnectionFactory
        {
            HostName = configuracao.HostName,
            Port = configuracao.Port,
            UserName = configuracao.UserName,
            Password = configuracao.Password,
            AutomaticRecoveryEnabled = false
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumirAsync(fabrica, configuracao.FilaDoacoesRecebidas, stoppingToken);
                logger.LogWarning("Consumo da fila {Fila} encerrado. Reconectando.", configuracao.FilaDoacoesRecebidas);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Falha no consumo da fila {Fila}. Reconectando.", configuracao.FilaDoacoesRecebidas);
            }

            await Task.Delay(IntervaloNovaTentativa, stoppingToken);
        }
    }

    private async Task ConsumirAsync(ConnectionFactory fabrica, string fila, CancellationToken cancellationToken)
    {
        await using var conexao = await fabrica.CreateConnectionAsync(cancellationToken);
        await using var canal = await conexao.CreateChannelAsync(cancellationToken: cancellationToken);
        var encerramento = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        canal.ChannelShutdownAsync += (_, _) =>
        {
            encerramento.TrySetResult();
            return Task.CompletedTask;
        };

        await canal.QueueDeclareAsync(fila, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: cancellationToken);
        await canal.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false,
            cancellationToken: cancellationToken);

        var consumidor = new AsyncEventingBasicConsumer(canal);
        consumidor.UnregisteredAsync += (_, _) =>
        {
            encerramento.TrySetResult();
            return Task.CompletedTask;
        };
        consumidor.ReceivedAsync += async (_, entrega) =>
        {
            try
            {
                await ProcessarEntregaAsync(canal, entrega, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // O encerramento do canal devolve ao broker entregas ainda não confirmadas.
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao confirmar a entrega {Entrega}.", entrega.DeliveryTag);
                encerramento.TrySetResult();
            }
        };

        await canal.BasicConsumeAsync(fila, autoAck: false, consumidor, cancellationToken);
        logger.LogInformation("Consumindo a fila {Fila}.", fila);
        await encerramento.Task.WaitAsync(cancellationToken);
    }

    private async Task ProcessarEntregaAsync(
        IChannel canal, BasicDeliverEventArgs entrega, CancellationToken cancellationToken)
    {
        DoacaoRecebidaEvent? evento = null;
        try
        {
            evento = JsonSerializer.Deserialize<DoacaoRecebidaEvent>(entrega.Body.Span, JsonSerializerOptions.Web);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Mensagem de doação com JSON inválido. MessageId: {MessageId}",
                entrega.BasicProperties.MessageId);
        }

        if (evento is null || evento.DoacaoId <= 0)
        {
            logger.LogWarning("Mensagem de doação sem identificador válido. MessageId: {MessageId}",
                entrega.BasicProperties.MessageId);
            await canal.BasicRejectAsync(entrega.DeliveryTag, requeue: false, cancellationToken);
            return;
        }

        bool processada;
        try
        {
            await using var escopo = escopos.CreateAsyncScope();
            var processamento = escopo.ServiceProvider.GetRequiredService<ProcessarDoacao>();
            processada = await processamento.ExecutarAsync(evento.DoacaoId, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Falha ao processar a doação {DoacaoId}. A mensagem será reenfileirada.",
                evento.DoacaoId);
            await Task.Delay(IntervaloNovaTentativa, cancellationToken);
            await canal.BasicNackAsync(entrega.DeliveryTag, multiple: false, requeue: true, cancellationToken);
            return;
        }

        if (!processada)
        {
            logger.LogWarning("Doação {DoacaoId} não encontrada para processamento.", evento.DoacaoId);
            await canal.BasicRejectAsync(entrega.DeliveryTag, requeue: false, cancellationToken);
            return;
        }

        await canal.BasicAckAsync(entrega.DeliveryTag, multiple: false, cancellationToken);
        logger.LogInformation("Processamento confirmado para a doação {DoacaoId}.", evento.DoacaoId);
    }
}
