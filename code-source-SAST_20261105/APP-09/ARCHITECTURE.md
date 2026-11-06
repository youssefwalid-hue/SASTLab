> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# Novabourse — Document d'architecture

> **Environnement de test – données fictives.** Novabourse (Portail titres) est une application de laboratoire
> du Banking DAST Benchmark Lab. Toutes les données sont fictives ; aucune marque réelle n'est utilisée.

## 1. Vue d'ensemble

- **Finalité** : Connexion OIDC PKCE, portefeuille, passage et suivi d'ordres, avis d'opéré.
- **Pile technique** : Python 3.12, FastAPI, SPA React.
- **Protocole principal** : SPA + REST (OpenAPI).
- **Hôte de laboratoire** : `https://novabourse.lab.exemple` (HTTPS, HTTP 80 redirigé en 443).
- **Thème de sécurité** : OIDC PKCE + API titres : IDOR portefeuille, redirect_uri/state faibles, BOLA passage d'ordre.

## 2. Architecture AWS (HTTPS, sans filtrage IP, moindre coût)

```mermaid
flowchart LR
  SC["Scanner / client Internet"] -->|HTTPS 443| ALB["ALB + certificat ACM<br/>terminaison TLS"]
  H80["HTTP 80"] -. redirection 308 .-> ALB
  ALB --> EB["Elastic Beanstalk<br/>Python · instance unique"]
  EB --> DB[("RDS PostgreSQL")]
  EB --> R[("ElastiCache Redis")]
```

- **Elastic Beanstalk** en **instance unique** (`SingleInstance`), runtime **Python**.
- **HTTPS** via **ALB mutualisé + certificat ACM** ; redirection HTTP 80 → 443.
- **Accès ouvert sans filtrage IP** : Security Group entrant `0.0.0.0/0` sur 443 (et 80), compensé par
  données fictives, arrêt hors campagne, rôle IAM minimal et réseau sortant restreint.
- **Ressources** : instance t3.small, RDS PostgreSQL, ElastiCache Redis.
- **Moindre coût** : mono-AZ, pas d'auto-scaling, pas de NAT Gateway, environnement arrêté hors fenêtres de scan
  (`deploy/30-start.sh` / `31-stop.sh`), suppression complète en fin de projet (`deploy/90-destroy.sh`).

## 3. Architecture logique et composants

