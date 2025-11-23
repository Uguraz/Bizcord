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

##  Reliability (W48)

Denne uge blev der arbejdet med reliability patterns for Channel-microservicen. Fokus var på at identificere svage punkter og implementere en konkret strategi til at øge robustheden i servicen.

Potentielle Failure Points

Nedenfor er de primære steder, microservicen kan fejle — og hvordan problemer håndteres.

1. HTTP-kald fra API Gateway → Channel Microservice

Failure: Microservicen kan være nede, langsom eller midlertidigt utilgængelig.
Konsekvens: Brugeren oplever at forespørgslen fejler eller loader for længe.
Mitigation:

Vi har implementeret Retry policy, som automatisk forsøger requestet igen ved midlertidige fejl.

Brug af circuit breaker er planlagt (ikke implementeret endnu).

2. ChannelService → Repository (in-memory / senere database)

Failure: Repository kan fejle ved duplicate checks eller ved læsning af data.
Konsekvens: 409 conflicts eller brud i flow.
Mitigation:

Validationslogik og exception-håndtering sikrer, at applikationen ikke crasher.

Graciøs håndtering af både ArgumentException og InvalidOperationException.

3. Event Publishing (RabbitMQ senere)

Failure: Messaging-system kan være nede eller utilgængeligt.
Konsekvens: ChannelCreated-events tabes.
Mitigation:

Event publishing er pt. mock’et.

Når RabbitMQ integreres tilføjes:

Retry

Dead-letter queue

Durable exchange/queue opsætning

Implementeret Reliability Policy
Retry Policy for ChannelService

Der er implementeret en Polly-retry strategi som beskytter mod midlertidige fejl i repository- eller event-publishing-laget.

Retry forsøger operationen igen 3 gange med eksponentiel backoff.

Eksempel fra Program.cs:

var retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt),
        onRetry: (ex, delay, attempt, ctx) =>
        {
            builder.Logger.LogWarning(
                ex,
                "Retrying operation (attempt {Attempt}) after {Delay}ms",
                attempt,
                delay.TotalMilliseconds);
        });


Policy’en bruges ved endpoint-kald:

app.MapPost("/channels", async (CreateChannelRequest req, ChannelService svc, CancellationToken ct) =>
{
    return await retryPolicy.ExecuteAsync(async () =>
    {
        var dto = await svc.CreateAsync(req, ct);
        return Results.Created($"/channels/{dto.Id}", dto);
    });
});

Resultat

Microservicen er nu mere robust, især mod midlertidige repository-fejl.

Brugeren får færre fejlbeskeder og bedre stabilitet.

Logging gør det synligt, når retry mekanismen aktiveres.