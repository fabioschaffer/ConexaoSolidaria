namespace Domain.Entidades;

public sealed class DoacaoProcessada
{
    public DoacaoProcessada(Guid doacaoId, DateTimeOffset dataProcessamento)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(doacaoId, Guid.Empty);
        DoacaoId = doacaoId;
        DataProcessamento = dataProcessamento.ToUniversalTime();
    }

    public Guid DoacaoId { get; private set; }
    public DateTimeOffset DataProcessamento { get; private set; }
}
