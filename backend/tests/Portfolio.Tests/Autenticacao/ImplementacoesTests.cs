using Microsoft.AspNetCore.DataProtection;
using OtpNet;
using Portfolio.Infrastructure.Autenticacao;

namespace Portfolio.Tests.Autenticacao;

// Implementações da Infrastructure, sem base de dados.
public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact]
    public void Gera_argon2id_em_formato_phc_com_salt_diferente_em_cada_hash()
    {
        var a = _hasher.Hash("password-de-teste");
        var b = _hasher.Hash("password-de-teste");

        Assert.StartsWith("$argon2id$v=19$m=19456,t=2,p=1$", a);
        Assert.NotEqual(a, b);
        Assert.True(a.Length <= 255);
    }

    [Fact]
    public void Verifica_a_password_certa_e_recusa_a_errada()
    {
        var hash = _hasher.Hash("password-de-teste");

        Assert.True(_hasher.Verificar("password-de-teste", hash));
        Assert.False(_hasher.Verificar("password-de-Teste", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("texto qualquer")]
    [InlineData("$argon2i$v=19$m=19456,t=2,p=1$c2FsdHNhbHQ$aGFzaA")]
    [InlineData("$argon2id$v=19$m=abc,t=2,p=1$c2FsdHNhbHQ$aGFzaA")]
    [InlineData("$argon2id$v=19$m=19456,t=2,p=1$não-é-base64$aGFzaA")]
    public void Um_hash_mal_formado_nunca_e_aceite(string hash)
    {
        Assert.False(_hasher.Verificar("password-de-teste", hash));
    }
}

public class TotpOtpNetTests
{
    private readonly TotpOtpNet _totp = new();

    [Fact]
    public void Aceita_o_codigo_atual_e_devolve_o_intervalo()
    {
        var segredo = _totp.GerarSegredo();
        var agora = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
        var codigo = new Totp(Base32Encoding.ToBytes(segredo)).ComputeTotp(agora.UtcDateTime);

        Assert.Equal(agora.ToUnixTimeSeconds() / 30, _totp.Validar(segredo, codigo, agora));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("12345a")]
    [InlineData("1234567")]
    public void Recusa_codigos_que_nao_tem_6_digitos(string codigo)
    {
        Assert.Null(_totp.Validar(_totp.GerarSegredo(), codigo, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Recusa_um_codigo_de_ha_mais_de_um_intervalo()
    {
        var segredo = _totp.GerarSegredo();
        var agora = DateTimeOffset.UtcNow;
        var antigo = new Totp(Base32Encoding.ToBytes(segredo)).ComputeTotp(agora.AddMinutes(-2).UtcDateTime);

        Assert.Null(_totp.Validar(segredo, antigo, agora));
    }

    [Fact]
    public void Gera_o_uri_otpauth_e_o_qr_code_em_png()
    {
        var configuracao = _totp.Configuracao("JBSWY3DPEHPK3PXP", "admin@exemplo.pt");

        Assert.StartsWith("otpauth://totp/", configuracao.Uri);
        Assert.Contains("secret=JBSWY3DPEHPK3PXP", configuracao.Uri);
        Assert.StartsWith("data:image/png;base64,", configuracao.QrCode);
    }
}

public class CifraDataProtectionTests
{
    [Fact]
    public void O_segredo_cifrado_nao_contem_o_original_e_volta_a_ler_se()
    {
        var cifra = new CifraDataProtection(new EphemeralDataProtectionProvider());

        var cifrado = cifra.Cifrar("JBSWY3DPEHPK3PXP");

        Assert.DoesNotContain("JBSWY3DPEHPK3PXP", cifrado);
        Assert.Equal("JBSWY3DPEHPK3PXP", cifra.Decifrar(cifrado));
        Assert.True(cifrado.Length <= 500);
    }
}
