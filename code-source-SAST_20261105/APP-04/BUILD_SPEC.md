> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-04 ConseilDesk — Conventions de build (partagées)

Laboratoire DAST : application bancaire volontairement vulnérable, **données 100 % fictives**, usage défensif
(test de scanners). **Sécurisée par défaut** ; les vulnérabilités sont des **écarts localisés** et *uniquement*
celles listées. Aucune marque réelle. Réponses portant `X-Lab-Environment: test-data-fictives`.

## Pile et exécution
- Pile : PHP 8, Symfony, rendu serveur Twig
- Plateforme Elastic Beanstalk : **PHP**
- Exécution : php -S 0.0.0.0:8080 -t public (local) ; EB PHP (Apache+php-fpm)
- Profil **standalone/local** sans base externe : SQLite ou dépôt mémoire/fichier (démarrage immédiat, données fictives).
- Démarrage : (plateforme PHP : document root public/, pas de Procfile)
- Seed au démarrage ; réinitialisation par `POST /admin/reset` (ou équivalent documenté).

## Spécificité
Back-office interne, rôles conseiller/directeur ; rendu serveur Twig ; accent contrôle d'accès vertical (élévation de rôle, CSRF de rôle), SQLi legacy, LFI export.

## Acteurs et données fictives
- Comptes de laboratoire : conseiller1, conseiller2, directeur1
- Hôte : `https://conseildesk.lab.exemple`. Données 100 % fictives ; aucun secret réel.

