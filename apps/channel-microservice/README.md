# 🛰️ Channel Microservice

Denne microservice håndterer **kanaler** i Bizcord-systemet.  
Den er udviklet som en del af **Compulsory Assignment #1 (W41)** i faget *System Integration*.

---

## Struktur i monorepo
apps/
channel-microservice/
src/
Application/
Contracts/
Domain/
Infrastructure/
Messaging/
Presentation/
Program.cs
tests/
ChannelMicroservice.UnitTests/
ChannelMicroservice.IntegrationTests/
Dockerfile
docker-compose.yml


---

## Domain Model

**Entity:**
```csharp
Channel {
  string Id;
  string Name;
  DateTimeOffset CreatedAt;
}

Rules:

Name kræves (ikke tom)
Trimmer whitespace
Maks 100 tegn
Genererer Id og CreatedAt automatisk

API Endpoints
POST /channels
Opretter en ny kanal.
Request
{
  "name": "general"
}

Response (201 Created)

{
  "id": "a4f22c9...",
  "name": "general",
  "createdAt": "2025-10-19T12:09:44.109Z"
}


Errors

400 Bad Request → ugyldigt navn

409 Conflict → kanalnavn allerede findes

GET /health

Health check endpoint for at verificere at servicen kører.

Response

{ "ok": true }

### Events

Produced Event:
channel.created

Payload

{
  "channelId": "uuid",
  "name": "general",
  "createdAt": "2025-10-19T12:09:44.109Z"
}


(Event-udgivelse er i øjeblikket en mock — reelt RabbitMQ-integreret senere.)

Kør lokalt
dotnet run --project apps/channel-microservice/src


Test endpoints:

# Health
Invoke-RestMethod -Uri http://localhost:5171/health

# POST /channels
$body = @{ name = "general" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://localhost:5171/channels -ContentType application/json -Body $body

Kør via Docker Compose
docker compose -f apps/channel-microservice/docker-compose.yml up --build


Adresser:

API → http://localhost:8000/health

RabbitMQ UI → http://localhost:15672

(brugernavn: guest / password: guest)

Tests
Kør alle tests:

dotnet test apps/channel-microservice/tests

Unit tests

Tester Channel domænelogik (trim, validering, timestamps)
Tester ChannelService oprettelse og validering

Integration tests

Tester REST-endpoint /channels
Verificerer 201-response og publicering af event (mocked message client)

W12 – API Gateway & Authorization
API Gateway Routing

I uge 12 blev API Gateway-delen for Bizcord opsat ved hjælp af YARP (Yet Another Reverse Proxy).
Gatewayen fungerer som indgangen til systemet og videresender trafik til Channel-microservice.

Implementeret routing:
"ReverseProxy": {
  "Routes": {
    "channel-route": {
      "ClusterId": "channel-cluster",
      "Match": { "Path": "/channels/{**catch-all}" }
    }
  },
  "Clusters": {
    "channel-cluster": {
      "Destinations": {
        "channel-api": {
          "Address": "http://localhost:5171/"
        }
      }
    }
  }
}


Gatewayen lytter på:
http://localhost:5099

Channel-service lytter på:
http://localhost:5171

Authorization via API-Key

Som en del af uge 12-kravet blev der implementeret API key-beskyttelse på Channel-microservice.

Hvordan det virker:

Endpoints i Channel-service kræver headeren:
X-Api-Key: super-secret-key

Manglende nøgle → 401 Unauthorized

Forkert nøgle → 401 Unauthorized

Korrekt nøgle → request accepteres

Eksempel (PowerShell)
Uden API-nøgle
Invoke-RestMethod -Method Post `
  -Uri http://localhost:5171/channels `
  -ContentType application/json `
  -Body (@{ name = "no-key" } | ConvertTo-Json)


Resultat: 401 Unauthorized

Med API-nøgle
$body = @{ name = "with-key" } | ConvertTo-Json

Invoke-RestMethod -Method Post `
  -Uri http://localhost:5171/channels `
  -ContentType application/json `
  -Headers @{ "X-Api-Key" = "super-secret-key" } `
  -Body $body


Resultat: 201 Created + ny channel

## Reliability (W48)

I denne uge blev der arbejdet med reliability patterns for Channel-microservicen.  
Fokus var på at identificere svage punkter og implementere en konkret strategi til at øge robustheden i servicen.

### Potentielle failure points

Nedenfor er de primære steder, microservicen kan fejle — og hvordan problemer håndteres.

