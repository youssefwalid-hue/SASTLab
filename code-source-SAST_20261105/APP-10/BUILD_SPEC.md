> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-10 OnboardR — Conventions de build (partagées)

Laboratoire DAST : application bancaire volontairement vulnérable, **données 100 % fictives**, usage défensif
(test de scanners). **Sécurisée par défaut** ; les vulnérabilités sont des **écarts localisés** et *uniquement*
celles listées. Aucune marque réelle. Réponses portant `X-Lab-Environment: test-data-fictives`.

## Pile et exécution
- Pile : Microservices Java (Spring Boot) + Node.js (NestJS) derrière un reverse proxy dédié
- Plateforme Elastic Beanstalk : **Docker**
- Exécution : docker compose up (local) ; EB Docker
- Profil **standalone/local** sans base externe : PostgreSQL+MySQL via compose, ou dépôt mémoire (démarrage immédiat, données fictives).
- Démarrage : (plateforme EB Docker : Dockerrun/compose)
- Seed au démarrage ; réinitialisation par `POST /admin/reset` (ou équivalent documenté).

## Spécificité
Microservices Java (Spring Boot) + Node.js (NestJS) derrière un reverse proxy Nginx DÉDIÉ ; accent architecture : bypass d'accès par le proxy, confiance X-Forwarded-*, normalisation de chemin, SSRF inter-service, request smuggling CL.TE, cache poisoning. Les vulnérabilités de couche Reverse Proxy (V001,V002,V003,V005,V012,V015) vivent dans la config Nginx fournie. Démonstrations ciblant exclusivement le laboratoire.

## Acteurs et données fictives
- Comptes de laboratoire : prospect1, prospect2, conformite1
- Hôte : `https://onboardr.lab.exemple`. Données 100 % fictives ; aucun secret réel.

