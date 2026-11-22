namespace Application.Doacoes;

public sealed record DoacaoRecebidaEvent(
    int DoacaoId, int CampanhaId, decimal Valor, DateTimeOffset DataRecebimento);

public interface IDoacaoRecebidaPublicador
{
    Task<bool> PublicarAsync(DoacaoRecebidaEvent evento, CancellationToken cancellationToken);
}
