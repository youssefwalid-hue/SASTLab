> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-09 Novabourse — Conventions de build (partagées)

Laboratoire DAST : application bancaire volontairement vulnérable, **données 100 % fictives**, usage défensif
(test de scanners). **Sécurisée par défaut** ; les vulnérabilités sont des **écarts localisés** et *uniquement*
celles listées. Aucune marque réelle. Réponses portant `X-Lab-Environment: test-data-fictives`.

## Pile et exécution
- Pile : Python 3.12, FastAPI, SPA React
- Plateforme Elastic Beanstalk : **Python**
- Exécution : uvicorn main:app --port 8080 (local) ; EB Python
- Profil **standalone/local** sans base externe : SQLite / dépôt mémoire (démarrage immédiat, données fictives).
- Démarrage : web: uvicorn main:app --host 0.0.0.0 --port 5000
- Seed au démarrage ; réinitialisation par `POST /admin/reset` (ou équivalent documenté).

## Spécificité
FastAPI + SPA React, OIDC avec PKCE ; accent API + OIDC : IDOR portefeuille, redirect_uri/state faibles, BOLA passage d'ordre, mass assignment, JWT expiration. OpenAPI sur /docs ; IdP de laboratoire pour OIDC PKCE.

## Acteurs et données fictives
- Comptes de laboratoire : invest-a, invest-b, backoffice
- Hôte : `https://novabourse.lab.exemple`. Données 100 % fictives ; aucun secret réel.

## Endpoints et vulnérabilités (22) — écarts localisés, taguer `// GT: APP09-Vxxx`
- APP09-V001 [L3/API] IDOR sur le portefeuille — `GET /api/portfolios/{id}` (param id)
- APP09-V002 [L3/Browser Security] Validation faible du redirect_uri OIDC — `GET /authorize` (param redirect_uri)
- APP09-V003 [L4/Browser Security] state/nonce OIDC non vérifiés (CSRF de connexion) — `GET /signin-oidc` (param state)
- APP09-V004 [L3/API] BOLA : passage d'ordre pour un autre compte — `POST /api/orders` (param accountId)
- APP09-V005 [L3/API] Mass assignment d'un ordre (prix négatif) — `POST /api/orders` (param price)
- APP09-V006 [L4/Business Logic] Annulation d'un ordre déjà exécuté — `POST /api/orders/{id}/cancel` (param id)
- APP09-V007 [L2/API] Exposition du schéma OpenAPI complet — `GET /openapi.json` (param —)
- APP09-V008 [L3/API] Expiration de JWT non vérifiée — `GET /api/portfolios` (param Authorization)
- APP09-V009 [L2/Application] Injection SQL dans la recherche d'instrument — `GET /api/instruments` (param q)
- APP09-V010 [L3/API] Absence de limitation de débit sur les ordres — `POST /api/orders` (param —)
- APP09-V011 [L3/API] BFLA : endpoint back-office accessible — `GET /api/admin/orders` (param —)
- APP09-V012 [L3/Application] XSS stocké sur le nom de portefeuille (React) — `POST /api/portfolios` (param name)
- APP09-V013 [L1/Framework] Documentation interactive exposée (/docs) — `GET /docs` (param —)
- APP09-V014 [L1/HTTP] En-têtes de sécurité HTTP absents — `GET /` (param —)
- APP09-V015 [L2/Browser Security] CORS avec credentials et origine large — `GET /api/portfolios` (param Origin)
- APP09-V016 [L1/Web Server] Divulgation de bannière serveur et de version — `GET /` (param Server / X-Powered-By)

## Negative controls (12) — routes/paramètres SÛRS et DISTINCTS, taguer `// NC: APP09-Nxxx`
Lire et implémenter EXACTEMENT `negative-controls.yaml` (déjà corrigé) : chaque contrôle est une route sûre
distincte de la vulnérabilité associée.

## Transverse
- Correct PARTOUT (validation d'entrée, contrôle de propriété/rôle, requêtes paramétrées, encodage de sortie,
  en-têtes durcis…) SAUF les écarts listés. N'ajouter AUCUNE vulnérabilité hors des 16.
- `.ebextensions` = variables d'environnement UNIQUEMENT (leçon AL2023 : aucune option dépréciée/`ProxyServer`).
- `.ebignore` excluant `benchmark/` et les sources sensibles. `benchmark/` jamais servi par HTTP.
- Documentation technique (`README.md`, `SECURITY_MODEL.md`), `benchmark/ground-truth.yaml` (16 entrées, schéma
  complet, en-tête « ne jamais exposer/fournir aux scanners »), `benchmark/validation/validate.py`
  (1 test par vuln = PASS si comportement vulnérable confirmé ; 1 test par NC = PASS si attaque bloquée ;
  `POST /admin/reset` entre les cas ; rapports json/html ; requirements.txt).

