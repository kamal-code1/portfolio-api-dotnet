# PortfolioAPI

API REST en ASP.NET Core 8 qui fournit les données d'un portfolio de développeur : projets, compétences, expériences, formations et messages de contact. Les opérations d'écriture et la lecture des messages sont réservées au rôle Admin (JWT) ; l'inscription publique crée un rôle User sans privilèges.

## Stack

- ASP.NET Core 8 (Web API, contrôleurs)
- Entity Framework Core 8 avec PostgreSQL (Npgsql)
- Authentification JWT Bearer, mots de passe hachés avec BCrypt
- Swagger / OpenAPI (`openapi.yaml`)
- Dockerfile multi-étapes

## Endpoints

| Méthode | Route | Auth |
|---|---|---|
| POST | `/api/auth/login` | — |
| POST | `/api/auth/register` | — |
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
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | Chaîne de connexion PostgreSQL |
| `Jwt:Key` | `Jwt__Key` | Clé de signature, 32 caractères minimum |
| `Admin:Email` | `Admin__Email` | Optionnel : compte admin créé au premier démarrage |
| `Admin:Password` | `Admin__Password` | Optionnel : mot de passe de ce compte |

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

Tests : le projet ne contient pas encore de tests automatisés.

Avec Docker :

```bash
docker build -t portfolio-api .
docker run -p 7000:7000 -e ASPNETCORE_URLS=http://+:7000 -e ConnectionStrings__DefaultConnection="..." -e Jwt__Key="..." portfolio-api
```

## Auteur

Kamal Drissi El-Bouzaidi — [GitHub](https://github.com/kamal-code1)
