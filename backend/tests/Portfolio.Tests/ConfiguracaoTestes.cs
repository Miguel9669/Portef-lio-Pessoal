using System.Runtime.CompilerServices;

namespace Portfolio.Tests;

internal static class ConfiguracaoTestes
{
    // Cada API criada nos testes ficava a observar o appsettings.json para recarregar a
    // configuração, e o Linux limita esses observadores (inotify, 128 por utilizador). Com muitas
    // APIs nos testes, e o Vite e o Next.js a correr ao mesmo tempo, o limite esgotava-se.
    // Nos testes a configuração nunca muda a meio, por isso o recarregamento fica desligado.
    [ModuleInitializer]
    internal static void Iniciar() => Environment.SetEnvironmentVariable("DOTNET_hostBuilder__reloadConfigOnChange", "false");
}
