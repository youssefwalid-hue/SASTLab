> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-10 — OnboardR

**Parcours d'entree en relation (KYC)** volontairement vulnerable, pour le
**Banking DAST Benchmark Lab**. Architecture **microservices** derriere un **reverse proxy Nginx dedie**.

> ⚠️ **Laboratoire de securite defensive.** Donnees **100 % fictives**, aucun secret reel.
> Securise par defaut : seules **21 vulnerabilites** volontaires (APP10-V001..V021) y sont
> implementees comme ecarts localises, avec **13 controles negatifs** (routes sures jumelles,
> APP10-N001..N007). Usage exclusif : test de scanners DAST en environnement interne isole.

## Architecture

```
Client/Scanner ──HTTP──> [ proxy Nginx DEDIE ] ──> kyc-internal (Java/Spring Boot, :8080)
                                │                └─> documents    (Node/NestJS, :3000)
                                └─ porte les ecarts de couche Reverse Proxy
```

- **proxy/** — reverse proxy Nginx **dedie** (`nginx.conf`). Porte les ecarts de couche
  Reverse Proxy : V001 (passthrough en-tete de confiance), V002 (X-Forwarded-* non reecrits),
  V003 (filtrage avant normalisation), V005 (CL.TE), V011 (en-tetes internes non masques),
  V012 (cache sans cle sur en-tete), V014 (en-tetes de securite absents), V015 (statique mal ancre).
- **onboarding-service/** — service d'onboarding KYC **Java / Spring Boot** (hote `kyc-internal`).
  Parcours KYC, dossier, verification OTP, decision, routes internes, `/admin/reset`.
- **documents-service/** — service documents **Node / NestJS**. Upload de pieces, recuperation
  de piece par URL, selfie, enrichissement.
- En-tete `X-Lab-Environment: test-data-fictives` sur toutes les reponses.

## Demarrage local

```bash
docker compose up --build        # proxy -> http://localhost:8080
```

Reinitialiser l'etat de demonstration : `POST /admin/reset` (reseed les deux services).

> Les microservices `kyc-internal` et `documents` ne sont **jamais publies** : seul le proxy
> est expose. La SSRF (V004) demontre l'atteinte des services internes depuis le reseau Docker.

## Acteurs et donnees fictives

- Prospects : `prospect1`, `prospect2` (role PROSPECT).
- Conformite : `conformite1` (role CONFORMITE).
- Mot de passe de labo : `Password1!`. Code OTP de labo : `123456`.
- Dossiers seedes : `1001` (prospect1), `1002` (prospect2).

## Endpoints principaux

| Methode | Endpoint | Role |
|---------|----------|------|
| POST | `/login` | authentification (cookie `OBRSESSION` signe) |
| POST | `/admin/reset` | reinitialisation d'etat (cascade documents) |
| GET  | `/api/onboarding` | liste des dossiers |
| POST | `/api/onboarding` | creation d'un dossier |
| GET/PUT | `/api/onboarding/{id}` | consultation / mise a jour |
| PUT | `/api/onboarding/{id}/contact` | mise a jour de coordonnees (DTO restreint) |
| POST | `/api/onboarding/{id}/verify` | verification OTP |
| POST/GET | `/api/onboarding/{id}/documents` | depot / liste de pieces |
| POST | `/api/onboarding/{id}/selfie` | depot de selfie (type valide) |
| POST | `/api/documents/fetch` | recuperation d'une piece par URL |
| GET  | `/api/documents/{docId}` | piece par id (propriete verifiee) |
| POST | `/api/enrichment/company-logo` | enrichissement (allowlist) |
| POST | `/api/support/messages` | messagerie support |
| GET  | `/api/config` | configuration publique (mise en cache) |
| GET  | `/api/redirect?target=` | redirection d'etape |
| POST | `/api/admin/onboarding/{id}/decision` | decision de conformite |
| POST | `/api/admin/onboarding/{id}/comment` | annotation (role CONFORMITE) |
| GET  | `/api/admin/reports` | rapports (role CONFORMITE) |
| GET  | `/internal/kyc[/applications]` | routes internes KYC |
| GET  | `/internal/kyc/metrics` | metriques internes (secret de passerelle) |

## Deploiement Elastic Beanstalk (Docker, Amazon Linux 2023)

La plateforme **Docker AL2023** lit nativement `docker-compose.yml`. L'ALB (listener HTTPS 443
+ redirection 80→443, certificat ACM) route vers le conteneur **proxy** (port 80).

- `.ebextensions/01-options.config` : **variables d'environnement UNIQUEMENT** (aucune option
  depreciee/`ProxyServer` — lecon AL2023). Type d'instance, single/ALB et HTTPS geres via `eb`/console.
- `.ebignore` : exclut `benchmark/` et la documentation de labo du bundle.

## Benchmark

- `benchmark/ground-truth.yaml` — verite terrain des 21 vulnerabilites (**ne jamais exposer**).
- `benchmark/validation/validate.py` — validation automatisee (21 vulns + 7 NC, rapports json/html).
- `VULNERABILITY_GUIDE.md`, `negative-controls.yaml`, `SECURITY_MODEL.md` — references de labo,
  **jamais servies par HTTP**.

---

# English version

# APP-10 — OnboardR

**Intentionally vulnerable client onboarding (KYC) journey**, for the
**Banking DAST Benchmark Lab**. **Microservices** architecture behind a **dedicated Nginx reverse proxy**.

> ⚠️ **Defensive security laboratory.** **100% fictitious** data, no real secrets.
> Secure by default: only **15 intentional vulnerabilities** (APP10-V001..V021) are
> implemented as localized deviations, with **13 negative controls** (twin safe routes,
> APP10-N001..N007). Exclusive use: testing DAST scanners in an isolated internal environment.

## Architecture

```
Client/Scanner ──HTTP──> [ proxy Nginx DEDIE ] ──> kyc-internal (Java/Spring Boot, :8080)
                                │                └─> documents    (Node/NestJS, :3000)
                                └─ porte les ecarts de couche Reverse Proxy
```

- **proxy/** — **dedicated** Nginx reverse proxy (`nginx.conf`). Carries the Reverse Proxy layer
  deviations: V001 (trust-header passthrough), V002 (X-Forwarded-* not rewritten),
  V003 (filtering before normalization), V005 (CL.TE), V011 (internal headers not masked),
  V012 (header-unkeyed cache), V014 (missing security headers), V015 (poorly anchored static).
- **onboarding-service/** — KYC onboarding service in **Java / Spring Boot** (host `kyc-internal`).
  KYC journey, dossier, OTP verification, decision, internal routes, `/admin/reset`.
- **documents-service/** — documents service in **Node / NestJS**. Document upload, document
  retrieval by URL, selfie, enrichment.
- `X-Lab-Environment: test-data-fictives` header on every response.

## Local startup

```bash
docker compose up --build        # proxy -> http://localhost:8080
```

Reset the demo state: `POST /admin/reset` (reseeds both services).

> The `kyc-internal` and `documents` microservices are **never published**: only the proxy
> is exposed. SSRF (V004) demonstrates reaching internal services from the Docker network.

## Actors and fictitious data

- Prospects: `prospect1`, `prospect2` (PROSPECT role).
- Compliance: `conformite1` (CONFORMITE role).
- Lab password: `Password1!`. Lab OTP code: `123456`.
- Seeded dossiers: `1001` (prospect1), `1002` (prospect2).

## Main endpoints

| Method | Endpoint | Role |
|---------|----------|------|
| POST | `/login` | authentication (signed `OBRSESSION` cookie) |
| POST | `/admin/reset` | state reset (documents cascade) |
| GET  | `/api/onboarding` | list of dossiers |
| POST | `/api/onboarding` | create a dossier |
| GET/PUT | `/api/onboarding/{id}` | read / update |
| PUT | `/api/onboarding/{id}/contact` | update contact details (restricted DTO) |
| POST | `/api/onboarding/{id}/verify` | OTP verification |
| POST/GET | `/api/onboarding/{id}/documents` | upload / list documents |
| POST | `/api/onboarding/{id}/selfie` | selfie upload (validated type) |
| POST | `/api/documents/fetch` | retrieve a document by URL |
| GET  | `/api/documents/{docId}` | document by id (ownership verified) |
| POST | `/api/enrichment/company-logo` | enrichment (allowlist) |
| POST | `/api/support/messages` | support messaging |
| GET  | `/api/config` | public configuration (cached) |
| GET  | `/api/redirect?target=` | step redirect |
| POST | `/api/admin/onboarding/{id}/decision` | compliance decision |
| POST | `/api/admin/onboarding/{id}/comment` | annotation (CONFORMITE role) |
| GET  | `/api/admin/reports` | reports (CONFORMITE role) |
| GET  | `/internal/kyc[/applications]` | internal KYC routes |
| GET  | `/internal/kyc/metrics` | internal metrics (gateway secret) |

## Elastic Beanstalk deployment (Docker, Amazon Linux 2023)

The **Docker AL2023** platform natively reads `docker-compose.yml`. The ALB (HTTPS 443 listener
+ 80→443 redirect, ACM certificate) routes to the **proxy** container (port 80).

- `.ebextensions/01-options.config`: **environment variables ONLY** (no deprecated option /
  `ProxyServer` — AL2023 lesson). Instance type, single/ALB and HTTPS managed via `eb`/console.
- `.ebignore`: excludes `benchmark/` and the lab documentation from the bundle.

## Benchmark

- `benchmark/ground-truth.yaml` — ground truth of the 21 vulnerabilities (**never expose**).
- `benchmark/validation/validate.py` — automated validation (21 vulns + 7 NCs, json/html reports).
- `VULNERABILITY_GUIDE.md`, `negative-controls.yaml`, `SECURITY_MODEL.md` — lab references,
  **never served over HTTP**.