---

# English version

# APP-09 Novabourse — Build conventions (shared)

DAST lab: intentionally vulnerable banking application, **100% fictitious data**, defensive use
(scanner testing). **Secure by default**; the vulnerabilities are **localized deviations** and *only*
those listed. No real brand. Responses carrying `X-Lab-Environment: test-data-fictives`.

## Stack and execution
- Stack: Python 3.12, FastAPI, React SPA
- Elastic Beanstalk platform: **Python**
- Execution: uvicorn main:app --port 8080 (local); EB Python
- **Standalone/local** profile without external database: SQLite / in-memory store (instant startup, fictitious data).
- Startup: web: uvicorn main:app --host 0.0.0.0 --port 5000
- Seed at startup; reset via `POST /admin/reset` (or documented equivalent).

## Specificity
FastAPI + React SPA, OIDC with PKCE; API + OIDC accent: portfolio IDOR, weak redirect_uri/state, order-placement BOLA, mass assignment, JWT expiration. OpenAPI at /docs; lab IdP for OIDC PKCE.

## Actors and fictitious data
- Lab accounts: invest-a, invest-b, backoffice
- Host: `https://novabourse.lab.exemple`. 100% fictitious data; no real secret.

## Endpoints and vulnerabilities (22) — localized deviations, tag `// GT: APP09-Vxxx`
- APP09-V001 [L3/API] Portfolio IDOR — `GET /api/portfolios/{id}` (param id)
- APP09-V002 [L3/Browser Security] Weak OIDC redirect_uri validation — `GET /authorize` (param redirect_uri)
- APP09-V003 [L4/Browser Security] OIDC state/nonce not verified (login CSRF) — `GET /signin-oidc` (param state)
- APP09-V004 [L3/API] BOLA: placing an order for another account — `POST /api/orders` (param accountId)
- APP09-V005 [L3/API] Order mass assignment (negative price) — `POST /api/orders` (param price)
- APP09-V006 [L4/Business Logic] Cancelling an already-executed order — `POST /api/orders/{id}/cancel` (param id)
- APP09-V007 [L2/API] Full OpenAPI schema exposure — `GET /openapi.json` (param —)
- APP09-V008 [L3/API] JWT expiration not verified — `GET /api/portfolios` (param Authorization)
- APP09-V009 [L2/Application] SQL injection in instrument search — `GET /api/instruments` (param q)
- APP09-V010 [L3/API] No rate limiting on orders — `POST /api/orders` (param —)
- APP09-V011 [L3/API] BFLA: accessible back-office endpoint — `GET /api/admin/orders` (param —)
- APP09-V012 [L3/Application] Stored XSS on portfolio name (React) — `POST /api/portfolios` (param name)
- APP09-V013 [L1/Framework] Interactive documentation exposed (/docs) — `GET /docs` (param —)
- APP09-V014 [L1/HTTP] HTTP security headers absent — `GET /` (param —)
- APP09-V015 [L2/Browser Security] CORS with credentials and broad origin — `GET /api/portfolios` (param Origin)
- APP09-V016 [L1/Web Server] Server banner and version disclosure — `GET /` (param Server / X-Powered-By)

## Negative controls (12) — SAFE and DISTINCT routes/parameters, tag `// NC: APP09-Nxxx`
Read and implement EXACTLY `negative-controls.yaml` (already corrected): each control is a safe route
distinct from the associated vulnerability.

## Cross-cutting
- Correct EVERYWHERE (input validation, ownership/role control, parameterized queries, output encoding,
  hardened headers…) EXCEPT the listed deviations. Add NO vulnerability outside the 16.
- `.ebextensions` = environment variables ONLY (AL2023 lesson: no deprecated option/`ProxyServer`).
- `.ebignore` excluding `benchmark/` and sensitive sources. `benchmark/` never served over HTTP.
- Technical documentation (`README.md`, `SECURITY_MODEL.md`), `benchmark/ground-truth.yaml` (16 entries, full
  schema, header "never expose/provide to scanners"), `benchmark/validation/validate.py`
  (1 test per vuln = PASS if vulnerable behavior confirmed; 1 test per NC = PASS if attack blocked;
  `POST /admin/reset` between cases; json/html reports; requirements.txt).
