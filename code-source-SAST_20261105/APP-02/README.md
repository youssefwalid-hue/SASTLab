> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-02 — Agréga Open

**API d'agrégation de comptes (Open Banking)** volontairement vulnérable, pour le
**Banking DAST Benchmark Lab**. Pendant API d'APP-01 (Hélios Banque).

> ⚠️ **Laboratoire de sécurité défensive.** Données **100 % fictives**, aucun secret réel.
> Sécurisée par défaut : seules **22 vulnérabilités** volontaires (APP02-V001..V022) y sont
> implémentées comme écarts localisés, avec **14 contrôles négatifs** (routes sûres jumelles).
> Usage exclusif : test de scanners DAST en environnement interne isolé.

## Pile

- **Node.js 20 / NestJS / TypeScript**, API REST documentée **OpenAPI 3** (`/docs`).
- **IdP de laboratoire intégré** : paire RSA de test générée au boot, JWT **RS256**,
  `kid` publié sur `GET /.well-known/jwks.json`.
- Authentification : **OAuth2 client_credentials** (JWT bearer, scopes) + **clés d'API** TPP.
- Stockage **en mémoire** par défaut (seed au démarrage), derrière une abstraction
  (`LabStore`) permettant une implémentation **PostgreSQL + Redis** en profil `prod`.
- En-tête `X-Lab-Environment: test-data-fictives` sur toutes les réponses.

## Profils

| Profil | Stockage | Port | Usage |
|--------|----------|------|-------|
| `local` | mémoire | 8080 | développement / validation (cible du benchmark) |
| `standalone` | mémoire | 5000 | déploiement EB immédiat sans base externe (défaut EB) |
| `prod` | PostgreSQL + Redis | 5000 | `DB_URL` / `REDIS_URL` requis |

## Démarrage local

```bash
npm install
APP_PROFILE=local npm start        # http://localhost:8080  — docs: /docs
```

Réinitialiser l'état de démonstration : `POST /admin/reset`.

## Acteurs et données fictives

- Clients OAuth (TPP) : `tpp-a`, `tpp-b` — secret de labo `Password1!`.
- Utilisateurs sous-jacents : `alice` (`ACC-FR-1001`), `bruno` (`ACC-FR-2001`).
- `admin` (administration des consentements).
- Clés d'API : `AGK-LAB-tppa-0001`, `AGK-LAB-tppb-0001` (FICTIVES).
- Scopes : `accounts:read`, `payments:read`, `consents:write`.

Obtenir un jeton :

```bash
curl -X POST http://localhost:8080/oauth/token \
  -d 'grant_type=client_credentials&client_id=tpp-a&client_secret=Password1!&scope=accounts:read'
```

## Endpoints

| Méthode | Chemin | Rôle |
|--------|--------|------|
| POST | `/oauth/token` | émission de jeton (client_credentials) |
| GET | `/.well-known/jwks.json` | clés publiques de l'IdP |
| GET | `/v1/accounts` · `/v1/accounts/{accountId}` | comptes |
| GET | `/v1/payments` | paiements |
| POST | `/v1/consents` · `/v1/admin/consents` | consentements |
| GET | `/v1/exports/{exportId}` | export agrégé |
| GET | `/v1/health` | santé |
| GET | `/v1-internal/accounts` | version interne héritée |
| GET | `/debug/requestlog` | journal de diagnostic |
| POST | `/admin/reset` | réinitialisation (labo) |
| — | `/docs` | OpenAPI / Swagger |
| — | `/v1/secure/*` | **contrôles négatifs** (routes sûres) |

## Déploiement Elastic Beanstalk (plateforme Node.js, AL2023)

- `.ebextensions/01-options.config` : **variables d'environnement uniquement** (leçon
  APP-01 — pas d'option dépréciée). Profil `standalone`, `PORT=5000`.
- `Procfile` : `web: node dist/main.js`.
- Le build (`npm run build` → `dist/`) est exécuté par la plateforme (`npm start` lance `prestart`).

## Vérité terrain & validation

- `benchmark/ground-truth.yaml` : les 22 vulnérabilités au schéma complet.
- `benchmark/validation/validate.py` : rejoue chaque vuln + chaque NC, produit un rapport.

