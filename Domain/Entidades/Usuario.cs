using System.Text.RegularExpressions;
using Domain.Enums;

namespace Domain.Entidades;

public sealed class Usuario
{
    private Usuario()
    {
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Cpf { get; private set; } = null!;
    public string Senha { get; private set; } = null!;
    public PerfilUsuario Perfil { get; private set; }

    public static Usuario CadastrarDoador(string nome, string email, string cpf, string senha)
        => Criar(nome, email, cpf, senha, PerfilUsuario.Doador);

    public static Usuario CriarGestor(string nome, string email, string cpf, string senha)
        => Criar(nome, email, cpf, senha, PerfilUsuario.GestorONG);

    private static Usuario Criar(
        string nome, string email, string cpf, string senha, PerfilUsuario perfil)
    {
        var usuario = new Usuario { Id = Guid.NewGuid() };
        usuario.Editar(nome, email, cpf, perfil, senha);
        return usuario;
    }

    public void Editar(string nome, string email, string cpf, PerfilUsuario perfil, string? senha = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(cpf);
        ArgumentException.ThrowIfNullOrWhiteSpace(senha ?? Senha);

        email = email.Trim();
        if (!System.Net.Mail.MailAddress.TryCreate(email, out var endereco) || endereco.Address != email)
        {
            throw new ArgumentException("O e-mail informado é inválido.", nameof(email));
        }

        if (!Enum.IsDefined(perfil))
        {
            throw new ArgumentOutOfRangeException(nameof(perfil), "Perfil de usuário inválido.");
        }

        cpf = cpf.Trim();
        if (!Regex.IsMatch(cpf, @"\A(?:[0-9]{11}|[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2})\z"))
        {
            throw new ArgumentException("O CPF deve ter onze dígitos ou usar o formato 000.000.000-00.", nameof(cpf));
        }

        Nome = nome.Trim();
        Email = email.ToLowerInvariant();
        Cpf = cpf.Replace(".", "").Replace("-", "");
        Senha = senha ?? Senha;
        Perfil = perfil;
    }
}
