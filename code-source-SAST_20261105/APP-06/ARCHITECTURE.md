> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# MobiPay — Document d'architecture

> **Environnement de test – données fictives.** MobiPay (API mobile (GraphQL)) est une application de laboratoire
> du Banking DAST Benchmark Lab. Toutes les données sont fictives ; aucune marque réelle n'est utilisée.

## 1. Vue d'ensemble

- **Finalité** : Endpoint GraphQL unique, requêtes comptes/cartes/notifications, mutations profil et OTP.
- **Pile technique** : Go 1.22, gqlgen.
- **Protocole principal** : GraphQL — API pure.
- **Hôte de laboratoire** : `https://mobipay.lab.exemple` (HTTPS, HTTP 80 redirigé en 443).
- **Thème de sécurité** : GraphQL : BOLA via node(id), exposition de champs, introspection, coût/profondeur, batching OTP.

## 2. Architecture AWS (HTTPS, sans filtrage IP, moindre coût)

```mermaid
flowchart LR
  SC["Scanner / client Internet"] -->|HTTPS 443| ALB["ALB + certificat ACM<br/>terminaison TLS"]
  H80["HTTP 80"] -. redirection 308 .-> ALB
  ALB --> EB["Elastic Beanstalk<br/>Go · instance unique"]
  EB --> DB[("RDS PostgreSQL")]
  EB --> R[("ElastiCache Redis")]
```

- **Elastic Beanstalk** en **instance unique** (`SingleInstance`), runtime **Go**.
- **HTTPS** via **ALB mutualisé + certificat ACM** ; redirection HTTP 80 → 443.
- **Accès ouvert sans filtrage IP** : Security Group entrant `0.0.0.0/0` sur 443 (et 80), compensé par
  données fictives, arrêt hors campagne, rôle IAM minimal et réseau sortant restreint.
- **Ressources** : instance t3.small, RDS PostgreSQL, ElastiCache Redis.
- **Moindre coût** : mono-AZ, pas d'auto-scaling, pas de NAT Gateway, environnement arrêté hors fenêtres de scan
  (`deploy/30-start.sh` / `31-stop.sh`), suppression complète en fin de projet (`deploy/90-destroy.sh`).

## 3. Architecture logique et composants

- Serveur GraphQL Go (gqlgen), endpoint unique /graphql
- Résolveurs (query/mutation)
- Authentification JWT + empreinte d'appareil
- Service comptes / cartes / notifications
- Cache Redis
- Requêtes persistées

## 4. Flux réseau

1. Le client (ou le scanner) atteint l'**ALB** en **HTTPS (443)** ; tout trafic **HTTP (80)** est redirigé en 443.
2. L'ALB transmet à l'instance **Elastic Beanstalk** (Go).
3. L'application accède à **RDS PostgreSQL**, au cache **Redis**.
4. Les en-têtes de sécurité (HSTS, CSP, cookies durcis, suppression de bannière) sont gérés par l'application
   et le reverse proxy — certains écarts volontaires constituent des vulnérabilités du corpus.

## 5. Modèle d'authentification

JWT porteur, empreinte d'appareil (device fingerprint) transmise par le client.

## 6. Modèle de données (entités principales)

- Utilisateur mobile
- Compte
- Carte (PAN/CVV)
- Transaction
- Notification
- Profil

## 7. Surface de sécurité

L'application est **sécurisée par défaut** ; les **22 vulnérabilités** du référentiel sont des **écarts localisés**
(voir `VULNERABILITY_GUIDE.md` et `ground-truth.yaml`, non exposés par HTTP). Des **negative controls**
fournissent des routes comparables correctement sécurisées. Couche d'accent : **GraphQL **.

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

# MobiPay — Architecture document

> **Test environment – fictitious data.** MobiPay (GraphQL mobile API) is a lab application
> of the Banking DAST Benchmark Lab. All data is fictitious; no real brand is used.

## 1. Overview

- **Purpose**: Single GraphQL endpoint, account/card/notification queries, profile and OTP mutations.
- **Technology stack**: Go 1.22, gqlgen.
- **Main protocol**: GraphQL — pure API.
- **Lab host**: `https://mobipay.lab.exemple` (HTTPS, HTTP 80 redirected to 443).
- **Security theme**: GraphQL: BOLA via node(id), field exposure, introspection, cost/depth, OTP batching.

## 2. AWS architecture (HTTPS, no IP filtering, least cost)

```mermaid
flowchart LR
  SC["Scanner / client Internet"] -->|HTTPS 443| ALB["ALB + certificat ACM<br/>terminaison TLS"]
  H80["HTTP 80"] -. redirection 308 .-> ALB
  ALB --> EB["Elastic Beanstalk<br/>Go · instance unique"]
  EB --> DB[("RDS PostgreSQL")]
  EB --> R[("ElastiCache Redis")]
```

- **Elastic Beanstalk** as a **single instance** (`SingleInstance`), **Go** runtime.
- **HTTPS** via **shared ALB + ACM certificate**; HTTP 80 → 443 redirection.
- **Open access without IP filtering**: inbound Security Group `0.0.0.0/0` on 443 (and 80), offset by
  fictitious data, shutdown outside campaigns, minimal IAM role and restricted outbound network.
- **Resources**: t3.small instance, RDS PostgreSQL, ElastiCache Redis.
- **Least cost**: single-AZ, no auto-scaling, no NAT Gateway, environment stopped outside scan windows
  (`deploy/30-start.sh` / `31-stop.sh`), full teardown at end of project (`deploy/90-destroy.sh`).

## 3. Logical architecture and components

- Go GraphQL server (gqlgen), single endpoint /graphql
- Resolvers (query/mutation)
- JWT authentication + device fingerprint
- Account / card / notification service
- Redis cache
- Persisted queries

## 4. Network flow

1. The client (or scanner) reaches the **ALB** over **HTTPS (443)**; all **HTTP (80)** traffic is redirected to 443.
2. The ALB forwards to the **Elastic Beanstalk** instance (Go).
3. The application accesses **RDS PostgreSQL** and the **Redis** cache.
4. The security headers (HSTS, CSP, hardened cookies, banner suppression) are handled by the application
   and the reverse proxy — some intentional deviations constitute vulnerabilities of the corpus.

## 5. Authentication model

Bearer JWT, device fingerprint transmitted by the client.

## 6. Data model (main entities)

- Mobile user
- Account
- Card (PAN/CVV)
- Transaction
- Notification
- Profile

## 7. Security surface

The application is **secure by default**; the **22 vulnerabilities** of the reference set are **localized deviations**
(see `VULNERABILITY_GUIDE.md` and `ground-truth.yaml`, not exposed over HTTP). **Negative controls**
provide comparable, correctly secured routes. Focus layer: **GraphQL **.

## 8. Deployment and operations

- Deployment: `deploy/20-deploy-eb.sh` (EB environment + HTTPS 443 listener + 80 redirection).
- Start / stop outside campaigns: `deploy/30-start.sh` / `deploy/31-stop.sh`.
- Full teardown: `deploy/90-destroy.sh`.
- Environment variables injected at deployment; no secret in the repository.

## 9. Lab reminders

- Permanent "Test environment – fictitious data" banner on every page.
- Demo state reset: `POST /admin/reset`.
- `VULNERABILITY_GUIDE.md`, `negative-controls.yaml` and `ground-truth.yaml` are **never exposed over HTTP**.
