# PortfolioAPI

[![CI](https://github.com/kamal-code1/portfolio-api-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/kamal-code1/portfolio-api-dotnet/actions/workflows/ci.yml)

API REST en ASP.NET Core 8 qui fournit les données d'un portfolio de développeur : projets, compétences, expériences, formations et messages de contact. Les opérations d'écriture et la lecture des messages sont réservées au rôle Admin (JWT) ; l'inscription publique crée un rôle User sans privilèges.

## Stack

- ASP.NET Core 8 (Web API, contrôleurs)
- Entity Framework Core 8 avec PostgreSQL (Npgsql)
- Authentification JWT Bearer, mots de passe hachés avec BCrypt
- Swagger / OpenAPI (`openapi.yaml`)
- Dockerfile multi-étapes
- Tests d'intégration xUnit + GitHub Actions

## Endpoints

| Méthode | Route | Auth |
|---|---|---|
| POST | `/api/auth/login` | — |
| POST | `/api/auth/register` | Désactivé par défaut |
| GET | `/api/portfolio/projets` | — |
| POST / DELETE | `/api/portfolio/projets`, `/api/portfolio/projets/{id}` | JWT (Admin) |
| GET | `/api/portfolio/competences`, `/api/portfolio/competences/grouped` | — |
| GET / POST | `/api/portfolio/experiences` | POST : JWT (Admin) |
| GET / POST | `/api/portfolio/formations` | POST : JWT (Admin) |
| POST | `/api/portfolio/messages` | — |
| GET / PUT | `/api/portfolio/messages`, `/api/portfolio/messages/{id}/lu` | JWT (Admin) |
| GET | `/api/portfolio/utilisateurs` | JWT (Admin) |
| GET | `/api/portfolio/me` | JWT |

## Configuration

Aucun secret n'est versionné. L'application refuse de démarrer si la connexion PostgreSQL ou la clé JWT manque.

| Clé | Variable d'environnement | Description |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | Chaîne PostgreSQL, ou `Data Source=<fichier>.db` pour SQLite |
| `Jwt:Key` | `Jwt__Key` | Clé de signature, 32 caractères minimum |
| `Email:Provider` | `Email__Provider` | `Resend` (API HTTP) ou `Smtp` (ex. Gmail) |
| `Email:To` | `Email__To` | Adresse qui reçoit les messages du formulaire |
| `Email:ResendApiKey` | `Email__ResendApiKey` | Clé API Resend (si `Resend`) |
| `Email:From` | `Email__From` | Optionnel, défaut `Portfolio <onboarding@resend.dev>` |
| `Email:SmtpUser` / `Email:SmtpPassword` | `Email__SmtpUser` / `Email__SmtpPassword` | Si `Smtp` : compte et mot de passe d'application Gmail |
| `Admin:Email` | `Admin__Email` | Optionnel : compte admin créé au premier démarrage |
| `Admin:Password` | `Admin__Password` | Optionnel : mot de passe de ce compte |
| `Auth:AllowRegistration` | `Auth__AllowRegistration` | Optionnel, `false` par défaut : désactive `/api/auth/register` |

## Formulaire de contact

Le site public (`wwwroot/`) envoie le formulaire à `POST /api/portfolio/messages`. Le message est enregistré, puis transmis par e-mail. Le visiteur ne voit un succès que si les deux étapes ont réussi.

Protections : validation des champs (longueurs, format e-mail), corps limité à 16 Ko, champ piège anti-robots, 5 envois par IP toutes les 15 minutes, e-mail en texte brut avec sujet sur une ligne (pas d'injection d'en-têtes), en-têtes de sécurité (CSP, nosniff, X-Frame-Options). La lecture des messages est réservée au rôle Admin.

## Déploiement (Render, offre gratuite)

`render.yaml` décrit un service web Docker. Dans Render : **New › Blueprint**, sélectionner ce dépôt, puis saisir `Email__To` et `Email__ResendApiKey`. `Jwt__Key` est générée automatiquement. Par défaut, la base SQLite du conteneur est réinitialisée à chaque redéploiement. Pour la conserver, utilisez une base PostgreSQL externe.

En local, avec les secrets utilisateur .NET :

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=portfolio_db;Username=postgres;Password=<mot-de-passe>"
dotnet user-secrets set "Jwt:Key" "<clé-aléatoire-de-32-caractères-minimum>"
```

## Lancement

```bash
dotnet restore
dotnet run
```

Swagger est disponible sur `/swagger` en environnement Development. La base est créée au démarrage (`EnsureCreated`), avec des compétences et projets d'exemple.

Avec Docker :

```bash
docker build -t portfolio-api .
docker run -p 10000:10000 -e ConnectionStrings__DefaultConnection="Data Source=/tmp/portfolio.db" -e Jwt__Key="..." -e Email__Provider=Resend -e Email__To="..." -e Email__ResendApiKey="..." portfolio-api
```

## Tests

Tests d'intégration xUnit (`tests/PortfolioAPI.Tests`) : l'API démarre en mémoire avec `WebApplicationFactory`, une base SQLite temporaire et un faux service d'e-mail. Ils couvrent `/health`, la lecture des projets, les en-têtes de sécurité, l'authentification JWT (rôle Admin, refus sans jeton, inscription désactivée) et le formulaire de contact (validation, champ piège, échec d'envoi, limite de débit).

```bash
dotnet test tests/PortfolioAPI.Tests
```

La CI GitHub Actions (`.github/workflows/ci.yml`) compile le projet et lance ces tests à chaque push et pull request.

## Auteur

Kamal Drissi El-Bouzaidi — [GitHub](https://github.com/kamal-code1)