1. **HTTP-kald fra ChannelMicroservice → HashiCorp Vault**

   - **Failure:** Vault kan være nede, utilgængelig eller langsom (timeout, 5xx-fejl, DNS-problemer).
   - **Konsekvens:** Microservicen kan ikke hente messaging connection string ved startup.
   - **Mitigation:**
     - Der er implementeret en Polly **retry policy**, som forsøger kaldet til Vault flere gange, hvis der opstår midlertidige fejl.
     - Fejl logges med `ILogger<Program>`, og microservicen starter stadig op, selv hvis Vault ikke kunne nås efter alle forsøg.

2. **ChannelService → Repository (in-memory / senere database)**

   - **Failure:** Repository kan kaste exceptions (fx ved invalide input eller duplicate navn).
   - **Konsekvens:** Oprettelse af kanal fejler.
   - **Mitigation:**
     - `ChannelEndpoints` håndterer exceptions og returnerer passende HTTP-statuskoder:
       - `400 Bad Request` ved `ArgumentException`
       - `409 Conflict` ved `InvalidOperationException`
     - Dette forhindrer, at applikationen crasher, og giver klienten en tydelig fejlbesked.

3. **Event publishing (IMessageClient / senere RabbitMQ)**

   - **Failure:** Messaging-system kan være nede eller utilgængeligt.
   - **Konsekvens:** `channel.created`-events kan gå tabt.
   - **Mitigation:**
     - I denne version er `IMessageClient` en mock (`NoopMessageClient`), så der opstår ikke reelle integrationsfejl endnu.
     - Når RabbitMQ integreres, er planen at tilføje:
       - Retry på publish-kaldet
       - Dead-letter queues
       - Durable exchanges/queues

---

### Implementeret reliability policy – Retry på Vault-kald

Der er implementeret en **Polly retry-strategi** omkring kaldet til HashiCorp Vault ved startup.

Ved opstart oprettes en retry policy:

```csharp
var vaultRetryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt)
    );
Policy’en bruges til at kalde Vault:
using (var scope = app.Services.CreateScope())
{
    var vault = scope.ServiceProvider.GetRequiredService<VaultMessagingSettingsProvider>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var connectionString = await vaultRetryPolicy.ExecuteAsync(
            () => vault.GetConnectionStringAsync()
        );

        logger.LogInformation("Loaded messaging connection string from Vault: {Conn}", connectionString);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to load messaging connection string from Vault, even after retries.");
    }
}
Resultat:

Microservicen er mere robust over for midlertidige fejl i Vault.

Der forsøges automatisk igen op til 3 gange, med stigende ventetid.

Hvis alle forsøg fejler, får vi structured logging, men microservicen kan stadig starte, så den ikke er hårdt afhængig af Vault for at køre.

W15 – Saga Pattern (Channel Creation Workflow)

I uge 15 blev der implementeret et Saga-pattern for Channel-microservicen.
En Saga bruges til at koordinere distribuerede workflows, specielt når flere systemer eller trin skal lykkes — eller rulles tilbage ved fejl.

Formål

Når en ny kanal bliver oprettet, skal der køres et workflow som senere vil involvere flere services, fx:

Audit logging

Notifications

Message syncing

Database writes i andre systemer

Indtil videre er Saga’en en mocked / simplified implementering, men den viser strukturen og flowet.

ChannelCreationSaga

Sagaen ligger i:

apps/channel-microservice/src/Messaging/Sagas/ChannelCreationSaga.cs

Hvad sker der?

Når POST /channels kaldes:

ChannelService.CreateAsync() opretter kanalen

Sagaen starter via:

await saga.HandleAsync(dto.Id, dto.Name, ct);


Sagaen logger workflowet og simulerer flere steps

Hvis et step fejler, bliver kompensation trigget (mocked)

Integration i Endpoints

Sagaen bliver kaldt direkte fra ChannelEndpoints:

group.MapPost("/", async (
        CreateChannelRequest req,
        ChannelService svc,
        ChannelCreationSaga saga,
        CancellationToken ct) =>
{
    var dto = await svc.CreateAsync(req, ct);

    await saga.HandleAsync(dto.Id, dto.Name, ct);

    return Results.Created($"/channels/{dto.Id}", dto);
});


Dette sikrer, at hver gang en kanal bliver oprettet, kører Saga-workflowet.

Fejlhåndtering & Compensating Actions

Sagaen er bygget efter de klassiske Saga-principper:

Try: kør trin for trin i workflowet

Catch: log fejl

Compensate: rulle tilbage (mocked)

Continue: service crasher ikke ved fejl i andre systemer

Det gør processen robust og klar til udvidelse i næste iteration.

Resultat

Microservicen understøtter nu:

✔ Saga workflow ved channel creation
✔ Klar struktur til distributed transactions
✔ Kompensationsstrategi (mocked)
✔ Ensartet flow der kan udvides til RabbitMQ, AuditService, NotificationService m.fl.
✔ Fuldt integreret i API’et