> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-09 — Novabourse (Portail titres)

Application de laboratoire du **Banking DAST Benchmark Lab** : portail titres fictif
exposant une **API REST FastAPI** et une **SPA React** minimale, avec un **IdP de
laboratoire OIDC + PKCE**. L'application est **sécurisée par défaut** et ne comporte
que **22 vulnérabilités volontaires** (écarts localisés) plus des **routes securisées
de référence** (negative controls).

> **Environnement de test — données 100 % fictives — usage défensif** (test de
> scanners DAST). Aucune marque réelle, aucun secret réel. Toutes les réponses
> portent l'en-tête `X-Lab-Environment: test-data-fictives`.

## Pile

- Python 3.12 / **FastAPI**, SPA **React** (servie en statique), **OpenAPI** sur `/docs`.
- Profil **standalone** : dépôt en mémoire + **SQLite** (table `instruments`), sans base externe.
- IdP de laboratoire : **OIDC Authorization Code + PKCE (S256)**, jetons **JWT** (HS256, secret fictif).

## Lancer en local

```bash
pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8080
# SPA : http://localhost:8080/        OpenAPI : http://localhost:8080/docs
```

Réinitialiser l'état de démonstration : `POST /admin/reset`.

## Comptes de laboratoire (mot de passe `Password1!`, OTP `123456`)

| Compte | Rôle | Portefeuille | Compte titres |
| --- | --- | --- | --- |
| `invest-a` | INVESTOR | PF-1001 | ACC-2001 |
| `invest-b` | INVESTOR | PF-1002 | ACC-2002 |
| `backoffice` | BACKOFFICE | — | supervision |

## Flux OIDC PKCE (laboratoire)

1. La SPA génère `code_verifier` + `code_challenge` (S256) et redirige vers
   `GET /authorize?response_type=code&client_id=novabourse-spa&redirect_uri=<uri>&code_challenge=<cc>&code_challenge_method=S256&state=<s>`.
2. Authentification auprès de l'IdP (commodité lab : indice `login=<compte>` pour un
   usage sans navigateur).
3. Retour `GET /callback?code=<code>&state=<s>` ; la SPA échange le code :
   `POST /token` (grant_type=authorization_code, `code_verifier`) → `access_token` JWT.
4. Les appels API portent `Authorization: Bearer <jwt>`.

## Surface API (extrait)

- `GET /api/portfolios`, `GET /api/portfolios/{id}`, `POST /api/portfolios`
- `POST /api/orders`, `GET /api/orders`, `GET /api/orders/{id}`, `POST /api/orders/{id}/cancel`
- `GET /api/admin/orders` (back-office)
- `GET /api/instruments?q=`
- Routes **securisées de référence** : `/api/v2/portfolios`, `/api/v2/portfolios/{id}`,
  `/api/v2/orders`, `/api/v2/instruments` (negative controls).
- OIDC : `GET /authorize`, `GET /signin-oidc`, `POST /token`, `/.well-known/openid-configuration`.

## Déploiement Elastic Beanstalk (plateforme Python)

- `Procfile` : `web: uvicorn main:app --host 0.0.0.0 --port 5000`.
- `.ebextensions/01-options.config` : **variables d'environnement uniquement**
  (type d'instance, HTTPS/ALB gérés par la CLI `eb` — pas d'options dépréciées AL2023).
- `.ebignore` : exclut `benchmark/` et les documents sensibles du bundle déployé.

## Documentation

- `ARCHITECTURE.md`, `USER_GUIDE.md`, `SECURITY_MODEL.md`.
- `benchmark/ground-truth.yaml`, `VULNERABILITY_GUIDE.md`, `negative-controls.yaml`
  (**oracle de notation — jamais exposé par HTTP, jamais fourni aux scanners**).
- `benchmark/validation/validate.py` : rejoue les 22 vulnérabilités + 12 NC et produit
  des rapports JSON/HTML.

---

# English version

# APP-09 — Novabourse (Securities Portal)

Lab application of the **Banking DAST Benchmark Lab**: a fictitious securities portal
exposing a **FastAPI REST API** and a minimal **React SPA**, with an **OIDC + PKCE
lab IdP**. The application is **secure by default** and contains only
**22 intentional vulnerabilities** (localized deviations) plus **reference secured
routes** (negative controls).

> **Test environment — 100% fictitious data — defensive use** (DAST scanner
> testing). No real brand, no real secret. All responses carry the
> `X-Lab-Environment: test-data-fictives` header.

## Stack

- Python 3.12 / **FastAPI**, **React** SPA (served statically), **OpenAPI** at `/docs`.
- **Standalone** profile: in-memory store + **SQLite** (`instruments` table), no external database.
- Lab IdP: **OIDC Authorization Code + PKCE (S256)**, **JWT** tokens (HS256, fictitious secret).

## Run locally

```bash
pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8080
# SPA : http://localhost:8080/        OpenAPI : http://localhost:8080/docs
```

Reset the demo state: `POST /admin/reset`.

## Lab accounts (password `Password1!`, OTP `123456`)

| Account | Role | Portfolio | Securities account |
| --- | --- | --- | --- |
| `invest-a` | INVESTOR | PF-1001 | ACC-2001 |
| `invest-b` | INVESTOR | PF-1002 | ACC-2002 |
| `backoffice` | BACKOFFICE | — | supervision |

## OIDC PKCE flow (lab)

1. The SPA generates `code_verifier` + `code_challenge` (S256) and redirects to
   `GET /authorize?response_type=code&client_id=novabourse-spa&redirect_uri=<uri>&code_challenge=<cc>&code_challenge_method=S256&state=<s>`.
2. Authentication against the IdP (lab convenience: `login=<account>` hint for
   browser-less use).
3. Return `GET /callback?code=<code>&state=<s>`; the SPA exchanges the code:
   `POST /token` (grant_type=authorization_code, `code_verifier`) → JWT `access_token`.
4. API calls carry `Authorization: Bearer <jwt>`.

## API surface (excerpt)

- `GET /api/portfolios`, `GET /api/portfolios/{id}`, `POST /api/portfolios`
- `POST /api/orders`, `GET /api/orders`, `GET /api/orders/{id}`, `POST /api/orders/{id}/cancel`
- `GET /api/admin/orders` (back-office)
- `GET /api/instruments?q=`
- **Reference secured** routes: `/api/v2/portfolios`, `/api/v2/portfolios/{id}`,
  `/api/v2/orders`, `/api/v2/instruments` (negative controls).
- OIDC: `GET /authorize`, `GET /signin-oidc`, `POST /token`, `/.well-known/openid-configuration`.

## Elastic Beanstalk deployment (Python platform)

- `Procfile`: `web: uvicorn main:app --host 0.0.0.0 --port 5000`.
- `.ebextensions/01-options.config`: **environment variables only**
  (instance type, HTTPS/ALB managed by the `eb` CLI — no deprecated AL2023 options).
- `.ebignore`: excludes `benchmark/` and sensitive documents from the deployed bundle.

## Documentation

- `ARCHITECTURE.md`, `USER_GUIDE.md`, `SECURITY_MODEL.md`.
- `benchmark/ground-truth.yaml`, `VULNERABILITY_GUIDE.md`, `negative-controls.yaml`
  (**scoring oracle — never exposed over HTTP, never provided to scanners**).
- `benchmark/validation/validate.py`: replays the 22 vulnerabilities + 12 NC and produces
  JSON/HTML reports.
