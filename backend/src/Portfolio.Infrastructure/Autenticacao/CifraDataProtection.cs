using Microsoft.AspNetCore.DataProtection;
using Portfolio.Core.Autenticacao;

namespace Portfolio.Infrastructure.Autenticacao;

// Cifra o segredo do 2FA com a Data Protection API (SEG09). As chaves ficam fora da base de
// dados (pasta configurada na API), por isso uma cópia da base de dados não chega para gerar códigos.
// Se as chaves se perderem, o segredo deixa de se conseguir ler: repor-password desativa o 2FA.
public sealed class CifraDataProtection(IDataProtectionProvider fornecedor) : ICifraSegredos
{
    private readonly IDataProtector _protetor = fornecedor.CreateProtector("Portfolio.Autenticacao.Totp.v1");

    public string Cifrar(string texto) => _protetor.Protect(texto);

    public string Decifrar(string cifrado) => _protetor.Unprotect(cifrado);
}
