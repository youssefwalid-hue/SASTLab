> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-03 — Trésoria (Portail de trésorerie entreprises)

Application **volontairement vulnérable** du *Banking DAST Benchmark Lab*.
**Usage défensif uniquement** (test de scanners DAST en laboratoire interne).
**DONNÉES 100% FICTIVES — aucun secret réel.** Toutes les réponses portent
l'en-tête `X-Lab-Environment: test-data-fictives`.

> Même procédé qu'APP-01 (Java) et APP-02 (NestJS) : **sécurisé par défaut**,
> les vulnérabilités sont des **écarts localisés** et *uniquement* les 16 listées.

## Pile

- **ASP.NET Core (.NET 8, C#)** — API REST documentée par **OpenAPI/Swagger** (`/swagger`).
- **SPA minimale** (pages statiques servies depuis `wwwroot/`) pour exercer le rendu
  du libellé (V009) et la navigation.
- Accent : **logique métier** (double validation, condition de concurrence) et
  **API** (BFLA, mass assignment, JWT).

## Profils d'exécution

| Profil       | Stockage            | Port | Variables |
|--------------|---------------------|------|-----------|
| `local`      | En mémoire          | 8080 | `APP_PROFILE=local` |
| `prod`       | SQL Server + Redis  | 5000 | `APP_PROFILE=prod`, `DB_URL`, `REDIS_URL` |
| `standalone` | En mémoire          | 5000 | `APP_PROFILE=standalone` (déploiement EB sans base) |

Le stockage passe par l'abstraction `IStore` : `MemoryStore` (local/standalone) ou une
implémentation SQL Server + Redis (prod). La logique métier — et ses écarts volontaires —
reste identique quel que soit le back-end.

## Démarrage local

```bash
APP_PROFILE=local dotnet run          # http://localhost:8080
```

- SPA : `http://localhost:8080/`
- Swagger : `http://localhost:8080/swagger`
- Santé : `GET /health`
- Réinitialisation de l'état : `POST /admin/reset`

## Comptes de laboratoire (fictifs)

Mot de passe : `Password1!` · OTP : `123456`

| Utilisateur | Rôle       | Entreprise |
|-------------|------------|------------|
| `saisie1`   | saisie     | A |
| `valideur1` | validation | A |
| `admin-ent` | admin      | A |
| `saisie2`   | saisie     | B |

Lots fictifs : `9001`..`9003` (A), `9005` (B). Plafond quotidien fictif : 10 000,00 / entreprise.

## Endpoints principaux

`POST /api/login` · `POST /api/logout` · `POST /api/token/refresh` ·
`POST|GET /api/batches` · `GET|PUT|DELETE /api/batches/{id}` ·
`POST /api/batches/{id}/submit|validate|approve` · `GET /api/batches/{id}/export` ·
`POST /api/transfers` · `GET /api/transfers?batchId=` · `GET /api/transfers/{id}` ·
`GET /api/reports?sort=&metric=&accountId=&groupBy=` ·
`POST /api/beneficiaries/{id}/approve` · `PUT /api/beneficiaries/{id}` ·
`POST /api/password/reset` · `GET /signin-callback?returnUrl=` ·
`GET /swagger/v1/swagger.json` · `POST /admin/reset` · `GET /health`

## Benchmark

- `benchmark/ground-truth.yaml` — vérité terrain des 22 vulnérabilités (**oracle**).
- `benchmark/validation/validate.py` — validation reproductible (22 vulns + 13 NC).

> **`benchmark/` ne doit JAMAIS être servi par HTTP ni fourni aux scanners.**
> Il est exclu du déploiement par `.ebignore`.

## Déploiement Elastic Beanstalk (.NET sur Linux)

- `.ebextensions/01-options.config` : **variables d'environnement uniquement**
  (leçon AL2023 — aucune option dépréciée).
- `Procfile` : `web: dotnet Tresoria.dll` (port 5000, profil standalone par défaut).
- `.ebignore` : exclut `benchmark/`, les sources C# et la documentation sensible.

```bash
dotnet publish -c Release -o publish
cd publish && eb deploy        # (environnement cree au prealable via `eb create`)
```

Voir `SECURITY_MODEL.md` pour le modèle de sécurité et la cartographie des 22 écarts.

---

# English version

# APP-03 — Trésoria (Corporate treasury portal)

**Deliberately vulnerable** application of the *Banking DAST Benchmark Lab*.
**Defensive use only** (testing DAST scanners in an internal lab).
**100% FICTITIOUS DATA — no real secrets.** All responses carry
the `X-Lab-Environment: test-data-fictives` header.

> Same approach as APP-01 (Java) and APP-02 (NestJS): **secure by default**,
> the vulnerabilities are **localized deviations** and *only* the 16 listed ones.

## Stack

- **ASP.NET Core (.NET 8, C#)** — REST API documented with **OpenAPI/Swagger** (`/swagger`).
- **Minimal SPA** (static pages served from `wwwroot/`) to exercise label rendering
  (V009) and navigation.
- Focus: **business logic** (dual validation, race condition) and
  **API** (BFLA, mass assignment, JWT).

## Execution profiles

| Profile      | Storage             | Port | Variables |
|--------------|---------------------|------|-----------|
| `local`      | In memory           | 8080 | `APP_PROFILE=local` |
| `prod`       | SQL Server + Redis  | 5000 | `APP_PROFILE=prod`, `DB_URL`, `REDIS_URL` |
| `standalone` | In memory           | 5000 | `APP_PROFILE=standalone` (EB deployment without a database) |

Storage goes through the `IStore` abstraction: `MemoryStore` (local/standalone) or a
SQL Server + Redis implementation (prod). The business logic — and its intentional deviations —
remains identical regardless of the backend.

## Local startup

```bash
APP_PROFILE=local dotnet run          # http://localhost:8080
```

- SPA: `http://localhost:8080/`
- Swagger: `http://localhost:8080/swagger`
- Health: `GET /health`
- State reset: `POST /admin/reset`

## Lab accounts (fictitious)

Password: `Password1!` · OTP: `123456`

| User        | Role       | Company |
|-------------|------------|---------|
| `saisie1`   | saisie     | A |
| `valideur1` | validation | A |
| `admin-ent` | admin      | A |
| `saisie2`   | saisie     | B |

Fictitious batches: `9001`..`9003` (A), `9005` (B). Fictitious daily cap: 10 000,00 / company.

## Main endpoints

`POST /api/login` · `POST /api/logout` · `POST /api/token/refresh` ·
`POST|GET /api/batches` · `GET|PUT|DELETE /api/batches/{id}` ·
`POST /api/batches/{id}/submit|validate|approve` · `GET /api/batches/{id}/export` ·
`POST /api/transfers` · `GET /api/transfers?batchId=` · `GET /api/transfers/{id}` ·
`GET /api/reports?sort=&metric=&accountId=&groupBy=` ·
`POST /api/beneficiaries/{id}/approve` · `PUT /api/beneficiaries/{id}` ·
`POST /api/password/reset` · `GET /signin-callback?returnUrl=` ·
`GET /swagger/v1/swagger.json` · `POST /admin/reset` · `GET /health`

## Benchmark

- `benchmark/ground-truth.yaml` — ground truth of the 22 vulnerabilities (**oracle**).
- `benchmark/validation/validate.py` — reproducible validation (22 vulns + 13 NC).

> **`benchmark/` must NEVER be served over HTTP nor provided to scanners.**
> It is excluded from deployment by `.ebignore`.

## Elastic Beanstalk deployment (.NET on Linux)

- `.ebextensions/01-options.config`: **environment variables only**
  (AL2023 lesson — no deprecated option).
- `Procfile`: `web: dotnet Tresoria.dll` (port 5000, standalone profile by default).
- `.ebignore`: excludes `benchmark/`, the C# sources and sensitive documentation.

```bash
dotnet publish -c Release -o publish
cd publish && eb deploy        # (environment created beforehand via `eb create`)
```

See `SECURITY_MODEL.md` for the security model and the mapping of the 16 deviations.
