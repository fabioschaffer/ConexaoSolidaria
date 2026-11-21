using System.Text.RegularExpressions;
using Domain.Enums;

namespace Domain.Entidades;

public sealed class Usuario
{
    private Usuario()
    {
    }

    public Guid Id { get; private set; }
    public string NomeCompleto { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Cpf { get; private set; } = null!;
    public string Senha { get; private set; } = null!;
    public PerfilUsuario Perfil { get; private set; }

    public static Usuario CadastrarDoador(string nomeCompleto, string email, string cpf, string senha)
        => Criar(nomeCompleto, email, cpf, senha, PerfilUsuario.Doador);

    public static Usuario CriarGestor(string nomeCompleto, string email, string cpf, string senha)
        => Criar(nomeCompleto, email, cpf, senha, PerfilUsuario.GestorONG);

    private static Usuario Criar(
        string nomeCompleto, string email, string cpf, string senha, PerfilUsuario perfil)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeCompleto);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(cpf);
        ArgumentException.ThrowIfNullOrWhiteSpace(senha);

        cpf = cpf.Trim();
        if (!Regex.IsMatch(cpf, @"\A(?:[0-9]{11}|[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2})\z"))
        {
            throw new ArgumentException("O CPF deve ter onze dígitos ou usar o formato 000.000.000-00.", nameof(cpf));
        }

        return new Usuario
        {
            Id = Guid.NewGuid(),
            NomeCompleto = nomeCompleto.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Cpf = cpf.Replace(".", "").Replace("-", ""),
            Senha = senha,
            Perfil = perfil
        };
    }
}
