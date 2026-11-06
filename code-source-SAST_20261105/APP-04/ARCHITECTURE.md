> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# ConseilDesk — Document d'architecture

> **Environnement de test – données fictives.** ConseilDesk (Back-office conseillers) est une application de laboratoire
> du Banking DAST Benchmark Lab. Toutes les données sont fictives ; aucune marque réelle n'est utilisée.

## 1. Vue d'ensemble

- **Finalité** : Recherche clients, dossiers, notes internes, exports, administration des rôles.
- **Pile technique** : PHP 8, Symfony, rendu serveur Twig.
- **Protocole principal** : Web classique + REST léger.
- **Hôte de laboratoire** : `https://conseildesk.lab.exemple` (HTTPS, HTTP 80 redirigé en 443).
- **Thème de sécurité** : Contrôle d'accès vertical : élévation conseiller→directeur, CSRF de rôle, SQLi legacy, LFI export.

## 2. Architecture AWS (HTTPS, sans filtrage IP, moindre coût)

```mermaid
flowchart LR
  SC["Scanner / client Internet"] -->|HTTPS 443| ALB["ALB + certificat ACM<br/>terminaison TLS"]
  H80["HTTP 80"] -. redirection 308 .-> ALB
  ALB --> EB["Elastic Beanstalk<br/>PHP · instance unique"]
  EB --> DB[("RDS MySQL")]
```

- **Elastic Beanstalk** en **instance unique** (`SingleInstance`), runtime **PHP**.
- **HTTPS** via **ALB mutualisé + certificat ACM** ; redirection HTTP 80 → 443.
- **Accès ouvert sans filtrage IP** : Security Group entrant `0.0.0.0/0` sur 443 (et 80), compensé par
  données fictives, arrêt hors campagne, rôle IAM minimal et réseau sortant restreint.
- **Ressources** : instance t3.micro, RDS MySQL.
- **Moindre coût** : mono-AZ, pas d'auto-scaling, pas de NAT Gateway, environnement arrêté hors fenêtres de scan
  (`deploy/30-start.sh` / `31-stop.sh`), suppression complète en fin de projet (`deploy/90-destroy.sh`).

## 3. Architecture logique et composants

- Frontend SSR Twig
- Contrôleurs Symfony
- Authentification par formulaire et session
- Service de recherche et de gestion des dossiers
- Module d'export (gabarits)
- API REST légère interne

## 4. Flux réseau

1. Le client (ou le scanner) atteint l'**ALB** en **HTTPS (443)** ; tout trafic **HTTP (80)** est redirigé en 443.
2. L'ALB transmet à l'instance **Elastic Beanstalk** (PHP).
3. L'application accède à **RDS MySQL**.
4. Les en-têtes de sécurité (HSTS, CSP, cookies durcis, suppression de bannière) sont gérés par l'application
   et le reverse proxy — certains écarts volontaires constituent des vulnérabilités du corpus.

## 5. Modèle d'authentification

Authentification par formulaire, session, rôles conseiller / directeur.

## 6. Modèle de données (entités principales)

- Utilisateur (rôle conseiller/directeur)
- Client
- Dossier client
- Note interne
- Export

## 7. Surface de sécurité

L'application est **sécurisée par défaut** ; les **22 vulnérabilités** du référentiel sont des **écarts localisés**
(voir `VULNERABILITY_GUIDE.md` et `ground-truth.yaml`, non exposés par HTTP). Des **negative controls**
fournissent des routes comparables correctement sécurisées. Couche d'accent : **Contrôle d'accès vertical **.

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

# ConseilDesk — Architecture document

> **Test environment – fictitious data.** ConseilDesk (Advisor back-office) is a lab application
> of the Banking DAST Benchmark Lab. All data is fictitious; no real brand is used.

## 1. Overview

- **Purpose**: Client search, case files, internal notes, exports, role administration.
- **Technical stack**: PHP 8, Symfony, server-side Twig rendering.
- **Main protocol**: Classic web + lightweight REST.
- **Lab host**: `https://conseildesk.lab.exemple` (HTTPS, HTTP 80 redirected to 443).
- **Security theme**: Vertical access control: advisor→director elevation, role CSRF, legacy SQLi, export LFI.

## 2. AWS architecture (HTTPS, no IP filtering, lowest cost)

```mermaid
flowchart LR
  SC["Scanner / client Internet"] -->|HTTPS 443| ALB["ALB + certificat ACM<br/>terminaison TLS"]
  H80["HTTP 80"] -. redirection 308 .-> ALB
  ALB --> EB["Elastic Beanstalk<br/>PHP · instance unique"]
  EB --> DB[("RDS MySQL")]
```

- **Elastic Beanstalk** as a **single instance** (`SingleInstance`), **PHP** runtime.
- **HTTPS** via **shared ALB + ACM certificate**; HTTP 80 → 443 redirection.
- **Open access without IP filtering**: inbound Security Group `0.0.0.0/0` on 443 (and 80), offset by
  fictitious data, shutdown outside campaigns, minimal IAM role and restricted outbound network.
- **Resources**: t3.micro instance, RDS MySQL.
- **Lowest cost**: single-AZ, no auto-scaling, no NAT Gateway, environment stopped outside scan windows
  (`deploy/30-start.sh` / `31-stop.sh`), full teardown at end of project (`deploy/90-destroy.sh`).

## 3. Logical architecture and components

- Twig SSR frontend
- Symfony controllers
- Form and session authentication
- Search and case-management service
- Export module (templates)
- Internal lightweight REST API

## 4. Network flow

1. The client (or the scanner) reaches the **ALB** over **HTTPS (443)**; all **HTTP (80)** traffic is redirected to 443.
2. The ALB forwards to the **Elastic Beanstalk** instance (PHP).
3. The application accesses **RDS MySQL**.
4. The security headers (HSTS, CSP, hardened cookies, banner removal) are handled by the application
   and the reverse proxy — some deliberate deviations constitute vulnerabilities of the corpus.

## 5. Authentication model

Form authentication, session, advisor / director roles.

## 6. Data model (main entities)

- User (advisor/director role)
- Client
- Client case file
- Internal note
- Export

## 7. Security surface

The application is **secure by default**; the **22 vulnerabilities** of the benchmark are **localized deviations**
(see `VULNERABILITY_GUIDE.md` and `ground-truth.yaml`, not exposed over HTTP). **Negative controls**
provide comparable, correctly secured routes. Focus layer: **Vertical access control **.

## 8. Deployment and operation

- Deployment: `deploy/20-deploy-eb.sh` (EB environment + HTTPS 443 listener + 80 redirection).
- Startup / shutdown outside campaigns: `deploy/30-start.sh` / `deploy/31-stop.sh`.
- Full teardown: `deploy/90-destroy.sh`.
- Environment variables injected at deployment; no secret in the repository.

## 9. Lab reminders

- Permanent "Test environment – fictitious data" banner on every page.
- Demo state reset: `POST /admin/reset`.
- `VULNERABILITY_GUIDE.md`, `negative-controls.yaml` and `ground-truth.yaml` are **never exposed over HTTP**.
