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
