> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-05 CréditFlux — Conventions de build (partagées)

Laboratoire DAST : application bancaire volontairement vulnérable, **données 100 % fictives**, usage défensif
(test de scanners). **Sécurisée par défaut** ; les vulnérabilités sont des **écarts localisés** et *uniquement*
celles listées. Aucune marque réelle. Réponses portant `X-Lab-Environment: test-data-fictives`.

## Pile et exécution
- Pile : Python 3.12, Django, rendu serveur
- Plateforme Elastic Beanstalk : **Python**
- Exécution : python manage.py runserver 0.0.0.0:8080 (local) ; EB Python (WSGI)
- Profil **standalone/local** sans base externe : SQLite (démarrage immédiat, données fictives).
- Démarrage : web: gunicorn creditflux.wsgi --bind 0.0.0.0:5000
- Seed au démarrage ; réinitialisation par `POST /admin/reset` (ou équivalent documenté).

## Spécificité
Django, upload multipart de justificatifs ; accent traitement de fichiers : upload non restreint, XXE, SSRF de pièce, désérialisation. Les charges XXE/SSRF ciblent exclusivement le laboratoire.

## Acteurs et données fictives
- Comptes de laboratoire : emprunteur1, emprunteur2, analyste1
- Hôte : `https://creditflux.lab.exemple`. Données 100 % fictives ; aucun secret réel.

