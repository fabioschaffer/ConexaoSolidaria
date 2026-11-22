namespace Application.Doacoes;

public sealed class ProcessarDoacao(IDoacaoRepositorio repositorio)
{
    public Task<bool> ExecutarAsync(int doacaoId, CancellationToken cancellationToken)
        => repositorio.ProcessarAsync(doacaoId, cancellationToken);
}