- SPA React
- API REST FastAPI (OpenAPI)
- Authentification OIDC avec PKCE via l'IdP du laboratoire
- Service de portefeuille et de passage d'ordres
- Cache Redis
- Back-office (endpoints d'administration)

## 4. Flux réseau

1. Le client (ou le scanner) atteint l'**ALB** en **HTTPS (443)** ; tout trafic **HTTP (80)** est redirigé en 443.
2. L'ALB transmet à l'instance **Elastic Beanstalk** (Python).
3. L'application accède à **RDS PostgreSQL**, au cache **Redis**.
4. Les en-têtes de sécurité (HSTS, CSP, cookies durcis, suppression de bannière) sont gérés par l'application
   et le reverse proxy — certains écarts volontaires constituent des vulnérabilités du corpus.

## 5. Modèle d'authentification

OIDC avec PKCE via l'IdP du laboratoire ; jetons porteurs.

## 6. Modèle de données (entités principales)

- Investisseur
- Portefeuille
- Instrument
- Ordre (état)
- Avis d'opéré

## 7. Surface de sécurité

L'application est **sécurisée par défaut** ; les **22 vulnérabilités** du référentiel sont des **écarts localisés**
(voir `VULNERABILITY_GUIDE.md` et `ground-truth.yaml`, non exposés par HTTP). Des **negative controls**
fournissent des routes comparables correctement sécurisées. Couche d'accent : **OIDC PKCE + API titres **.

## 8. Déploiement et exploitation

- Déploiement : `deploy/20-deploy-eb.sh` (environnement EB + listener HTTPS 443 + redirection 80).
- Démarrage / arrêt hors campagne : `deploy/30-start.sh` / `deploy/31-stop.sh`.
- Suppression complète : `deploy/90-destroy.sh`.
- Variables d'environnement injectées au déploiement ; aucun secret dans le dépôt.

## 9. Rappels de laboratoire

- Bandeau permanent « Environnement de test – données fictives » sur toutes les pages.
- Réinitialisation de l'état de démonstration : `POST /admin/reset`.
- `VULNERABILITY_GUIDE.md`, `negative-controls.yaml` et `ground-truth.yaml` ne sont **jamais exposés par HTTP**.

---

# English version

# Novabourse — Architecture document

> **Test environment – fictitious data.** Novabourse (Securities Portal) is a lab application
> of the Banking DAST Benchmark Lab. All data is fictitious; no real brand is used.

## 1. Overview

- **Purpose**: OIDC PKCE sign-in, portfolio, order placement and tracking, trade confirmation.
- **Technical stack**: Python 3.12, FastAPI, React SPA.
- **Main protocol**: SPA + REST (OpenAPI).
- **Lab host**: `https://novabourse.lab.exemple` (HTTPS, HTTP 80 redirected to 443).
- **Security theme**: OIDC PKCE + securities API: portfolio IDOR, weak redirect_uri/state, order-placement BOLA.

## 2. AWS architecture (HTTPS, no IP filtering, lowest cost)

```mermaid
flowchart LR
  SC["Scanner / client Internet"] -->|HTTPS 443| ALB["ALB + certificat ACM<br/>terminaison TLS"]
  H80["HTTP 80"] -. redirection 308 .-> ALB
  ALB --> EB["Elastic Beanstalk<br/>Python · instance unique"]
  EB --> DB[("RDS PostgreSQL")]
  EB --> R[("ElastiCache Redis")]
```

- **Elastic Beanstalk** as a **single instance** (`SingleInstance`), **Python** runtime.
- **HTTPS** via **shared ALB + ACM certificate**; HTTP 80 → 443 redirection.
- **Open access without IP filtering**: inbound Security Group `0.0.0.0/0` on 443 (and 80), offset by
  fictitious data, shutdown outside campaigns, minimal IAM role and restricted outbound network.
- **Resources**: t3.small instance, RDS PostgreSQL, ElastiCache Redis.
- **Lowest cost**: single-AZ, no auto-scaling, no NAT Gateway, environment stopped outside scan windows
  (`deploy/30-start.sh` / `31-stop.sh`), full teardown at project end (`deploy/90-destroy.sh`).

## 3. Logical architecture and components

- React SPA
- FastAPI REST API (OpenAPI)
- OIDC authentication with PKCE via the lab IdP
- Portfolio and order-placement service
- Redis cache
- Back-office (administration endpoints)

## 4. Network flow

1. The client (or scanner) reaches the **ALB** over **HTTPS (443)**; all **HTTP (80)** traffic is redirected to 443.
2. The ALB forwards to the **Elastic Beanstalk** instance (Python).
3. The application accesses **RDS PostgreSQL** and the **Redis** cache.
4. Security headers (HSTS, CSP, hardened cookies, banner suppression) are managed by the application
   and the reverse proxy — some intentional deviations constitute corpus vulnerabilities.

## 5. Authentication model

OIDC with PKCE via the lab IdP; bearer tokens.

## 6. Data model (main entities)

- Investor
- Portfolio
- Instrument
- Order (state)
- Trade confirmation

## 7. Security surface

The application is **secure by default**; the **22 vulnerabilities** of the corpus are **localized deviations**
(see `VULNERABILITY_GUIDE.md` and `ground-truth.yaml`, not exposed over HTTP). **Negative controls**
provide comparable, correctly secured routes. Accent layer: **OIDC PKCE + securities API **.

## 8. Deployment and operations

- Deployment: `deploy/20-deploy-eb.sh` (EB environment + HTTPS 443 listener + 80 redirection).
- Start / stop outside campaigns: `deploy/30-start.sh` / `deploy/31-stop.sh`.
- Full teardown: `deploy/90-destroy.sh`.
- Environment variables injected at deployment; no secret in the repository.

## 9. Lab reminders

- Permanent "Test environment – fictitious data" banner on all pages.
- Demo state reset: `POST /admin/reset`.
- `VULNERABILITY_GUIDE.md`, `negative-controls.yaml` and `ground-truth.yaml` are **never exposed over HTTP**.
