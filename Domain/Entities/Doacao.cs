using Domain.Enums;

namespace Domain.Entities;

public sealed class Doacao
{
    private Doacao()
    {
    }

    public Guid Id { get; private set; }
    public Guid CampanhaId { get; private set; }
    public Guid DoadorId { get; private set; }
    public decimal Valor { get; private set; }
    public DateTimeOffset DataRecebimento { get; private set; }

    public static Doacao Registrar(Campanha campanha, Usuario doador, decimal valor, DateTimeOffset dataRecebimento)
    {
        ArgumentNullException.ThrowIfNull(campanha);
        ArgumentNullException.ThrowIfNull(doador);

        if (valor <= 0 || decimal.Round(valor, 2) != valor)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "O valor deve ser positivo e expresso em reais e centavos.");
        }

        if (doador.Perfil != PerfilUsuario.Doador)
        {
            throw new InvalidOperationException("Apenas usuários com perfil Doador podem registrar doações.");
        }

        if (!campanha.AceitaDoacaoEm(dataRecebimento))
        {
            throw new InvalidOperationException("A campanha não aceita doações na data de recebimento.");
        }

        return new Doacao
        {
            Id = Guid.NewGuid(),
            CampanhaId = campanha.Id,
            DoadorId = doador.Id,
            Valor = valor,
            DataRecebimento = dataRecebimento.ToUniversalTime()
        };
    }
}
