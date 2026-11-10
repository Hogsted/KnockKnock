# Knock Knock Server 2.0

Multithreaded TCP-server, der fortæller Knock Knock-vittigheder. Vittigheder og indstillinger
indlæses fra `KnockKnockServer/appsettings.json` via Options Pattern og dependency injection.

| Projekt | Indhold |
|---|---|
| `KnockKnockServer` | Serveren |
| `KnockKnockClient` | Konsolklient (manuel eller `--auto`) |
| `KnockKnockServer.Tests` | Unit- og integrationstests (xUnit) |
| `docs/` | UML-klassediagram og -tilstandsdiagram |

## Kør

```powershell
dotnet build
dotnet run --project KnockKnockServer          # stop med Ctrl+C
dotnet run --project KnockKnockClient          # manuel klient
dotnet run --project KnockKnockClient -- --auto            # svarer selv
dotnet run --project KnockKnockClient -- --auto --wrong    # sender først et forkert svar
dotnet test
```

Alle indstillinger kan overskrives fra kommandolinjen, uden at ændre filen, fx
`dotnet run --project KnockKnockServer -- --Server:MaxClients=2 --JokeSelection:Strategy=Sequential`.

## Demo af testscenarierne

| Test | Sådan | Automatisk test |
|---|---|---|
| Én klient forbinder | Start server + én klient | `ThreeClients_CanTalkAtTheSameTime` |
| Tre samtidige klienter | Start tre `--auto`-klienter i hver sin terminal | `ThreeClients_CanTalkAtTheSameTime` |
| Forkert svar | Skriv fx `Hello?`, eller brug `--auto --wrong` | `WrongAnswer_KeepsState` |
| Klient afbryder | Luk et klientvindue midt i en samtale | `ClientDisconnecting_DoesNotAffectOthers` |
| Random | `"Strategy": "Random"` | `Random_OnlySelectsConfiguredJokes` |
| Sequential | `"Strategy": "Sequential"` | `Sequential_SelectsInOrderAndStartsOver` |
| Ny vittighed | Tilføj den i `bin/Debug/net9.0/appsettings.json` og genstart exe'en. Ingen genkompilering. | – |
| Maks. klienter | `--Server:MaxClients=2` og start tre klienter | `MaxClientsReached_NewClientIsRejected` |
| Ugyldig konfiguration | Fx `--Server:Port=0` eller `--JokeSelection:Strategy=Foo` | `InvalidConfiguration_StopsWithReadableError` |
| Serveren stoppes | Ctrl+C i serverens vindue | `ServerStopping_ClosesConnectionsAndThread` |

## Protokol

Én UTF-8-kodet besked pr. linje (`\n`). Serveren afslutter altid med en linje `Bye!` og lukker
forbindelsen, både efter vittigheden, når serveren er fuld, og når den lukker ned.

## Delte data og race conditions

| Data | Ejes af | Synkronisering |
|---|---|---|
| `TcpClient`, `ClientSession`, `KnockKnockProtocol` (tilstand) | Én klienttråd | Ingen. Hver klient har sine egne instanser |
| Vittighederne (`KnockKnockJoke[]`) | Delt | Ingen. Læses kun |
| `SequentialJokeSelector._nextIndex` | Delt | `lock`. Ellers kunne to klienter få samme index |
| `Random.Shared` | Delt | Er trådsikker i forvejen |
| `KnockKnockTcpServer._activeSessions` | Hovedtråd + klienttråde | `lock`. Tjek af `MaxClients` og tilføjelse sker under samme lock |
| `KnockKnockTcpServer._nextClientId` | Kun hovedtråden | Ingen nødvendig |
| Skrivning til en klient | Klienttråd + hovedtråd ved nedlukning | `ClientSession._writeLock` |

## SOLID

- **SRP**: `KnockKnockTcpServer` accepterer forbindelser og styrer tråde. `ClientSession` står for
  linjebaseret I/O med én klient. `KnockKnockProtocol` indeholder samtalens regler. `IJokeSelector`
  vælger vittigheder. Options-klasserne beskriver konfigurationen.
- **OCP**: En ny strategi kræver en ny klasse og én `AddKeyedSingleton`-linje i `Program.cs`.
  Server, session og protokol ændres ikke.
- **LSP**: `RandomJokeSelector` og `SequentialJokeSelector` kan begge bruges, hvor der forventes en
  `IJokeSelector`, og begge er trådsikre som interfacet kræver.
- **ISP**: `ClientSession` kender kun `IConversation` (`Start`, `HandleInput`, `IsFinished`), og
  serveren kender kun `IConversationFactory.Create()`.
- **DIP**: Server, factory og strategier afhænger af abstraktioner (`IConversationFactory`,
  `IJokeSelector`, `IOptions<T>`) og får dem via constructor injection.