## Endpoints et vulnérabilités (23) — écarts localisés, taguer `// GT: APP05-Vxxx`
- APP05-V001 [L2/Application] Upload de fichier non restreint — `POST /applications/{id}/documents` (param file)
- APP05-V002 [L3/Application] XXE via un document XML importé — `POST /import` (param file)
- APP05-V003 [L3/Application] SSRF à la récupération d'une pièce distante — `POST /documents/fetch` (param url)
- APP05-V004 [L3/Application] Traversée de chemin dans le nom de fichier uploadé — `POST /applications/{id}/documents` (param filename)
- APP05-V005 [L4/Application] Désérialisation non sûre (pickle) — `POST /import/session` (param data)
- APP05-V006 [L3/Application] IDOR sur le téléchargement de justificatif — `GET /documents/{id}` (param id)
- APP05-V007 [L4/Business Logic] Modification du montant du prêt après accord — `POST /applications/{id}/amount` (param amount)
- APP05-V008 [L2/Application] Injection SQL dans le filtre de suivi — `GET /applications` (param status)
- APP05-V009 [L3/Application] XSS stocké sur un commentaire analyste — `POST /applications/{id}/comment` (param text)
- APP05-V010 [L2/Application] CSRF sur la soumission d'une demande — `POST /applications/{id}/submit` (param —)
- APP05-V011 [L1/Framework] Django DEBUG=True (page d'erreur détaillée) — `GET /applications/err` (param —)
- APP05-V012 [L2/Browser Security] Redirection ouverte via le paramètre next — `GET /login` (param next)
- APP05-V013 [L1/HTTP] Absence de HSTS — `GET /` (param —)
- APP05-V014 [L1/HTTP] Absence de X-Content-Type-Options (MIME sniffing) — `GET /static/app.js` (param —)
- APP05-V015 [L3/Web Server] Contournement du contrôle d'upload par polyglotte MIME — `POST /applications/{id}/documents` (param Content-Type)
- APP05-V016 [L1/Web Server] Divulgation de bannière serveur et de version — `GET /` (param Server / X-Powered-By)
- APP05-V017 [L1/Browser Security] Cookie de session sans Secure ni SameSite — `POST /login` (param Set-Cookie)

## Negative controls (12) — routes/paramètres SÛRS et DISTINCTS, taguer `// NC: APP05-Nxxx`
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

# APP-05 CréditFlux — Build conventions (shared)

DAST lab: intentionally vulnerable banking application, **100% fictitious data**, defensive use
(scanner testing). **Secure by default**; the vulnerabilities are **localized deviations** and *only*
those listed. No real brand. Responses carrying `X-Lab-Environment: test-data-fictives`.

## Stack and execution
- Stack: Python 3.12, Django, server-side rendering
- Elastic Beanstalk platform: **Python**
- Execution: python manage.py runserver 0.0.0.0:8080 (local); EB Python (WSGI)
- **Standalone/local** profile without an external database: SQLite (immediate startup, fictitious data).
- Startup: web: gunicorn creditflux.wsgi --bind 0.0.0.0:5000
- Seed on startup; reset via `POST /admin/reset` (or documented equivalent).

## Specificity
Django, multipart upload of supporting documents; focus on file handling: unrestricted upload, XXE, attachment SSRF, deserialization. XXE/SSRF payloads target the lab exclusively.

## Actors and fictitious data
- Lab accounts: emprunteur1, emprunteur2, analyste1
- Host: `https://creditflux.lab.exemple`. 100% fictitious data; no real secret.

## Endpoints and vulnerabilities (23) — localized deviations, tag `// GT: APP05-Vxxx`
- APP05-V001 [L2/Application] Unrestricted file upload — `POST /applications/{id}/documents` (param file)
- APP05-V002 [L3/Application] XXE via an imported XML document — `POST /import` (param file)
- APP05-V003 [L3/Application] SSRF when fetching a remote attachment — `POST /documents/fetch` (param url)
- APP05-V004 [L3/Application] Path traversal in the uploaded filename — `POST /applications/{id}/documents` (param filename)
- APP05-V005 [L4/Application] Unsafe deserialization (pickle) — `POST /import/session` (param data)
- APP05-V006 [L3/Application] IDOR on supporting-document download — `GET /documents/{id}` (param id)
- APP05-V007 [L4/Business Logic] Loan amount modification after approval — `POST /applications/{id}/amount` (param amount)
- APP05-V008 [L2/Application] SQL injection in the tracking filter — `GET /applications` (param status)
- APP05-V009 [L3/Application] Stored XSS on an analyst comment — `POST /applications/{id}/comment` (param text)
- APP05-V010 [L2/Application] CSRF on application submission — `POST /applications/{id}/submit` (param —)
- APP05-V011 [L1/Framework] Django DEBUG=True (detailed error page) — `GET /applications/err` (param —)
- APP05-V012 [L2/Browser Security] Open redirect via the next parameter — `GET /login` (param next)
- APP05-V013 [L1/HTTP] Missing HSTS — `GET /` (param —)
- APP05-V014 [L1/HTTP] Missing X-Content-Type-Options (MIME sniffing) — `GET /static/app.js` (param —)
- APP05-V015 [L3/Web Server] Upload-control bypass via MIME polyglot — `POST /applications/{id}/documents` (param Content-Type)
- APP05-V016 [L1/Web Server] Server banner and version disclosure — `GET /` (param Server / X-Powered-By)
- APP05-V017 [L1/Browser Security] Session cookie without Secure or SameSite — `POST /login` (param Set-Cookie)

## Negative controls (12) — SAFE and DISTINCT routes/parameters, tag `// NC: APP05-Nxxx`
Read and implement EXACTLY `negative-controls.yaml` (already corrected): each control is a safe route
distinct from the associated vulnerability.

## Cross-cutting
- Correct EVERYWHERE (input validation, ownership/role check, parameterized queries, output encoding,
  hardened headers…) EXCEPT the listed deviations. Add NO vulnerability beyond the 16.
- `.ebextensions` = environment variables ONLY (AL2023 lesson: no deprecated option/`ProxyServer`).
- `.ebignore` excluding `benchmark/` and sensitive sources. `benchmark/` never served over HTTP.
- Technical documentation (`README.md`, `SECURITY_MODEL.md`), `benchmark/ground-truth.yaml` (16 entries, full
  schema, header "never expose/provide to scanners"), `benchmark/validation/validate.py`
  (1 test per vuln = PASS if vulnerable behavior confirmed; 1 test per NC = PASS if attack blocked;
  `POST /admin/reset` between cases; json/html reports; requirements.txt).