> `benchmark/` **ne doit jamais être servi par HTTP ni fourni à un scanner** (oracle de
> notation). Il est exclu du paquet de déploiement (`.ebignore`).

Voir `SECURITY_MODEL.md` pour la cartographie détaillée des écarts et des contrôles négatifs.

---

# English version

# APP-02 — Agréga Open

Intentionally vulnerable **account aggregation API (Open Banking)** for the
**Banking DAST Benchmark Lab**. API counterpart of APP-01 (Hélios Banque).

> ⚠️ **Defensive security laboratory.** **100 % fictitious** data, no real secrets.
> Secure by default: only **16 intentional vulnerabilities** (APP02-V001..V022) are
> implemented as localized deviations, with **14 negative controls** (twin safe routes).
> Exclusive use: testing DAST scanners in an isolated internal environment.

## Stack

- **Node.js 20 / NestJS / TypeScript**, REST API documented with **OpenAPI 3** (`/docs`).
- **Embedded lab IdP**: test RSA key pair generated at boot, **RS256** JWT,
  `kid` published on `GET /.well-known/jwks.json`.
- Authentication: **OAuth2 client_credentials** (JWT bearer, scopes) + TPP **API keys**.
- **In-memory** storage by default (seeded at startup), behind an abstraction
  (`LabStore`) that allows a **PostgreSQL + Redis** implementation in the `prod` profile.
- `X-Lab-Environment: test-data-fictives` header on all responses.

## Profiles

| Profile | Storage | Port | Use |
|--------|----------|------|-------|
| `local` | memory | 8080 | development / validation (benchmark target) |
| `standalone` | memory | 5000 | immediate EB deployment without external database (EB default) |
| `prod` | PostgreSQL + Redis | 5000 | `DB_URL` / `REDIS_URL` required |

## Local startup

```bash
npm install
APP_PROFILE=local npm start        # http://localhost:8080  — docs: /docs
```

Reset the demo state: `POST /admin/reset`.

## Actors and fictitious data

- OAuth clients (TPP): `tpp-a`, `tpp-b` — lab secret `Password1!`.
- Underlying users: `alice` (`ACC-FR-1001`), `bruno` (`ACC-FR-2001`).
- `admin` (consent administration).
- API keys: `AGK-LAB-tppa-0001`, `AGK-LAB-tppb-0001` (FICTITIOUS).
- Scopes: `accounts:read`, `payments:read`, `consents:write`.

Obtain a token:

```bash
curl -X POST http://localhost:8080/oauth/token \
  -d 'grant_type=client_credentials&client_id=tpp-a&client_secret=Password1!&scope=accounts:read'
```

## Endpoints

| Method | Path | Role |
|--------|--------|------|
| POST | `/oauth/token` | token issuance (client_credentials) |
| GET | `/.well-known/jwks.json` | IdP public keys |
| GET | `/v1/accounts` · `/v1/accounts/{accountId}` | accounts |
| GET | `/v1/payments` | payments |
| POST | `/v1/consents` · `/v1/admin/consents` | consents |
| GET | `/v1/exports/{exportId}` | aggregated export |
| GET | `/v1/health` | health |
| GET | `/v1-internal/accounts` | legacy internal version |
| GET | `/debug/requestlog` | diagnostic log |
| POST | `/admin/reset` | reset (lab) |
| — | `/docs` | OpenAPI / Swagger |
| — | `/v1/secure/*` | **negative controls** (safe routes) |

## Elastic Beanstalk deployment (Node.js platform, AL2023)

- `.ebextensions/01-options.config`: **environment variables only** (APP-01
  lesson — no deprecated option). `standalone` profile, `PORT=5000`.
- `Procfile`: `web: node dist/main.js`.
- The build (`npm run build` → `dist/`) is run by the platform (`npm start` triggers `prestart`).

## Ground truth & validation

- `benchmark/ground-truth.yaml`: the 22 vulnerabilities in full schema.
- `benchmark/validation/validate.py`: replays each vuln + each NC, produces a report.

> `benchmark/` **must never be served over HTTP nor provided to a scanner** (scoring
> oracle). It is excluded from the deployment package (`.ebignore`).

See `SECURITY_MODEL.md` for the detailed mapping of deviations and negative controls.