## Endpoints et vulnérabilités (21) — écarts localisés, taguer `// GT: APP10-Vxxx`
- APP10-V001 [L3/Reverse Proxy] Contournement du contrôle d'accès par le reverse proxy — `GET /internal/kyc` (param en-tête interne)
- APP10-V002 [L3/Reverse Proxy] Confiance dans X-Forwarded-* (bypass d'authentification) — `GET /api/onboarding` (param X-Forwarded-Host)
- APP10-V003 [L3/Reverse Proxy] Contournement de règle par normalisation de chemin — `GET /api/..%2fadmin` (param —)
- APP10-V004 [L4/API] SSRF inter-service à la récupération de pièce — `POST /api/documents/fetch` (param url)
- APP10-V005 [L4/Reverse Proxy] Request smuggling (CL.TE) — `POST /api/onboarding` (param Content-Length/Transfer-Encoding)
- APP10-V006 [L3/API] IDOR sur le dossier KYC — `GET /api/onboarding/{id}` (param id)
- APP10-V007 [L2/Application] Upload non restreint de pièce d'identité — `POST /api/onboarding/{id}/documents` (param file)
- APP10-V008 [L3/API] Mass assignment du statut KYC (auto-validation) — `PUT /api/onboarding/{id}` (param status)
- APP10-V009 [L3/Business Logic] OTP simulé côté client contournable — `POST /api/onboarding/{id}/verify` (param otp)
- APP10-V010 [L3/HTTP] Injection d'en-tête / CRLF — `GET /api/redirect` (param target)
- APP10-V011 [L1/HTTP] Fuite d'en-têtes de service interne — `GET /api/onboarding` (param X-Service)
- APP10-V012 [L4/HTTP] Empoisonnement de cache par en-tête non clé — `GET /api/config` (param X-Forwarded-Host)
- APP10-V013 [L3/API] BFLA : administration de l'onboarding — `POST /api/admin/onboarding/{id}/decision` (param —)
- APP10-V014 [L1/HTTP] Absence de HSTS / en-têtes de sécurité — `GET /` (param —)
- APP10-V015 [L2/Web Server] Traversée de chemin sur les ressources statiques du proxy — `GET /static/../` (param —)

## Negative controls (13) — routes/paramètres SÛRS et DISTINCTS, taguer `// NC: APP10-Nxxx`
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

# APP-10 OnboardR — Build conventions (shared)

DAST laboratory: an intentionally vulnerable banking application, **100% fictitious data**, defensive use
(scanner testing). **Secure by default**; the vulnerabilities are **localized deviations** and *only*
the listed ones. No real brand. Responses carry `X-Lab-Environment: test-data-fictives`.

## Stack and execution
- Stack: Java microservices (Spring Boot) + Node.js (NestJS) behind a dedicated reverse proxy
- Elastic Beanstalk platform: **Docker**
- Execution: docker compose up (local); EB Docker
- **Standalone/local** profile without an external database: PostgreSQL+MySQL via compose, or in-memory store (instant startup, fictitious data).
- Startup: (EB Docker platform: Dockerrun/compose)
- Seed at startup; reset via `POST /admin/reset` (or documented equivalent).

## Specificity
Java microservices (Spring Boot) + Node.js (NestJS) behind a DEDICATED Nginx reverse proxy; architecture emphasis: access bypass through the proxy, X-Forwarded-* trust, path normalization, inter-service SSRF, CL.TE request smuggling, cache poisoning. The Reverse Proxy layer vulnerabilities (V001,V002,V003,V005,V012,V015) live in the supplied Nginx config. Demonstrations target the laboratory exclusively.

## Actors and fictitious data
- Laboratory accounts: prospect1, prospect2, conformite1
- Host: `https://onboardr.lab.exemple`. 100% fictitious data; no real secrets.

## Endpoints and vulnerabilities (21) — localized deviations, tag `// GT: APP10-Vxxx`
- APP10-V001 [L3/Reverse Proxy] Access control bypass by the reverse proxy — `GET /internal/kyc` (internal header param)
- APP10-V002 [L3/Reverse Proxy] Trust in X-Forwarded-* (authentication bypass) — `GET /api/onboarding` (X-Forwarded-Host param)
- APP10-V003 [L3/Reverse Proxy] Rule bypass via path normalization — `GET /api/..%2fadmin` (param —)
- APP10-V004 [L4/API] Inter-service SSRF at document retrieval — `POST /api/documents/fetch` (url param)
- APP10-V005 [L4/Reverse Proxy] Request smuggling (CL.TE) — `POST /api/onboarding` (Content-Length/Transfer-Encoding param)
- APP10-V006 [L3/API] IDOR on the KYC dossier — `GET /api/onboarding/{id}` (id param)
- APP10-V007 [L2/Application] Unrestricted ID document upload — `POST /api/onboarding/{id}/documents` (file param)
- APP10-V008 [L3/API] Mass assignment of KYC status (self-approval) — `PUT /api/onboarding/{id}` (status param)
- APP10-V009 [L3/Business Logic] Bypassable client-side simulated OTP — `POST /api/onboarding/{id}/verify` (otp param)
- APP10-V010 [L3/HTTP] Header injection / CRLF — `GET /api/redirect` (target param)
- APP10-V011 [L1/HTTP] Internal service header leak — `GET /api/onboarding` (X-Service param)
- APP10-V012 [L4/HTTP] Cache poisoning via unkeyed header — `GET /api/config` (X-Forwarded-Host param)
- APP10-V013 [L3/API] BFLA: onboarding administration — `POST /api/admin/onboarding/{id}/decision` (param —)
- APP10-V014 [L1/HTTP] Missing HSTS / security headers — `GET /` (param —)
- APP10-V015 [L2/Web Server] Path traversal on the proxy static resources — `GET /static/../` (param —)

## Negative controls (13) — SAFE and DISTINCT routes/parameters, tag `// NC: APP10-Nxxx`
Read and implement EXACTLY `negative-controls.yaml` (already fixed): each control is a safe route
distinct from the associated vulnerability.

## Cross-cutting
- Correct EVERYWHERE (input validation, ownership/role control, parameterized queries, output encoding,
  hardened headers…) EXCEPT the listed deviations. Add NO vulnerability outside the 16.
- `.ebextensions` = environment variables ONLY (AL2023 lesson: no deprecated option / `ProxyServer`).
- `.ebignore` excluding `benchmark/` and the sensitive sources. `benchmark/` never served over HTTP.
- Technical documentation (`README.md`, `SECURITY_MODEL.md`), `benchmark/ground-truth.yaml` (16 entries, full
  schema, "never expose / never provide to scanners" header), `benchmark/validation/validate.py`
  (1 test per vuln = PASS if vulnerable behavior confirmed; 1 test per NC = PASS if attack blocked;
  `POST /admin/reset` between cases; json/html reports; requirements.txt).
