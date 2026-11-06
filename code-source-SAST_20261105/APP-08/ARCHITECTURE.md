> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# CartaVie — Document d'architecture

> **Environnement de test – données fictives.** CartaVie (Gestion des cartes) est une application de laboratoire
> du Banking DAST Benchmark Lab. Toutes les données sont fictives ; aucune marque réelle n'est utilisée.

## 1. Vue d'ensemble

- **Finalité** : Gestion des plafonds, opposition, création de carte virtuelle, contestations, API REST.
- **Pile technique** : Ruby 3.3, Ruby on Rails.
- **Protocole principal** : Web + REST JSON (OpenAPI).
- **Hôte de laboratoire** : `https://cartavie.lab.exemple` (HTTPS, HTTP 80 redirigé en 443).
- **Thème de sécurité** : Mixte Web/REST : mass assignment des plafonds, IDOR carte virtuelle, clé d'API rejouable.

## 2. Architecture AWS (HTTPS, sans filtrage IP, moindre coût)

```mermaid
flowchart LR
  SC["Scanner / client Internet"] -->|HTTPS 443| ALB["ALB + certificat ACM<br/>terminaison TLS"]
  H80["HTTP 80"] -. redirection 308 .-> ALB
  ALB --> EB["Elastic Beanstalk<br/>Ruby · instance unique"]
  EB --> DB[("RDS PostgreSQL")]
```

- **Elastic Beanstalk** en **instance unique** (`SingleInstance`), runtime **Ruby**.
- **HTTPS** via **ALB mutualisé + certificat ACM** ; redirection HTTP 80 → 443.
- **Accès ouvert sans filtrage IP** : Security Group entrant `0.0.0.0/0` sur 443 (et 80), compensé par
  données fictives, arrêt hors campagne, rôle IAM minimal et réseau sortant restreint.
- **Ressources** : instance t3.micro, RDS PostgreSQL.
- **Moindre coût** : mono-AZ, pas d'auto-scaling, pas de NAT Gateway, environnement arrêté hors fenêtres de scan
  (`deploy/30-start.sh` / `31-stop.sh`), suppression complète en fin de projet (`deploy/90-destroy.sh`).

## 3. Architecture logique et composants

- Frontend SSR ERB (Rails)
- Contrôleurs Rails (web + API REST JSON)
- Authentification par formulaire + session ; clé d'API pour le REST
- Service de gestion des plafonds et oppositions
- Service de cartes virtuelles et contestations

## 4. Flux réseau

1. Le client (ou le scanner) atteint l'**ALB** en **HTTPS (443)** ; tout trafic **HTTP (80)** est redirigé en 443.
2. L'ALB transmet à l'instance **Elastic Beanstalk** (Ruby).
3. L'application accède à **RDS PostgreSQL**.
4. Les en-têtes de sécurité (HSTS, CSP, cookies durcis, suppression de bannière) sont gérés par l'application
   et le reverse proxy — certains écarts volontaires constituent des vulnérabilités du corpus.

## 5. Modèle d'authentification

Formulaire + session pour le web ; clé d'API (X-API-Key) pour les endpoints REST.

## 6. Modèle de données (entités principales)

- Client
- Gestionnaire
- Carte (plafond, statut)
- Carte virtuelle
- Contestation

## 7. Surface de sécurité

L'application est **sécurisée par défaut** ; les **22 vulnérabilités** du référentiel sont des **écarts localisés**
(voir `VULNERABILITY_GUIDE.md` et `ground-truth.yaml`, non exposés par HTTP). Des **negative controls**
fournissent des routes comparables correctement sécurisées. Couche d'accent : **Mixte Web/REST **.

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

# CartaVie — Architecture document

> **Test environment – fictitious data.** CartaVie (Card Management) is a lab application
> of the Banking DAST Benchmark Lab. All data is fictitious; no real brand is used.

## 1. Overview

- **Purpose**: Management of limits, card blocking, virtual-card creation, disputes, REST API.
- **Tech stack**: Ruby 3.3, Ruby on Rails.
- **Main protocol**: Web + REST JSON (OpenAPI).
- **Lab host**: `https://cartavie.lab.exemple` (HTTPS, HTTP 80 redirected to 443).
- **Security theme**: Mixed Web/REST: mass assignment of limits, virtual-card IDOR, replayable API key.

## 2. AWS architecture (HTTPS, no IP filtering, lowest cost)

```mermaid
flowchart LR
  SC["Scanner / client Internet"] -->|HTTPS 443| ALB["ALB + certificat ACM<br/>terminaison TLS"]
  H80["HTTP 80"] -. redirection 308 .-> ALB
  ALB --> EB["Elastic Beanstalk<br/>Ruby · instance unique"]
  EB --> DB[("RDS PostgreSQL")]
```

- **Elastic Beanstalk** as a **single instance** (`SingleInstance`), **Ruby** runtime.
- **HTTPS** via **shared ALB + ACM certificate**; HTTP 80 → 443 redirect.
- **Open access without IP filtering**: inbound Security Group `0.0.0.0/0` on 443 (and 80), offset by
  fictitious data, shutdown outside campaigns, minimal IAM role and restricted outbound network.
- **Resources**: t3.micro instance, RDS PostgreSQL.
- **Lowest cost**: single-AZ, no auto-scaling, no NAT Gateway, environment stopped outside scan windows
  (`deploy/30-start.sh` / `31-stop.sh`), complete teardown at project end (`deploy/90-destroy.sh`).

## 3. Logical architecture and components

- SSR ERB frontend (Rails)
- Rails controllers (web + JSON REST API)
- Form + session authentication; API key for REST
- Limit and card-blocking management service
- Virtual-card and dispute service

## 4. Network flow

1. The client (or scanner) reaches the **ALB** over **HTTPS (443)**; all **HTTP (80)** traffic is redirected to 443.
2. The ALB forwards to the **Elastic Beanstalk** instance (Ruby).
3. The application accesses **RDS PostgreSQL**.
4. Security headers (HSTS, CSP, hardened cookies, banner removal) are handled by the application
   and the reverse proxy — some intentional deviations constitute vulnerabilities of the corpus.

## 5. Authentication model

Form + session for the web; API key (X-API-Key) for REST endpoints.

## 6. Data model (main entities)

- Client
- Manager
- Card (limit, status)
- Virtual card
- Dispute

## 7. Security surface

The application is **secure by default**; the **22 vulnerabilities** of the reference set are **localized deviations**
(see `VULNERABILITY_GUIDE.md` and `ground-truth.yaml`, not exposed over HTTP). **Negative controls**
provide comparable, correctly secured routes. Focus layer: **Mixed Web/REST**.

## 8. Deployment and operations

- Deployment: `deploy/20-deploy-eb.sh` (EB environment + HTTPS 443 listener + 80 redirect).
- Start / stop outside campaigns: `deploy/30-start.sh` / `deploy/31-stop.sh`.
- Complete teardown: `deploy/90-destroy.sh`.
- Environment variables injected at deployment; no secret in the repository.

## 9. Lab reminders

- Permanent "Test environment – fictitious data" banner on all pages.
- Reset of the demo state: `POST /admin/reset`.
- `VULNERABILITY_GUIDE.md`, `negative-controls.yaml` and `ground-truth.yaml` are **never exposed over HTTP**.
