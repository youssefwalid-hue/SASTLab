> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-07 PayGate XML — Conventions de build (partagées)

Laboratoire DAST : application bancaire volontairement vulnérable, **données 100 % fictives**, usage défensif
(test de scanners). **Sécurisée par défaut** ; les vulnérabilités sont des **écarts localisés** et *uniquement*
celles listées. Aucune marque réelle. Réponses portant `X-Lab-Environment: test-data-fictives`.

## Pile et exécution
- Pile : .NET Framework (C#), WCF
- Plateforme Elastic Beanstalk : **.NET sur Windows Server (IIS)**
- Exécution : (Windows uniquement : WCF/.NET Framework) — non exécutable sur Linux ici
- Profil **standalone/local** sans base externe : SQL Server LocalDB / dépôt mémoire (démarrage immédiat, données fictives).
- Démarrage : (plateforme EB .NET Windows / IIS — web.config)
- Seed au démarrage ; réinitialisation par `POST /admin/reset` (ou équivalent documenté).

## Spécificité
Service SOAP/XML WCF (.NET Framework), POST /PaymentService.svc, WSDL via ?wsdl ; accent SOAP/XML : XXE, injection XML, en-tête wsse falsifiable, billion laughs, spécificités IIS. Enveloppes SOAP réalistes ; charges XXE/entity ciblant exclusivement le laboratoire. Écrire le code même sans compilateur disponible.

## Acteurs et données fictives
- Comptes de laboratoire : operateur-ent (jeton SOAP), service-interne
- Hôte : `https://paygate.lab.exemple`. Données 100 % fictives ; aucun secret réel.

## Endpoints et vulnérabilités (21) — écarts localisés, taguer `// GT: APP07-Vxxx`
- APP07-V001 [L3/Application] XXE dans une remise d'ordre SOAP — `POST /PaymentService.svc` (param corps XML)
- APP07-V002 [L3/Application] Injection XML / modification du montant — `POST /PaymentService.svc` (param élément amount)
- APP07-V003 [L3/API] En-tête SOAP d'authentification falsifiable — `POST /PaymentService.svc` (param wsse:UsernameToken)
- APP07-V004 [L1/API] WSDL exposant des opérations internes — `GET /PaymentService.svc?wsdl` (param —)
- APP07-V005 [L3/API] BFLA : opération d'administration SOAP — `POST /PaymentService.svc` (param CancelAllBatches)
- APP07-V006 [L3/API] Déni de service par expansion d'entités (billion laughs) — `POST /PaymentService.svc` (param DTD)
- APP07-V007 [L4/Business Logic] Rejeu d'ordre (absence de nonce) — `POST /PaymentService.svc` (param SubmitOrder)
- APP07-V008 [L3/Web Server] Divulgation de noms courts 8.3 (IIS) — `GET /~1` (param —)
- APP07-V009 [L1/Framework] Erreurs IIS/.NET verbeuses (version divulguée) — `GET /PaymentService.svc/err` (param —)
- APP07-V010 [L2/Web Server] Listing de répertoire des fichiers de lot — `GET /batches/` (param —)
- APP07-V011 [L2/Application] Injection SQL sur la consultation d'ordre — `POST /PaymentService.svc` (param GetOrder(ref))
- APP07-V012 [L2/Web Server] Méthodes WebDAV activées — `OPTIONS /` (param —)
- APP07-V013 [L1/HTTP] Configuration TLS/headers faible — `GET /` (param —)
- APP07-V014 [L3/Application] Traversée de chemin au téléchargement d'accusé — `GET /receipts/download` (param name)
- APP07-V015 [L4/Framework] Désérialisation ViewState (MAC désactivé) — `POST /legacy/Report.aspx` (param __VIEWSTATE)

## Negative controls (13) — routes/paramètres SÛRS et DISTINCTS, taguer `// NC: APP07-Nxxx`
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

# APP-07 PayGate XML — Build conventions (shared)

DAST laboratory: intentionally vulnerable banking application, **100% fictitious data**, defensive use
(scanner testing). **Secure by default**; the vulnerabilities are **localized gaps** and *only*
those listed. No real brand. Responses carrying `X-Lab-Environment: test-data-fictives`.

## Stack and execution
- Stack: .NET Framework (C#), WCF
- Elastic Beanstalk platform: **.NET on Windows Server (IIS)**
- Execution: (Windows only: WCF/.NET Framework) — not runnable on Linux here
- **Standalone/local** profile without external database: SQL Server LocalDB / in-memory store (immediate startup, fictitious data).
- Startup: (EB .NET Windows / IIS platform — web.config)
- Seed at startup; reset via `POST /admin/reset` (or documented equivalent).

## Specificity
WCF SOAP/XML service (.NET Framework), POST /PaymentService.svc, WSDL via ?wsdl ; SOAP/XML emphasis: XXE, XML injection, forgeable wsse header, billion laughs, IIS specificities. Realistic SOAP envelopes; XXE/entity payloads targeting the laboratory exclusively. Write the code even without an available compiler.

## Actors and fictitious data
- Laboratory accounts: operateur-ent (SOAP token), service-interne
- Host: `https://paygate.lab.exemple`. 100% fictitious data; no real secret.

## Endpoints and vulnerabilities (21) — localized gaps, tag `// GT: APP07-Vxxx`
- APP07-V001 [L3/Application] XXE in a SOAP order submission — `POST /PaymentService.svc` (XML body param)
- APP07-V002 [L3/Application] XML injection / amount modification — `POST /PaymentService.svc` (amount element param)
- APP07-V003 [L3/API] Forgeable SOAP authentication header — `POST /PaymentService.svc` (wsse:UsernameToken param)
- APP07-V004 [L1/API] WSDL exposing internal operations — `GET /PaymentService.svc?wsdl` (param —)
- APP07-V005 [L3/API] BFLA: SOAP administration operation — `POST /PaymentService.svc` (CancelAllBatches param)
- APP07-V006 [L3/API] Denial of service via entity expansion (billion laughs) — `POST /PaymentService.svc` (DTD param)
- APP07-V007 [L4/Business Logic] Order replay (no nonce) — `POST /PaymentService.svc` (SubmitOrder param)
- APP07-V008 [L3/Web Server] 8.3 short-name disclosure (IIS) — `GET /~1` (param —)
- APP07-V009 [L1/Framework] Verbose IIS/.NET errors (version disclosed) — `GET /PaymentService.svc/err` (param —)
- APP07-V010 [L2/Web Server] Directory listing of batch files — `GET /batches/` (param —)
- APP07-V011 [L2/Application] SQL injection on order lookup — `POST /PaymentService.svc` (GetOrder(ref) param)
- APP07-V012 [L2/Web Server] WebDAV methods enabled — `OPTIONS /` (param —)
- APP07-V013 [L1/HTTP] Weak TLS/headers configuration — `GET /` (param —)
- APP07-V014 [L3/Application] Path traversal on acknowledgement download — `GET /receipts/download` (name param)
- APP07-V015 [L4/Framework] ViewState deserialization (MAC disabled) — `POST /legacy/Report.aspx` (__VIEWSTATE param)

## Negative controls (13) — SAFE and DISTINCT routes/parameters, tag `// NC: APP07-Nxxx`
Read and implement EXACTLY `negative-controls.yaml` (already corrected): each control is a safe route
distinct from the associated vulnerability.

## Cross-cutting
- Correct EVERYWHERE (input validation, ownership/role control, parameterized queries, output encoding,
  hardened headers…) EXCEPT the listed gaps. Do NOT add ANY vulnerability outside the 16.
- `.ebextensions` = environment variables ONLY (AL2023 lesson: no deprecated option/`ProxyServer`).
- `.ebignore` excluding `benchmark/` and the sensitive sources. `benchmark/` never served over HTTP.
- Technical documentation (`README.md`, `SECURITY_MODEL.md`), `benchmark/ground-truth.yaml` (16 entries, full
  schema, "never expose / provide to scanners" header), `benchmark/validation/validate.py`
  (1 test per vuln = PASS if vulnerable behavior confirmed; 1 test per NC = PASS if attack blocked;
  `POST /admin/reset` between cases; json/html reports; requirements.txt).
