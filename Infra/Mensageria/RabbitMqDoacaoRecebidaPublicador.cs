using System.Globalization;
using System.Text.Json;
using Application.Doacoes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Infra.Mensageria;

public sealed class RabbitMqDoacaoRecebidaPublicador(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqDoacaoRecebidaPublicador> logger) : IDoacaoRecebidaPublicador
{
    public async Task<bool> PublicarAsync(DoacaoRecebidaEvent evento, CancellationToken cancellationToken)
    {
        var configuracao = options.Value;
        var fila = configuracao.FilaDoacoesRecebidas;
        var fabrica = new ConnectionFactory
        {
            HostName = configuracao.HostName,
            Port = configuracao.Port,
            UserName = configuracao.UserName,
            Password = configuracao.Password,
            AutomaticRecoveryEnabled = false
        };

        using var prazo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        prazo.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await using var conexao = await fabrica.CreateConnectionAsync(prazo.Token);
            var confirmacoes = new CreateChannelOptions(
                publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);
            await using var canal = await conexao.CreateChannelAsync(confirmacoes, prazo.Token);
            await canal.QueueDeclareAsync(fila, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: prazo.Token);

            var propriedades = new BasicProperties
            {
                ContentType = "application/json",
                Persistent = true,
                Type = nameof(DoacaoRecebidaEvent),
                MessageId = evento.DoacaoId.ToString(CultureInfo.InvariantCulture)
            };
            var corpo = JsonSerializer.SerializeToUtf8Bytes(evento, JsonSerializerOptions.Web);
            await canal.BasicPublishAsync(exchange: string.Empty, routingKey: fila, mandatory: true,
                basicProperties: propriedades, body: corpo, cancellationToken: prazo.Token);
            return true;
        }
        catch (Exception ex) when (ex is BrokerUnreachableException or OperationInterruptedException
            or PublishException or IOException or TimeoutException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Publicação da doação {DoacaoId} na fila {Fila} sem confirmação.",
                evento.DoacaoId, fila);
            cancellationToken.ThrowIfCancellationRequested();
            return false;
        }
    }
}
