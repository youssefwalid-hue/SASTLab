> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-08 CartaVie — Conventions de build (partagées)

Laboratoire DAST : application bancaire volontairement vulnérable, **données 100 % fictives**, usage défensif
(test de scanners). **Sécurisée par défaut** ; les vulnérabilités sont des **écarts localisés** et *uniquement*
celles listées. Aucune marque réelle. Réponses portant `X-Lab-Environment: test-data-fictives`.

## Pile et exécution
- Pile : Ruby 3.3, Ruby on Rails
- Plateforme Elastic Beanstalk : **Ruby**
- Exécution : rails server -b 0.0.0.0 -p 8080 (local) ; EB Ruby (Puma)
- Profil **standalone/local** sans base externe : SQLite (démarrage immédiat, données fictives).
- Démarrage : web: bundle exec puma -p 5000
- Seed au démarrage ; réinitialisation par `POST /admin/reset` (ou équivalent documenté).

## Spécificité
Ruby on Rails, Web (ERB) + REST JSON ; accent mixte : mass assignment des plafonds, IDOR carte virtuelle, clé d'API rejouable, contournement strong params.

## Acteurs et données fictives
- Comptes de laboratoire : client-a, client-b, gestionnaire
- Hôte : `https://cartavie.lab.exemple`. Données 100 % fictives ; aucun secret réel.

## Endpoints et vulnérabilités (22) — écarts localisés, taguer `// GT: APP08-Vxxx`
- APP08-V001 [L2/API] Mass assignment du plafond de carte — `PUT /api/cards/{id}` (param limit)
- APP08-V002 [L3/API] IDOR sur une carte virtuelle — `GET /api/cards/{id}` (param id)
- APP08-V003 [L3/API] Clé d'API rejouable sans expiration — `GET /api/cards` (param X-API-Key)
- APP08-V004 [L3/API] BFLA : opposition sur la carte d'autrui — `POST /api/cards/{id}/block` (param id)
- APP08-V005 [L3/Application] Contournement des strong params (Rails) — `PATCH /cards/{id}` (param card[status])
- APP08-V006 [L2/Application] Injection SQL sur le paramètre de tri — `GET /cards` (param order)
- APP08-V007 [L3/Application] XSS stocké sur le libellé de carte — `POST /cards/{id}/label` (param label)
- APP08-V008 [L4/Business Logic] Dépassement de plafond via la contestation — `POST /cards/{id}/dispute` (param amount)
- APP08-V009 [L2/API] Exposition excessive du PAN — `GET /api/cards/{id}` (param —)
- APP08-V010 [L2/Application] CSRF sur le blocage de carte (web) — `POST /cards/{id}/toggle` (param —)
- APP08-V011 [L3/Browser Security] Cookie de session faiblement protégé — `POST /login` (param _session)
- APP08-V012 [L1/Framework] Erreurs Rails verbeuses (routes exposées) — `GET /cards/err` (param —)
- APP08-V013 [L2/Browser Security] Redirection ouverte — `GET /login` (param redirect)
- APP08-V014 [L1/HTTP] En-têtes de sécurité HTTP absents — `GET /` (param —)
- APP08-V015 [L3/Reverse Proxy] Accès à /rails/info via normalisation de chemin — `GET /rails/info/properties` (param —)
- APP08-V016 [L1/Web Server] Divulgation de bannière serveur et de version — `GET /` (param Server / X-Powered-By)

## Negative controls (12) — routes/paramètres SÛRS et DISTINCTS, taguer `// NC: APP08-Nxxx`
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

# APP-08 CartaVie — Build conventions (shared)

DAST lab: deliberately vulnerable banking application, **100% fictitious data**, defensive use
(scanner testing). **Secure by default**; the vulnerabilities are **localized deviations** and *only*
the listed ones. No real brands. Responses carry `X-Lab-Environment: test-data-fictives`.

## Stack and runtime
- Stack: Ruby 3.3, Ruby on Rails
- Elastic Beanstalk platform: **Ruby**
- Runtime: rails server -b 0.0.0.0 -p 8080 (local); EB Ruby (Puma)
- **Standalone/local** profile without external database: SQLite (instant startup, fictitious data).
- Startup: web: bundle exec puma -p 5000
- Seed at startup; reset via `POST /admin/reset` (or documented equivalent).

## Specificity
Ruby on Rails, Web (ERB) + REST JSON; mixed focus: mass assignment of limits, virtual-card IDOR, replayable API key, strong-params bypass.

## Actors and fictitious data
- Lab accounts: client-a, client-b, gestionnaire
- Host: `https://cartavie.lab.exemple`. 100% fictitious data; no real secrets.

## Endpoints and vulnerabilities (22) — localized deviations, tag `// GT: APP08-Vxxx`
- APP08-V001 [L2/API] Mass assignment of the card limit — `PUT /api/cards/{id}` (param limit)
- APP08-V002 [L3/API] IDOR on a virtual card — `GET /api/cards/{id}` (param id)
- APP08-V003 [L3/API] Replayable API key without expiration — `GET /api/cards` (param X-API-Key)
- APP08-V004 [L3/API] BFLA: blocking another user's card — `POST /api/cards/{id}/block` (param id)
- APP08-V005 [L3/Application] Strong-params bypass (Rails) — `PATCH /cards/{id}` (param card[status])
- APP08-V006 [L2/Application] SQL injection on the sort parameter — `GET /cards` (param order)
- APP08-V007 [L3/Application] Stored XSS on the card label — `POST /cards/{id}/label` (param label)
- APP08-V008 [L4/Business Logic] Limit overrun via dispute — `POST /cards/{id}/dispute` (param amount)
- APP08-V009 [L2/API] Excessive PAN exposure — `GET /api/cards/{id}` (param —)
- APP08-V010 [L2/Application] CSRF on card blocking (web) — `POST /cards/{id}/toggle` (param —)
- APP08-V011 [L3/Browser Security] Weakly protected session cookie — `POST /login` (param _session)
- APP08-V012 [L1/Framework] Verbose Rails errors (exposed routes) — `GET /cards/err` (param —)
- APP08-V013 [L2/Browser Security] Open redirect — `GET /login` (param redirect)
- APP08-V014 [L1/HTTP] Missing HTTP security headers — `GET /` (param —)
- APP08-V015 [L3/Reverse Proxy] Access to /rails/info via path normalization — `GET /rails/info/properties` (param —)
- APP08-V016 [L1/Web Server] Server and version banner disclosure — `GET /` (param Server / X-Powered-By)

## Negative controls (12) — SAFE and DISTINCT routes/parameters, tag `// NC: APP08-Nxxx`
Read and implement EXACTLY `negative-controls.yaml` (already corrected): each control is a safe route
distinct from the associated vulnerability.

## Cross-cutting
- Correct EVERYWHERE (input validation, ownership/role checks, parameterized queries, output encoding,
  hardened headers…) EXCEPT the listed deviations. Add NO vulnerability beyond the 16.
- `.ebextensions` = environment variables ONLY (AL2023 lesson: no deprecated option/`ProxyServer`).
- `.ebignore` excluding `benchmark/` and sensitive sources. `benchmark/` never served over HTTP.
- Technical documentation (`README.md`, `SECURITY_MODEL.md`), `benchmark/ground-truth.yaml` (16 entries, full
  schema, "never expose/provide to scanners" header), `benchmark/validation/validate.py`
  (1 test per vuln = PASS if vulnerable behavior confirmed; 1 test per NC = PASS if attack blocked;
  `POST /admin/reset` between cases; json/html reports; requirements.txt).
