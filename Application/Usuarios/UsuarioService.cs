using Domain.Entidades;
using Domain.Enums;

namespace Application.Usuarios;

public sealed class UsuarioService(IUsuarioRepositorio repositorio, IHashSenha hashSenha)
{
    public async Task<(ResultadoAlteracaoUsuario Resultado, UsuarioDto? Usuario)> CriarAsync(
        CriarUsuarioDados dados, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dados.Senha);
        var senha = hashSenha.Gerar(dados.Senha);
        var usuario = dados.Perfil switch
        {
            PerfilUsuario.Doador => Usuario.CadastrarDoador(dados.Nome, dados.Email, dados.Cpf, senha),
            PerfilUsuario.GestorONG => Usuario.CriarGestor(dados.Nome, dados.Email, dados.Cpf, senha),
            _ => throw new ArgumentOutOfRangeException(nameof(dados.Perfil), "Perfil de usuário inválido.")
        };

        repositorio.Adicionar(usuario);
        var resultado = await repositorio.SalvarAsync(cancellationToken);
        return (resultado, resultado == ResultadoAlteracaoUsuario.Sucesso ? UsuarioDto.De(usuario) : null);
    }

    public async Task<UsuarioDto?> ObterAsync(int id, CancellationToken cancellationToken)
    {
        var usuario = await repositorio.ObterAsync(id, cancellationToken);
        return usuario is null ? null : UsuarioDto.De(usuario);
    }

    public Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken cancellationToken)
        => repositorio.ListarAsync(cancellationToken);

    public async Task<ResultadoAlteracaoUsuario> AtualizarAsync(
        int id, AtualizarUsuarioDados dados, CancellationToken cancellationToken)
    {
        var usuario = await repositorio.ObterAsync(id, cancellationToken);
        if (usuario is null)
        {
            return ResultadoAlteracaoUsuario.NaoEncontrado;
        }

        string? senha = null;
        if (dados.Senha is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(dados.Senha);
            senha = hashSenha.Gerar(dados.Senha);
        }

        usuario.Editar(dados.Nome, dados.Email, dados.Cpf, dados.Perfil, senha);
        return await repositorio.SalvarAsync(cancellationToken);
    }

    public async Task<ResultadoAlteracaoUsuario> RemoverAsync(int id, CancellationToken cancellationToken)
    {
        var usuario = await repositorio.ObterAsync(id, cancellationToken);
        if (usuario is null)
        {
            return ResultadoAlteracaoUsuario.NaoEncontrado;
        }

        repositorio.Remover(usuario);
        return await repositorio.SalvarAsync(cancellationToken);
    }
}
