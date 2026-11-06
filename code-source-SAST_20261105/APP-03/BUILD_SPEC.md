> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-03 Trésoria — Conventions de build (partagées)

Laboratoire DAST : **portail de trésorerie entreprises** volontairement vulnérable, **données 100 % fictives**,
usage défensif (test de scanners). **Sécurisé par défaut** ; les vulnérabilités sont des **écarts localisés** et
*uniquement* celles listées. Aucune marque réelle. Réponses portant `X-Lab-Environment: test-data-fictives`.
Accent : **logique métier** (double validation, condition de concurrence) et **API** (BFLA, mass assignment, JWT).

## Pile et exécution
- **ASP.NET Core (C#, .NET 8)** pour l'API ; SPA **Angular** (une version compilée simple, ou des pages statiques
  servies par l'API, suffit pour exercer le rendu et les routes). REST documenté par **OpenAPI/Swagger**.
- Profils via `ASPNETCORE_ENVIRONMENT` / `APP_PROFILE` :
  - `local` : stockage **en mémoire** (EF Core InMemory ou dépôt mémoire), port **8080**.
  - `prod` : **SQL Server** + **Redis** via variables d'env (`DB_URL`, `REDIS_URL`), port **5000**.
  - mode **standalone** (en mémoire) qui tourne aussi sur le port 5000 pour un déploiement sans base (comme APP-01/APP-02).
- Seed au démarrage ; réinitialisation par `POST /admin/reset`.

## Acteurs et données fictives (multi-tenant)
- Entreprise A : `saisie1` (rôle saisie), `valideur1` (rôle validation), `admin-ent` (admin entreprise).
- Entreprise B : `saisie2` (rôle saisie) — sert aux tests de cloisonnement (IDOR inter-entreprises).
- JWT avec refresh token ; rôles `saisie` / `validation` / `admin`. Plafond quotidien fictif par entreprise.
- Lots (batches) de virements de masse, identifiants fictifs (ex. 9001 entreprise A, 9005 entreprise B).

## Endpoints (EXACTS — repris du VULNERABILITY_GUIDE.md)
- `POST /api/login` · `POST /api/logout` · `POST /api/token/refresh`
- `POST /api/batches` · `GET /api/batches` · `GET /api/batches/{id}` · `PUT /api/batches/{id}`
- `POST /api/batches/{id}/submit` · `POST /api/batches/{id}/validate` · `POST /api/batches/{id}/approve` · `GET /api/batches/{id}/export`
- `POST /api/transfers` · `GET /api/transfers?batchId=` · `GET /api/reports?sort=&metric=&accountId=`
- `GET /signin-callback?returnUrl=` · `GET /swagger/v1/swagger.json`
- Routes sûres (negative controls) : `POST /api/beneficiaries/{id}/approve`, `DELETE /api/batches/{id}`,
  `GET /api/transfers/{id}`, `PUT /api/beneficiaries/{id}`, `POST /api/password/reset`, et paramètres `currency`/`groupBy`.
- `POST /admin/reset`

## Vulnérabilités (22) — écarts localisés, taguer `// GT: APP03-Vxxx`
V001 contournement double validation : le créateur peut `approve` son propre lot (workflow à état) ·
V002 BFLA : rôle saisie peut appeler `/validate` · V003 condition de concurrence TOCTOU sur le plafond quotidien (`POST /api/transfers`) ·
V004 IDOR inter-entreprises `GET /api/batches/{id}` · V005 mass assignment `status` sur `PUT /api/batches/{id}` ·
V006 refresh token non révoqué après logout · V007 montant négatif accepté (`amount`) inversant le sens ·
V008 SQLi dans `sort` du reporting (ORDER BY concaténé) · V009 XSS stocké sur `label` de virement (rendu SPA) ·
V010 exposition excessive à l'export (`/api/batches/{id}/export` renvoie champs internes) ·
V011 open redirect `returnUrl` sur `/signin-callback` · V012 cookie d'auth `SameSite=None` sans `Secure` ·
V013 erreurs .NET verbeuses (stack trace) sur id invalide · V014 en-têtes de sécurité / CSP absents ·
V015 accès à Swagger via `/swagger//v1/swagger.json` (normalisation de chemin) · V016 bannière serveur / `X-Powered-By`.

## Negative controls (13) — routes/paramètres SÛRS et DISTINCTS (taguer `// NC: APP03-Nxxx`)
Lire et implémenter `negative-controls.yaml` (déjà corrigé) : `beneficiaries/{id}/approve` à quatre yeux correct,
`DELETE /api/batches/{id}` restreint au rôle, `transfers` param `currency` validé, `GET /api/transfers/{id}` cloisonné,
`PUT /api/beneficiaries/{id}` DTO en liste blanche (`verified` ignoré), `password/reset` avec `reset_token` robuste,
`reports` param `groupBy` en liste blanche.

## Transverse
- Double validation, autorisation par rôle, cloisonnement par tenant, requêtes paramétrées, DTO en liste blanche,
  encodage de sortie, validation de montant, révocation de refresh token, en-têtes durcis — corrects PARTOUT
  SAUF les écarts listés.
- `.ebextensions` = variables d'environnement uniquement (leçon AL2023) ; `Procfile`/déploiement .NET sur Linux.
- OpenAPI sur `/swagger`. `benchmark/` jamais servi par HTTP. Aucun secret réel. Données 100 % fictives.
- N'ajouter AUCUNE vulnérabilité hors des 16.

---

# English version

# APP-03 Trésoria — Build conventions (shared)

DAST lab: deliberately vulnerable **corporate treasury portal**, **100% fictitious data**,
defensive use (testing scanners). **Secure by default**; the vulnerabilities are **localized deviations** and
*only* those listed. No real brand. Responses carry `X-Lab-Environment: test-data-fictives`.
Focus: **business logic** (dual validation, race condition) and **API** (BFLA, mass assignment, JWT).

## Stack and execution
- **ASP.NET Core (C#, .NET 8)** for the API; **Angular** SPA (a simple compiled build, or static pages
  served by the API, is enough to exercise rendering and routes). REST documented with **OpenAPI/Swagger**.
- Profiles via `ASPNETCORE_ENVIRONMENT` / `APP_PROFILE`:
  - `local`: **in-memory** storage (EF Core InMemory or memory repository), port **8080**.
  - `prod`: **SQL Server** + **Redis** via env variables (`DB_URL`, `REDIS_URL`), port **5000**.
  - **standalone** mode (in memory) that also runs on port 5000 for a deployment without a database (like APP-01/APP-02).
- Seeding at startup; reset via `POST /admin/reset`.

## Actors and fictitious data (multi-tenant)
- Company A: `saisie1` (saisie role), `valideur1` (validation role), `admin-ent` (company admin).
- Company B: `saisie2` (saisie role) — used for isolation tests (cross-company IDOR).
- JWT with refresh token; roles `saisie` / `validation` / `admin`. Fictitious daily cap per company.
- Mass-transfer batches, fictitious identifiers (e.g. 9001 company A, 9005 company B).

## Endpoints (EXACT — taken from VULNERABILITY_GUIDE.md)
- `POST /api/login` · `POST /api/logout` · `POST /api/token/refresh`
- `POST /api/batches` · `GET /api/batches` · `GET /api/batches/{id}` · `PUT /api/batches/{id}`
- `POST /api/batches/{id}/submit` · `POST /api/batches/{id}/validate` · `POST /api/batches/{id}/approve` · `GET /api/batches/{id}/export`
- `POST /api/transfers` · `GET /api/transfers?batchId=` · `GET /api/reports?sort=&metric=&accountId=`
- `GET /signin-callback?returnUrl=` · `GET /swagger/v1/swagger.json`
- Safe routes (negative controls): `POST /api/beneficiaries/{id}/approve`, `DELETE /api/batches/{id}`,
  `GET /api/transfers/{id}`, `PUT /api/beneficiaries/{id}`, `POST /api/password/reset`, and parameters `currency`/`groupBy`.
- `POST /admin/reset`

## Vulnerabilities (22) — localized deviations, tag `// GT: APP03-Vxxx`
V001 dual-validation bypass: the creator can `approve` their own batch (state workflow) ·
V002 BFLA: saisie role can call `/validate` · V003 TOCTOU race condition on the daily cap (`POST /api/transfers`) ·
V004 cross-company IDOR `GET /api/batches/{id}` · V005 mass assignment of `status` on `PUT /api/batches/{id}` ·
V006 refresh token not revoked after logout · V007 negative amount accepted (`amount`) reversing the direction ·
V008 SQLi in reporting `sort` (concatenated ORDER BY) · V009 stored XSS on transfer `label` (SPA rendering) ·
V010 excessive data exposure on export (`/api/batches/{id}/export` returns internal fields) ·
V011 open redirect `returnUrl` on `/signin-callback` · V012 auth cookie `SameSite=None` without `Secure` ·
V013 verbose .NET errors (stack trace) on invalid id · V014 security headers / CSP missing ·
V015 Swagger access via `/swagger//v1/swagger.json` (path normalization) · V016 server banner / `X-Powered-By`.

## Negative controls (13) — SAFE and DISTINCT routes/parameters (tag `// NC: APP03-Nxxx`)
Read and implement `negative-controls.yaml` (already fixed): `beneficiaries/{id}/approve` with correct four eyes,
`DELETE /api/batches/{id}` restricted to role, `transfers` `currency` parameter validated, `GET /api/transfers/{id}` isolated,
`PUT /api/beneficiaries/{id}` allowlisted DTO (`verified` ignored), `password/reset` with robust `reset_token`,
`reports` `groupBy` parameter allowlisted.

## Cross-cutting
- Dual validation, role-based authorization, tenant isolation, parameterized queries, allowlisted DTO,
  output encoding, amount validation, refresh token revocation, hardened headers — correct EVERYWHERE
  EXCEPT at the listed deviations.
- `.ebextensions` = environment variables only (AL2023 lesson); `Procfile`/.NET-on-Linux deployment.
- OpenAPI at `/swagger`. `benchmark/` never served over HTTP. No real secret. 100% fictitious data.
- Add NO vulnerability outside the 16.
