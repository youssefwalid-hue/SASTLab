> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-06 MobiPay — Conventions de build (partagées)

Laboratoire DAST : application bancaire volontairement vulnérable, **données 100 % fictives**, usage défensif
(test de scanners). **Sécurisée par défaut** ; les vulnérabilités sont des **écarts localisés** et *uniquement*
celles listées. Aucune marque réelle. Réponses portant `X-Lab-Environment: test-data-fictives`.

## Pile et exécution
- Pile : Go 1.22, gqlgen
- Plateforme Elastic Beanstalk : **Go**
- Exécution : go run . (local 8080) ; EB Go (binaire)
- Profil **standalone/local** sans base externe : dépôt mémoire (démarrage immédiat, données fictives).
- Démarrage : web: ./mobipay (port 5000)
- Seed au démarrage ; réinitialisation par `POST /admin/reset` (ou équivalent documenté).

## Spécificité
API GraphQL (gqlgen), endpoint unique POST /graphql ; accent GraphQL : BOLA via node(id), exposition de champs, introspection, profondeur/coût, batching/alias d'OTP. Fournir des queries/mutations concrètes.

## Acteurs et données fictives
- Comptes de laboratoire : mobile-alice, mobile-bruno, mobile-admin
- Hôte : `https://mobipay.lab.exemple`. Données 100 % fictives ; aucun secret réel.

## Endpoints et vulnérabilités (22) — écarts localisés, taguer `// GT: APP06-Vxxx`
- APP06-V001 [L3/API] BOLA via le résolveur node(id) — `POST /graphql` (param node(id))
- APP06-V002 [L2/API] Exposition excessive de champs sensibles (PAN/CVV) — `POST /graphql` (param card{pan cvv})
- APP06-V003 [L1/API] Introspection GraphQL activée en production — `POST /graphql` (param __schema)
- APP06-V004 [L3/API] Absence de limite de profondeur/coût (DoS) — `POST /graphql` (param query imbriquée)
- APP06-V005 [L3/API] BFLA : mutation d'administration accessible — `POST /graphql` (param blockAllCards)
- APP06-V006 [L3/API] Mass assignment via la mutation updateProfile — `POST /graphql` (param role)
- APP06-V007 [L3/API] Empreinte d'appareil contournable — `POST /graphql` (param X-Device-Id)
- APP06-V008 [L4/API] Brute force d'OTP via alias/batching — `POST /graphql` (param verifyOtp (alias))
- APP06-V009 [L3/API] IDOR sur les notifications — `POST /graphql` (param notifications(userId))
- APP06-V010 [L3/API] Injection SQL via un filtre GraphQL — `POST /graphql` (param transactions(filter))
- APP06-V011 [L1/API] Fuite d'information dans les erreurs de résolveur — `POST /graphql` (param requête malformée)
- APP06-V012 [L3/API] Contournement du mécanisme de requêtes persistées — `POST /graphql` (param query libre)
- APP06-V013 [L2/Browser Security] CORS trop permissif — `POST /graphql` (param Origin)
- APP06-V014 [L1/HTTP] En-têtes de sécurité HTTP absents — `GET /healthz` (param —)
- APP06-V015 [L3/API] SSRF via l'URL d'avatar (mutation) — `POST /graphql` (param setAvatar(url))
- APP06-V016 [L1/Web Server] Divulgation de bannière serveur et de version — `GET /healthz` (param Server / X-Powered-By)

## Negative controls (13) — routes/paramètres SÛRS et DISTINCTS, taguer `// NC: APP06-Nxxx`
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

# APP-06 MobiPay — Build conventions (shared)

DAST lab: intentionally vulnerable banking application, **100 % fictitious data**, defensive use
(scanner testing). **Secure by default**; the vulnerabilities are **localized deviations** and *only*
those listed. No real brand. Responses carrying `X-Lab-Environment: test-data-fictives`.

## Stack and execution
- Stack: Go 1.22, gqlgen
- Elastic Beanstalk platform: **Go**
- Execution: go run . (local 8080); EB Go (binary)
- **standalone/local** profile with no external database: in-memory repository (instant startup, fictitious data).
- Startup: web: ./mobipay (port 5000)
- Seed at startup; reset via `POST /admin/reset` (or documented equivalent).

## Specificity
GraphQL API (gqlgen), single endpoint POST /graphql; GraphQL focus: BOLA via node(id), field exposure, introspection, depth/cost, OTP batching/aliasing. Provide concrete queries/mutations.

## Actors and fictitious data
- Lab accounts: mobile-alice, mobile-bruno, mobile-admin
- Host: `https://mobipay.lab.exemple`. 100 % fictitious data; no real secret.

## Endpoints and vulnerabilities (22) — localized deviations, tag `// GT: APP06-Vxxx`
- APP06-V001 [L3/API] BOLA via the node(id) resolver — `POST /graphql` (param node(id))
- APP06-V002 [L2/API] Excessive exposure of sensitive fields (PAN/CVV) — `POST /graphql` (param card{pan cvv})
- APP06-V003 [L1/API] GraphQL introspection enabled in production — `POST /graphql` (param __schema)
- APP06-V004 [L3/API] No depth/cost limit (DoS) — `POST /graphql` (param nested query)
- APP06-V005 [L3/API] BFLA: accessible administration mutation — `POST /graphql` (param blockAllCards)
- APP06-V006 [L3/API] Mass assignment via the updateProfile mutation — `POST /graphql` (param role)
- APP06-V007 [L3/API] Bypassable device fingerprint — `POST /graphql` (param X-Device-Id)
- APP06-V008 [L4/API] OTP brute force via aliasing/batching — `POST /graphql` (param verifyOtp (alias))
- APP06-V009 [L3/API] IDOR on notifications — `POST /graphql` (param notifications(userId))
- APP06-V010 [L3/API] SQL injection via a GraphQL filter — `POST /graphql` (param transactions(filter))
- APP06-V011 [L1/API] Information leak in resolver errors — `POST /graphql` (param malformed query)
- APP06-V012 [L3/API] Persisted-queries mechanism bypass — `POST /graphql` (param free-form query)
- APP06-V013 [L2/Browser Security] Overly permissive CORS — `POST /graphql` (param Origin)
- APP06-V014 [L1/HTTP] Missing HTTP security headers — `GET /healthz` (param —)
- APP06-V015 [L3/API] SSRF via the avatar URL (mutation) — `POST /graphql` (param setAvatar(url))
- APP06-V016 [L1/Web Server] Server banner and version disclosure — `GET /healthz` (param Server / X-Powered-By)

## Negative controls (13) — SAFE and DISTINCT routes/parameters, tag `// NC: APP06-Nxxx`
Read and implement EXACTLY `negative-controls.yaml` (already fixed): each control is a safe route
distinct from the associated vulnerability.

## Cross-cutting
- Correct EVERYWHERE (input validation, ownership/role check, parameterized queries, output encoding,
  hardened headers…) EXCEPT the listed deviations. Add NO vulnerability outside the 16.
- `.ebextensions` = environment variables ONLY (AL2023 lesson: no deprecated option/`ProxyServer`).
- `.ebignore` excluding `benchmark/` and the sensitive sources. `benchmark/` never served over HTTP.
- Technical documentation (`README.md`, `SECURITY_MODEL.md`), `benchmark/ground-truth.yaml` (16 entries, full
  schema, header "never expose/supply to scanners"), `benchmark/validation/validate.py`
  (1 test per vuln = PASS if vulnerable behavior confirmed; 1 test per NC = PASS if attack blocked;
  `POST /admin/reset` between cases; json/html reports; requirements.txt).
