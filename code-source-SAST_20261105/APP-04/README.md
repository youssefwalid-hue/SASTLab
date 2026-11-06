> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-04 — ConseilDesk (Back-office conseillers)

Application **volontairement vulnérable** du *Banking DAST Benchmark Lab*.
**Usage défensif uniquement** (test de scanners DAST en laboratoire interne).
**DONNÉES 100% FICTIVES — aucun secret réel.** Toutes les réponses portent
l'en-tête `X-Lab-Environment: test-data-fictives`.

> Même procédé qu'APP-01 (Java/Spring), APP-02 (NestJS) et APP-03 (ASP.NET) :
> **sécurisé par défaut**, les vulnérabilités sont des **écarts localisés** et
> *uniquement* les **16** listées. Des **negative controls** fournissent des routes
> comparables correctement sécurisées (une alerte sur l'une d'elles est un faux positif).

## Pile

- **PHP 8 / Symfony** — rendu serveur **Twig**, authentification formulaire + session
  (rôles `CONSEILLER` / `DIRECTEUR`), **API REST légère** (JSON).
- Stockage **SQLite** (profil standalone) ; en `prod` réel : **RDS MySQL** (même schéma,
  même logique, mêmes écarts). Seed au démarrage ; réinitialisation par `POST /admin/reset`.
- Accent sécurité : **contrôle d'accès vertical** (élévation conseiller→directeur, CSRF de
  rôle), **SQLi legacy**, **LFI export**.

> **Note de construction (hors-ligne)** : ce dépôt est structuré comme un projet Symfony
> (`public/` front controllers, `src/Controller`, `src/Service`, `templates/*.twig`,
> `config/`, `composer.json`). Le registre de paquets (Packagist) étant **inaccessible**
> dans l'environnement de build, l'application embarque un noyau autonome minimal et un
> moteur de gabarits **Twig-subset** (échappement automatique par défaut, filtre `|raw`,
> `{% if %}`/`{% for %}`/`{% include %}`) afin de **tourner sans `vendor/`** via le serveur
> web intégré de PHP. Les gabarits `.html.twig` et la structure restent interchangeables
> avec un déploiement Symfony complet (`composer install`).

## Démarrage local

```bash
# Serveur web intégré de PHP (routeur fourni) :
php -S 0.0.0.0:8080 -t public public/router.php
```

- Application : `http://localhost:8080/`
- Connexion : `conseiller1` / `conseiller2` / `directeur1` — mot de passe `Password1!`
- Réinitialisation de l'état : `POST /admin/reset`
- Export : `GET /export?template=dossier&id=1500`
- API légère : `GET /api/clients/1500` (`Accept: application/json`)

