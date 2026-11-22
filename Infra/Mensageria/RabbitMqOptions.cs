namespace Infra.Mensageria;

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FilaDoacoesRecebidas { get; set; } = "doacoes.recebidas";
}
