using Domain.Entidades;

namespace Application.Usuarios;

public interface IUsuarioRepositorio
{
    void Adicionar(Usuario usuario);
    Task<Usuario?> ObterAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken cancellationToken);
    void Remover(Usuario usuario);
    Task<ResultadoAlteracaoUsuario> SalvarAsync(CancellationToken cancellationToken);
}

public enum ResultadoAlteracaoUsuario
{
    Sucesso,
    NaoEncontrado,
    EmailDuplicado,
    CpfDuplicado,
    Conflito,
    PossuiDoacoes
}
