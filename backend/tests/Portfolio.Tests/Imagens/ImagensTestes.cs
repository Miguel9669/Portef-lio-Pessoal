using Microsoft.Extensions.Hosting;
using Portfolio.Core.Imagens;
using Portfolio.Infrastructure.Imagens;
using SkiaSharp;

namespace Portfolio.Tests.Imagens;

// Imagens de teste geradas na hora, sem ficheiros no repositório.
public static class ImagensDeTeste
{
    public static byte[] Criar(SKEncodedImageFormat formato, int largura = 20, int altura = 10)
    {
        using var bitmap = new SKBitmap(largura, altura);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var imagem = SKImage.FromBitmap(bitmap);
        return imagem.Encode(formato, 90).ToArray();
    }

    // JPEG com um bloco EXIF a dizer "rodar 90°" (orientação 6), como as fotos de telemóvel,
    // e um texto que faz de dado privado (por exemplo coordenadas GPS).
    public static byte[] JpegComExif(int largura = 20, int altura = 10)
    {
        var jpeg = Criar(SKEncodedImageFormat.Jpeg, largura, altura);
        byte[] tiff =
        [
            0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00, // TIFF little-endian, IFD no byte 8
            0x01, 0x00,                                     // 1 entrada
            0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, // Orientation, SHORT, 1 valor
            0x06, 0x00, 0x00, 0x00,                         // = 6 (rodar 90° no sentido do relógio)
            0x00, 0x00, 0x00, 0x00,                         // sem mais IFDs
            .. "GPS-SECRETO"u8,
        ];
        byte[] cabecalho = [.. "Exif"u8, 0, 0];
        var tamanho = 2 + cabecalho.Length + tiff.Length;
        byte[] app1 = [0xFF, 0xE1, (byte)(tamanho >> 8), (byte)tamanho, .. cabecalho, .. tiff];
        return [.. jpeg[..2], .. app1, .. jpeg[2..]];
    }
}

public class ProcessadorSkiaTests
{
    private readonly ProcessadorSkia _processador = new(new OpcoesImagens());

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg, ".jpg", "image/jpeg")]
    [InlineData(SKEncodedImageFormat.Png, ".png", "image/png")]
    [InlineData(SKEncodedImageFormat.Webp, ".webp", "image/webp")]
    public void Aceita_jpeg_png_e_webp_pelo_conteudo(SKEncodedImageFormat formato, string extensao, string tipo)
    {
        var resultado = _processador.Processar(new MemoryStream(ImagensDeTeste.Criar(formato)));

        Assert.NotNull(resultado);
        Assert.Equal(extensao, resultado.Extensao);
        Assert.Equal(tipo, resultado.TipoMime);
        using var lida = SKBitmap.Decode(resultado.Conteudo);
        Assert.Equal((20, 10), (lida.Width, lida.Height));
    }

    [Fact]
    public void Remove_o_exif_e_aplica_a_orientacao()
    {
        var original = ImagensDeTeste.JpegComExif(largura: 20, altura: 10);
        Assert.True(original.AsSpan().IndexOf("GPS-SECRETO"u8) >= 0);

        var resultado = _processador.Processar(new MemoryStream(original))!;

        Assert.True(resultado.Conteudo.AsSpan().IndexOf("Exif"u8) < 0);
        Assert.True(resultado.Conteudo.AsSpan().IndexOf("GPS-SECRETO"u8) < 0);
        // Orientação 6: a foto 20x10 fica de pé, 10x20.
        using var lida = SKBitmap.Decode(resultado.Conteudo);
        Assert.Equal((10, 20), (lida.Width, lida.Height));
    }

    [Theory]
    [InlineData("isto é texto com extensão .jpg")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>")]
    [InlineData("GIF89a")]
    [InlineData("")]
    public void Recusa_o_que_nao_e_uma_imagem_aceite(string conteudo)
    {
        Assert.Null(_processador.Processar(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(conteudo))));
    }

    [Fact]
    public void Recusa_um_jpeg_cortado_a_meio()
    {
        var jpeg = ImagensDeTeste.Criar(SKEncodedImageFormat.Jpeg, 200, 200);

        Assert.Null(_processador.Processar(new MemoryStream(jpeg[..40])));
    }

    [Fact]
    public void Recusa_imagens_com_dimensoes_exageradas()
    {
        var processador = new ProcessadorSkia(new OpcoesImagens { MaximoMegapixeis = 1 });

        Assert.Null(processador.Processar(new MemoryStream(ImagensDeTeste.Criar(SKEncodedImageFormat.Png, 2000, 1000))));
    }
}

public sealed class ArmazenamentoDiscoLocalTests : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "portfolio-testes", Guid.NewGuid().ToString("N"));
    private readonly ArmazenamentoDiscoLocal _armazenamento;

    public ArmazenamentoDiscoLocalTests()
    {
        _armazenamento = new ArmazenamentoDiscoLocal(new OpcoesImagens { Pasta = _pasta }, new Ambiente());
    }

    public void Dispose() => Directory.Delete(_pasta, recursive: true);

    [Fact]
    public async Task Guarda_abre_e_apaga()
    {
        const string ficheiro = "0123456789abcdef0123456789abcdef.png";

        await _armazenamento.GuardarAsync(ficheiro, [1, 2, 3], TestContext.Current.CancellationToken);
        await using (var aberto = _armazenamento.Abrir(ficheiro))
            Assert.Equal(3, aberto!.Length);
        _armazenamento.Apagar(ficheiro);

        Assert.Null(_armazenamento.Abrir(ficheiro));
        Assert.Empty(Directory.GetFiles(_pasta));
    }

    [Fact]
    public async Task Volta_a_criar_a_pasta_se_tiver_sido_apagada()
    {
        Directory.Delete(_pasta, recursive: true);

        await _armazenamento.GuardarAsync("0123456789abcdef0123456789abcdef.webp", [1], TestContext.Current.CancellationToken);

        Assert.Single(Directory.GetFiles(_pasta));
    }

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("/etc/passwd")]
    [InlineData("..\\0123456789abcdef0123456789abcdef.png")]
    [InlineData("0123456789abcdef0123456789abcdef.svg")]
    [InlineData("foto.jpg")]
    public void So_aceita_nomes_gerados_pela_aplicacao(string ficheiro)
    {
        Assert.Throws<ArgumentException>(() => _armazenamento.Abrir(ficheiro));
    }

    private sealed class Ambiente : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testes";
        public string ApplicationName { get; set; } = "Portfolio";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