## Endpoints et vulnérabilités (22) — écarts localisés, taguer `// GT: APP04-Vxxx`
- APP04-V001 [L3/Application] Élévation de privilège conseiller → directeur — `POST /admin/users/{id}/role` (param role)
- APP04-V002 [L2/Application] Injection SQL dans la recherche client (legacy) — `GET /clients` (param search)
- APP04-V003 [L3/Application] LFI / traversée de chemin sur l'export de dossier — `GET /export` (param template)
- APP04-V004 [L3/Application] IDOR sur les notes d'un client — `GET /clients/{id}/notes` (param id)
- APP04-V005 [L3/Application] XSS stocké sur une note interne — `POST /clients/{id}/notes` (param body)
- APP04-V006 [L3/Application] CSRF sur le changement de rôle — `POST /admin/users/{id}/role` (param role)
- APP04-V007 [L3/Business Logic] Clôture de dossier sans contrôle d'état — `POST /files/{id}/close` (param —)
- APP04-V008 [L2/Application] Exposition de PII par un export complet — `GET /export` (param full)
- APP04-V009 [L3/API] IDOR sur l'API légère clients — `GET /api/clients/{id}` (param id)
- APP04-V010 [L3/Browser Security] Fixation de session — `POST /login` (param PHPSESSID)
- APP04-V011 [L1/Browser Security] Cookie de session sans HttpOnly — `POST /login` (param Set-Cookie)
- APP04-V012 [L2/Framework] Profiler Symfony exposé — `GET /_profiler` (param —)
- APP04-V013 [L2/Framework] Mode debug / front controller de développement — `GET /app_dev.php` (param —)
- APP04-V014 [L2/Web Server] Méthodes HTTP d'écriture activées — `OPTIONS /` (param —)
- APP04-V015 [L3/Reverse Proxy] Confiance dans X-Forwarded-For (usurpation d'IP journalisée) — `GET /clients` (param X-Forwarded-For)
- APP04-V016 [L1/Web Server] Divulgation de bannière serveur et de version — `GET /` (param Server / X-Powered-By)

## Negative controls (12) — routes/paramètres SÛRS et DISTINCTS, taguer `// NC: APP04-Nxxx`
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

# APP-04 ConseilDesk — Build conventions (shared)

DAST lab: deliberately vulnerable banking application, **100% fictitious data**, defensive use
(scanner testing). **Secure by default**; the vulnerabilities are **localized deviations** and *only*
the ones listed. No real brand. Responses carrying `X-Lab-Environment: test-data-fictives`.

## Stack and execution
- Stack: PHP 8, Symfony, server-side Twig rendering
- Elastic Beanstalk platform: **PHP**
- Execution: php -S 0.0.0.0:8080 -t public (local); EB PHP (Apache+php-fpm)
- **Standalone/local** profile without external database: SQLite or memory/file store (immediate startup, fictitious data).
- Startup: (PHP platform: document root public/, no Procfile)
- Seed at startup; reset via `POST /admin/reset` (or documented equivalent).

## Specificity
Internal back-office, advisor/director roles; server-side Twig rendering; focus on vertical access control (role elevation, role CSRF), legacy SQLi, export LFI.

## Actors and fictitious data
- Lab accounts: conseiller1, conseiller2, directeur1
- Host: `https://conseildesk.lab.exemple`. 100% fictitious data; no real secret.

## Endpoints and vulnerabilities (22) — localized deviations, tag `// GT: APP04-Vxxx`
- APP04-V001 [L3/Application] Privilege elevation advisor → director — `POST /admin/users/{id}/role` (param role)
- APP04-V002 [L2/Application] SQL injection in client search (legacy) — `GET /clients` (param search)
- APP04-V003 [L3/Application] LFI / path traversal on case export — `GET /export` (param template)
- APP04-V004 [L3/Application] IDOR on a client's notes — `GET /clients/{id}/notes` (param id)
- APP04-V005 [L3/Application] Stored XSS on an internal note — `POST /clients/{id}/notes` (param body)
- APP04-V006 [L3/Application] CSRF on role change — `POST /admin/users/{id}/role` (param role)
- APP04-V007 [L3/Business Logic] Case closure without state check — `POST /files/{id}/close` (param —)
- APP04-V008 [L2/Application] PII exposure via a full export — `GET /export` (param full)
- APP04-V009 [L3/API] IDOR on the lightweight client API — `GET /api/clients/{id}` (param id)
- APP04-V010 [L3/Browser Security] Session fixation — `POST /login` (param PHPSESSID)
- APP04-V011 [L1/Browser Security] Session cookie without HttpOnly — `POST /login` (param Set-Cookie)
- APP04-V012 [L2/Framework] Exposed Symfony profiler — `GET /_profiler` (param —)
- APP04-V013 [L2/Framework] Debug mode / development front controller — `GET /app_dev.php` (param —)
- APP04-V014 [L2/Web Server] HTTP write methods enabled — `OPTIONS /` (param —)
- APP04-V015 [L3/Reverse Proxy] Trust in X-Forwarded-For (logged IP spoofing) — `GET /clients` (param X-Forwarded-For)
- APP04-V016 [L1/Web Server] Server and version banner disclosure — `GET /` (param Server / X-Powered-By)

## Negative controls (12) — SAFE and DISTINCT routes/parameters, tag `// NC: APP04-Nxxx`
Read and implement EXACTLY `negative-controls.yaml` (already corrected): each control is a safe route
distinct from the associated vulnerability.

## Cross-cutting
- Correct EVERYWHERE (input validation, ownership/role control, parameterized queries, output encoding,
  hardened headers…) EXCEPT the listed deviations. Add NO vulnerability outside the 16.
- `.ebextensions` = environment variables ONLY (AL2023 lesson: no deprecated option/`ProxyServer`).
- `.ebignore` excluding `benchmark/` and sensitive sources. `benchmark/` never served over HTTP.
- Technical documentation (`README.md`, `SECURITY_MODEL.md`), `benchmark/ground-truth.yaml` (16 entries, complete
  schema, header "never expose/supply to scanners"), `benchmark/validation/validate.py`
  (1 test per vuln = PASS if vulnerable behavior confirmed; 1 test per NC = PASS if attack blocked;
  `POST /admin/reset` between cases; json/html reports; requirements.txt).
