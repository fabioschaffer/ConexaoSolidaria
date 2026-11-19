namespace Domain.Entities;

public sealed class ArrecadacaoCampanha
{
    public ArrecadacaoCampanha(Guid campanhaId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(campanhaId, Guid.Empty);
        CampanhaId = campanhaId;
    }

    public Guid CampanhaId { get; private set; }
    public decimal ValorTotalArrecadado { get; private set; }

    public void AdicionarDoacao(decimal valor)
    {
        if (valor <= 0 || decimal.Round(valor, 2) != valor)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "O valor deve ser positivo e expresso em reais e centavos.");
        }

        ValorTotalArrecadado += valor;
    }
}