Sur Elastic Beanstalk (plateforme **PHP**, Apache + php-fpm) : document root `public/`,
réécriture via `public/.htaccess`. Variables d'environnement dans `.ebextensions/01-options.config`
(**uniquement** des variables d'environnement — aucune option dépréciée AL2023).

## Comptes et données (fictifs)

| Compte | id | Rôle | Portefeuille |
|--------|----|------|--------------|
| conseiller1 | 41 | CONSEILLER | clients 1500, 1501 |
| conseiller2 | 42 | CONSEILLER | client 2050 |
| directeur1  | 43 | DIRECTEUR  | transverse |

Dossiers : `1001` (EN_ATTENTE_VALIDATION), `1002` (VALIDE), `2001` (VALIDE).

## Les 22 vulnérabilités (écarts localisés, tag `// GT: APP04-Vxxx`)

| ID | Vulnérabilité | Endpoint / paramètre |
|----|---------------|----------------------|
| V001 | Élévation de privilège conseiller→directeur | `POST /admin/users/{id}/role` · role |
| V002 | Injection SQL recherche client (legacy) | `GET /clients` · search |
| V003 | LFI / traversée de chemin export | `GET /export` · template |
| V004 | IDOR notes client | `GET /clients/{id}/notes` · id |
| V005 | XSS stocké note interne | `POST /clients/{id}/notes` · body |
| V006 | CSRF changement de rôle | `POST /admin/users/{id}/role` · role |
| V007 | Clôture dossier sans contrôle d'état | `POST /files/{id}/close` |
| V008 | Exposition PII par export complet | `GET /export` · full |
| V009 | IDOR API légère clients | `GET /api/clients/{id}` · id |
| V010 | Fixation de session | `POST /login` · PHPSESSID |
| V011 | Cookie de session sans HttpOnly | `POST /login` · Set-Cookie |
| V012 | Profiler Symfony exposé | `GET /_profiler` |
| V013 | Mode debug / front controller dev | `GET /app_dev.php` |
| V014 | Méthodes HTTP d'écriture activées | `OPTIONS /`, `PUT /uploads/{f}` |
| V015 | Confiance dans X-Forwarded-For | `GET /clients` · X-Forwarded-For |
| V016 | Divulgation de bannière serveur | `GET /` · Server / X-Powered-By |

## Negative controls (routes sûres, tag `// NC: APP04-Nxxx`)

| ID | Route sûre | Blindage |
|----|-----------|----------|
| N001 | `POST /admin/roles/{id}/assign` | ROLE_DIRECTEUR + CSRF + anti auto-élévation |
| N002 | `GET /clients/v2` | requête préparée paramétrée |
| N003 | `GET /export/v2` | liste blanche de gabarits + champs selon rôle |
| N004 | `GET /api/v2/clients/{id}` | voter de propriété ; XFF ignoré |
| N005 | `POST /login/v2` | session régénérée + cookie HttpOnly/Secure/SameSite |
| N006 | `POST /files/{id}/close-v2` | machine à états (409 hors VALIDE) |

## Validation

```bash
cd benchmark/validation
pip install -r requirements.txt
python3 validate.py --base http://localhost:8080
```

1 test par vulnérabilité (PASS = comportement vulnérable confirmé) + 1 test par negative
control (PASS = attaque bloquée). Reset entre chaque cas. Rapports `validation-report.json`
et `validation-report.html`. **Résultat de référence : 22/22 PASS.**

> `benchmark/` (ground-truth, guide, validation) ne doit **jamais** être servi par HTTP ni
> fourni à un scanner : il contient l'oracle de notation. Exclu du déploiement via `.ebignore`.

---

# English version

# APP-04 — ConseilDesk (Advisor back-office)

**Deliberately vulnerable** application of the *Banking DAST Benchmark Lab*.
**Defensive use only** (testing DAST scanners in an internal lab).
**100% FICTITIOUS DATA — no real secrets.** Every response carries
the `X-Lab-Environment: test-data-fictives` header.

> Same approach as APP-01 (Java/Spring), APP-02 (NestJS) and APP-03 (ASP.NET):
> **secure by default**, the vulnerabilities are **localized deviations** and
> *only* the **16** listed. **Negative controls** provide comparable, correctly
> secured routes (an alert on one of them is a false positive).

## Stack

- **PHP 8 / Symfony** — server-side **Twig** rendering, form + session authentication
  (roles `CONSEILLER` / `DIRECTEUR`), **lightweight REST API** (JSON).
- **SQLite** storage (standalone profile); in real `prod`: **RDS MySQL** (same schema,
  same logic, same deviations). Seeded at startup; reset via `POST /admin/reset`.
- Security focus: **vertical access control** (advisor→director elevation, role CSRF),
  **legacy SQLi**, **export LFI**.

> **Build note (offline)**: this repository is structured as a Symfony project
> (`public/` front controllers, `src/Controller`, `src/Service`, `templates/*.twig`,
> `config/`, `composer.json`). Because the package registry (Packagist) is **unreachable**
> in the build environment, the application embeds a minimal standalone kernel and a
> **Twig-subset** template engine (automatic escaping by default, `|raw` filter,
> `{% if %}`/`{% for %}`/`{% include %}`) so it can **run without `vendor/`** via PHP's
> built-in web server. The `.html.twig` templates and the structure remain interchangeable
> with a full Symfony deployment (`composer install`).

## Local startup

```bash
# PHP built-in web server (router provided):
php -S 0.0.0.0:8080 -t public public/router.php
```

- Application: `http://localhost:8080/`
- Login: `conseiller1` / `conseiller2` / `directeur1` — password `Password1!`
- State reset: `POST /admin/reset`
- Export: `GET /export?template=dossier&id=1500`
- Lightweight API: `GET /api/clients/1500` (`Accept: application/json`)

On Elastic Beanstalk (**PHP** platform, Apache + php-fpm): document root `public/`,
rewriting via `public/.htaccess`. Environment variables in `.ebextensions/01-options.config`
(**only** environment variables — no deprecated AL2023 option).

## Accounts and data (fictitious)

| Account | id | Role | Portfolio |
|--------|----|------|--------------|
| conseiller1 | 41 | CONSEILLER | clients 1500, 1501 |
| conseiller2 | 42 | CONSEILLER | client 2050 |
| directeur1  | 43 | DIRECTEUR  | cross-cutting |

Case files: `1001` (EN_ATTENTE_VALIDATION), `1002` (VALIDE), `2001` (VALIDE).

## The 22 vulnerabilities (localized deviations, tag `// GT: APP04-Vxxx`)

| ID | Vulnerability | Endpoint / parameter |
|----|---------------|----------------------|
| V001 | Privilege elevation advisor→director | `POST /admin/users/{id}/role` · role |
| V002 | SQL injection in client search (legacy) | `GET /clients` · search |
| V003 | LFI / path traversal in export | `GET /export` · template |
| V004 | IDOR on client notes | `GET /clients/{id}/notes` · id |
| V005 | Stored XSS in internal note | `POST /clients/{id}/notes` · body |
| V006 | CSRF on role change | `POST /admin/users/{id}/role` · role |
| V007 | Case closure without state check | `POST /files/{id}/close` |
| V008 | PII exposure via full export | `GET /export` · full |
| V009 | IDOR on lightweight client API | `GET /api/clients/{id}` · id |
| V010 | Session fixation | `POST /login` · PHPSESSID |
| V011 | Session cookie without HttpOnly | `POST /login` · Set-Cookie |
| V012 | Exposed Symfony profiler | `GET /_profiler` |
| V013 | Debug mode / dev front controller | `GET /app_dev.php` |
| V014 | HTTP write methods enabled | `OPTIONS /`, `PUT /uploads/{f}` |
| V015 | Trust in X-Forwarded-For | `GET /clients` · X-Forwarded-For |
| V016 | Server banner disclosure | `GET /` · Server / X-Powered-By |

## Negative controls (safe routes, tag `// NC: APP04-Nxxx`)

| ID | Safe route | Hardening |
|----|-----------|----------|
| N001 | `POST /admin/roles/{id}/assign` | ROLE_DIRECTEUR + CSRF + anti self-elevation |
| N002 | `GET /clients/v2` | parameterized prepared statement |
| N003 | `GET /export/v2` | template allow-list + role-based fields |
| N004 | `GET /api/v2/clients/{id}` | ownership voter; XFF ignored |
| N005 | `POST /login/v2` | regenerated session + HttpOnly/Secure/SameSite cookie |
| N006 | `POST /files/{id}/close-v2` | state machine (409 outside VALIDE) |

## Validation

```bash
cd benchmark/validation
pip install -r requirements.txt
python3 validate.py --base http://localhost:8080
```

1 test per vulnerability (PASS = vulnerable behavior confirmed) + 1 test per negative
control (PASS = attack blocked). Reset between each case. Reports `validation-report.json`
and `validation-report.html`. **Reference result: 22/22 PASS.**

> `benchmark/` (ground-truth, guide, validation) must **never** be served over HTTP nor
> supplied to a scanner: it contains the scoring oracle. Excluded from deployment via `.ebignore`.
