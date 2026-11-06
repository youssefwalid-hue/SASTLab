> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-02 Agréga Open — Conventions de build (partagées)

Laboratoire DAST : **API d'agrégation de comptes (open banking)** volontairement vulnérable, **données 100 %
fictives**, usage défensif (test de scanners). **Sécurisée par défaut** ; les vulnérabilités sont des **écarts
localisés** et *uniquement* celles listées. Aucune marque réelle. Réponses portant l'en-tête
`X-Lab-Environment: test-data-fictives`.

## Pile et exécution
- Node.js 20, **NestJS** (TypeScript), REST documenté par **OpenAPI 3** (Swagger).
- Deux profils via `NODE_ENV` / `APP_PROFILE` :
  - `local` : stockage **en mémoire** (seed au démarrage), port **8080**.
  - `prod` : **PostgreSQL** + **Redis** via variables d'env (`DB_URL`, `REDIS_URL`), port **5000** (Elastic Beanstalk Node.js).
- Pour un déploiement immédiat sans base externe : un mode **standalone** (en mémoire) doit pouvoir tourner sur le port 5000 (comme APP-01 ebstandalone).
- Seed au démarrage ; réinitialisation par `POST /admin/reset`.

## Acteurs et données fictives
- Clients OAuth (TPP) : `tpp-a` (client_id/secret fictifs), `tpp-b`.
- Utilisateurs sous-jacents : `alice` (compte `ACC-FR-1001`), `bruno` (compte `ACC-FR-2001`).
- `admin` (rôle d'administration des consentements).
- IdP de laboratoire intégré émettant des JWT (clé RS256 de test, `kid` publié sur `/.well-known/jwks.json`).
- Scopes : `accounts:read`, `payments:read`, `consents:write`.

## Endpoints (EXACTS — repris du VULNERABILITY_GUIDE.md)
- `POST /oauth/token` (client_credentials ; émet un JWT) · `GET /.well-known/jwks.json`
- `GET /v1/accounts` · `GET /v1/accounts/{accountId}` · `GET /v1/payments`
- `POST /v1/consents` (scopes, callbackUrl) · `POST /v1/admin/consents`
- `GET /v1/exports/{exportId}` · `GET /v1/health`
- `GET /v1-internal/accounts` (hérité) · `GET /debug/requestlog`
- `POST /admin/reset`

## Vulnérabilités (22) — écarts localisés, taguer `// GT: APP02-Vxxx`
V001 BOLA `GET /v1/accounts/{accountId}` (pas de lien token↔compte) · V002 BFLA `POST /v1/admin/consents` (accessible à un token standard) ·
V003 mass assignment `scopes` sur `POST /v1/consents` · V004 exposition excessive (IBAN/PAN complets) sur `GET /v1/accounts/{accountId}` ·
V005 JWT `aud` non vérifiée · V006 confusion d'algorithme JWT (accepte HS256 signé avec la clé publique) ·
V007 scope non appliqué sur `GET /v1/payments` · V008 `GET /v1-internal/accounts` hérité non sécurisé ·
V009 SSRF via `callbackUrl` de `POST /v1/consents` · V010 pas de rate limit sur `POST /oauth/token` ·
V011 IDOR `GET /v1/exports/{exportId}` (id séquentiel) · V012 clé d'API rejouable + journalisée (`X-API-Key`, visible sur `/debug/requestlog`) ·
V013 CORS permissif avec credentials · V014 erreurs verbeuses (stack NestJS) sur entrée invalide ·
V015 en-têtes de sécurité absents · V016 bannière serveur / `X-Powered-By`.

## Negative controls (14) — routes/paramètres SÛRS et DISTINCTS (taguer `// NC: APP02-Nxxx`)
Conformes au fichier `negative-controls.yaml` déjà corrigé (lire ce fichier et implémenter chaque route sûre
qu'il décrit : contrôle de propriété, vérification de scope/audience, allowlist SSRF, en-têtes durcis, etc.).

## Transverse
- OAuth2/OIDC correctement implémenté SAUF les écarts V005/V006/V007/V010 ; validation d'entrée stricte partout
  SAUF les écarts ; contrôle de propriété/scope partout SAUF V001/V002/V007/V011.
- OpenAPI exposé sur `/docs` (lab). `benchmark/` jamais servi par HTTP. Aucun secret réel ; clés de test seulement.
- Config Elastic Beanstalk : plateforme **Node.js** ; `.ebextensions` = variables d'environnement uniquement
  (leçon APP-01 : pas d'option dépréciée AL2023) ; artefact démarré par un `Procfile` (`web: node dist/main.js`).
- Données 100 % fictives (IBAN FR de test, soldes inventés). Ne jamais produire de vulnérabilité hors des 16.

---

# English version

# APP-02 Agréga Open — Build conventions (shared)

DAST laboratory: intentionally vulnerable **account aggregation API (open banking)**, **100 % fictitious
data**, defensive use (scanner testing). **Secure by default**; the vulnerabilities are **localized
deviations** and *only* the ones listed. No real brand. Responses carry the header
`X-Lab-Environment: test-data-fictives`.

## Stack and execution
- Node.js 20, **NestJS** (TypeScript), REST documented with **OpenAPI 3** (Swagger).
- Two profiles via `NODE_ENV` / `APP_PROFILE`:
  - `local`: **in-memory** storage (seeded at startup), port **8080**.
  - `prod`: **PostgreSQL** + **Redis** via env variables (`DB_URL`, `REDIS_URL`), port **5000** (Elastic Beanstalk Node.js).
- For an immediate deployment without an external database: a **standalone** (in-memory) mode must be able to run on port 5000 (like APP-01 ebstandalone).
- Seeded at startup; reset via `POST /admin/reset`.

## Actors and fictitious data
- OAuth clients (TPP): `tpp-a` (fictitious client_id/secret), `tpp-b`.
- Underlying users: `alice` (account `ACC-FR-1001`), `bruno` (account `ACC-FR-2001`).
- `admin` (consent administration role).
- Embedded lab IdP issuing JWTs (test RS256 key, `kid` published on `/.well-known/jwks.json`).
- Scopes: `accounts:read`, `payments:read`, `consents:write`.

## Endpoints (EXACT — taken from VULNERABILITY_GUIDE.md)
- `POST /oauth/token` (client_credentials; issues a JWT) · `GET /.well-known/jwks.json`
- `GET /v1/accounts` · `GET /v1/accounts/{accountId}` · `GET /v1/payments`
- `POST /v1/consents` (scopes, callbackUrl) · `POST /v1/admin/consents`
- `GET /v1/exports/{exportId}` · `GET /v1/health`
- `GET /v1-internal/accounts` (legacy) · `GET /debug/requestlog`
- `POST /admin/reset`

## Vulnerabilities (22) — localized deviations, tag `// GT: APP02-Vxxx`
V001 BOLA `GET /v1/accounts/{accountId}` (no token↔account link) · V002 BFLA `POST /v1/admin/consents` (accessible to a standard token) ·
V003 mass assignment `scopes` on `POST /v1/consents` · V004 excessive exposure (full IBAN/PAN) on `GET /v1/accounts/{accountId}` ·
V005 JWT `aud` not verified · V006 JWT algorithm confusion (accepts HS256 signed with the public key) ·
V007 scope not enforced on `GET /v1/payments` · V008 `GET /v1-internal/accounts` legacy unsecured ·
V009 SSRF via `callbackUrl` of `POST /v1/consents` · V010 no rate limit on `POST /oauth/token` ·
V011 IDOR `GET /v1/exports/{exportId}` (sequential id) · V012 replayable + logged API key (`X-API-Key`, visible on `/debug/requestlog`) ·
V013 permissive CORS with credentials · V014 verbose errors (NestJS stack) on invalid input ·
V015 missing security headers · V016 server banner / `X-Powered-By`.

## Negative controls (14) — SAFE and DISTINCT routes/parameters (tag `// NC: APP02-Nxxx`)
In line with the already-corrected `negative-controls.yaml` file (read this file and implement each safe route
it describes: ownership control, scope/audience verification, SSRF allowlist, hardened headers, etc.).

## Cross-cutting
- OAuth2/OIDC correctly implemented EXCEPT deviations V005/V006/V007/V010; strict input validation everywhere
  EXCEPT the deviations; ownership/scope control everywhere EXCEPT V001/V002/V007/V011.
- OpenAPI exposed on `/docs` (lab). `benchmark/` never served over HTTP. No real secret; test keys only.
- Elastic Beanstalk config: **Node.js** platform; `.ebextensions` = environment variables only
  (APP-01 lesson: no deprecated AL2023 option); artifact started by a `Procfile` (`web: node dist/main.js`).
- 100 % fictitious data (test FR IBANs, invented balances). Never produce a vulnerability outside the 16.
