using System.Net.Sockets;
using KnockKnockServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Så æ, ø og å vises korrekt i Windows-konsollen
Console.OutputEncoding = System.Text.Encoding.UTF8;

IHost? host = null;
try
{
    host = BuildHost(args);
    // ValidateOnStart() gør, at ugyldig konfiguration opdages her - ikke først når en klient forbinder
    await host.StartAsync();
}
catch (Exception ex) when (ex is OptionsValidationException or InvalidOperationException or InvalidDataException)
{
    Console.Error.WriteLine("Ugyldig konfiguration i appsettings.json:");
    if (ex is OptionsValidationException validation)
    {
        foreach (string failure in validation.Failures)
            Console.Error.WriteLine($"  - {failure}");
    }
    else
    {
        // Fx tekst hvor der forventes et tal, eller ugyldig JSON
        Console.Error.WriteLine($"  - {ex.Message} {ex.InnerException?.Message}");
    }

    // Hostens Ctrl+C-håndtering blokerer programmets afslutning, indtil hosten er disposed
    host?.Dispose();
    return 1;
}

using (host)
{
    KnockKnockTcpServer server = host.Services.GetRequiredService<KnockKnockTcpServer>();
    IHostApplicationLifetime lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

    try
    {
        // Hovedtråden lytter efter nye klienter, indtil der trykkes Ctrl+C
        server.Run(lifetime.ApplicationStopping);
    }
    catch (SocketException ex)
    {
        Console.Error.WriteLine($"Kunne ikke starte serveren: {ex.Message}");
        return 1;
    }
    finally
    {
        await host.StopAsync();
    }
}
return 0;

static IHost BuildHost(string[] args)
{
    // Læs appsettings.json fra programmets mappe, så serveren kan startes fra en vilkårlig mappe
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        Args = args,
        ContentRootPath = AppContext.BaseDirectory
    });

    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
    // Konfigurationsfejl skrives selv som en kort besked - ikke som hostens lange stack trace
    builder.Logging.AddFilter("Microsoft.Extensions.Hosting", LogLevel.None);

    // Options Pattern: bind sektionerne i appsettings.json til stærkt typede klasser og valider dem
    builder.Services.AddOptions<ServerOptions>()
        .Bind(builder.Configuration.GetSection(ServerOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddOptions<JokeOptions>()
        .Bind(builder.Configuration)
        .Validate(o => o.Jokes.Count > 0, "Jokes: der skal være mindst én vittighed.")
        .Validate(o => o.Jokes.All(j => !string.IsNullOrWhiteSpace(j.Who) && !string.IsNullOrWhiteSpace(j.Punchline)),
            "Jokes: alle vittigheder skal have både 'Who' og 'Punchline'.")
        .ValidateOnStart();

    builder.Services.AddOptions<JokeSelectionOptions>()
        .Bind(builder.Configuration.GetSection(JokeSelectionOptions.SectionName))
        .ValidateDataAnnotations()
        .Validate<IServiceProvider>((o, services) => services.GetKeyedService<IJokeSelector>(o.Strategy) != null,
            "JokeSelection:Strategy er ukendt. Brug navnet på en registreret strategi, fx 'Random' eller 'Sequential'.")
        .ValidateOnStart();

    // Strategy Pattern: hver strategi registreres under et navn. En ny strategi kræver
    // en ny klasse og én linje her - samtalelogikken og serveren skal ikke ændres.
    builder.Services.AddKeyedSingleton<IJokeSelector, RandomJokeSelector>("Random");
    builder.Services.AddKeyedSingleton<IJokeSelector, SequentialJokeSelector>("Sequential");

    // Den strategi, der er valgt i konfigurationen
    builder.Services.AddSingleton<IJokeSelector>(services =>
    {
        string strategy = services.GetRequiredService<IOptions<JokeSelectionOptions>>().Value.Strategy;
        return services.GetRequiredKeyedService<IJokeSelector>(strategy);
    });

    builder.Services.AddSingleton<IConversationFactory, KnockKnockConversationFactory>();
    builder.Services.AddSingleton<KnockKnockTcpServer>();

    return builder.Build();
}
