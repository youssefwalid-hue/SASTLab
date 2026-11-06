# APP-01 Hélios Banque — Conventions de build (partagées)

> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

Laboratoire DAST : application bancaire volontairement vulnérable, **données 100 % fictives**, usage défensif
(test de scanners). **Sécurisée par défaut** ; les vulnérabilités sont des **écarts localisés** et *uniquement*
celles listées. Aucune marque réelle. Bandeau permanent « Environnement de test – données fictives ».

## Pile et exécution
- Java 21, Spring Boot 3.3.x, Maven, Spring Security, Thymeleaf (SSR), Spring Data JPA.
- Package de base : `com.lab.helios`.
- Deux profils :
  - `local` : base **H2 en mémoire**, port **8080** (pour exécution et validation locales).
  - `prod` : **PostgreSQL** via variables d'env `DB_URL`, `DB_USER`, `DB_PASS`, port **5000** (Elastic Beanstalk).
- Artefact : **JAR exécutable** (Tomcat embarqué), déployable sur EB plateforme Corretto 21 derrière Nginx.
- Seed au démarrage via `CommandLineRunner` ; réinitialisation par `POST /admin/reset`.

## Comptes de laboratoire (BCrypt, mot de passe `Password1!`)
- `alice` (CLIENT, possède compte **ACC-1001**)
- `bruno` (CLIENT, possède compte **ACC-2001**)
- `claire` (ADVISOR)
- `admin` (ADMIN)
- OTP de laboratoire fixe : `123456`.

## Entités
Utilisateur(role), Compte(ACC-xxxx, solde), Opération(label, montant, date, counterparty), Bénéficiaire(name,
iban, note, activatedAt), Virement(source, beneficiary, amount, status: PENDING_OTP/EXECUTED, otpValidated),
Relevé(fichier PDF fictif), PasswordReset(token, reset_url).

## Endpoints (EXACTS — repris du VULNERABILITY_GUIDE.md ; les routes ne doivent pas trahir la vuln)
- `POST /login` (username, password, returnUrl, otp si 2FA)
- `GET /` dashboard · `GET /search?q=` · `GET /accounts/{accountId}` · `GET /accounts/{accountId}/transactions?label=&counterparty=`
- `GET /beneficiaries` · `POST /beneficiaries` (name, iban, note) · `POST /beneficiaries/{id}/edit` (activatedAt)
- `GET /statements/download?file=` · `GET /statements/by-id/{statementId}` (NC)
- `POST /transfers` (sourceAccount, beneficiaryId, amount) · `POST /transfers/{id}/confirm` (otp) · `POST /transfers/{id}/finalize` (NC)
- `GET /api/accounts/{accountId}/balance` · `GET /api/accounts/{accountId}/summary` (NC)
- `POST /password/forgot` · `POST /password/reset` (email) · `POST /password/reset-secure` (NC)
- `POST /admin/reset`
- Actuator exposé : `/actuator/env` (vuln V013)

## Vulnérabilités (22) — écarts localisés, taguer en commentaire `// GT: APP01-Vxxx`
V001 SQLi `transactions?label` (concaténation) · V002 XSS stocké bénéficiaire name (`th:utext`) ·
V003 XSS réfléchi `search?q` (`th:utext`) · V004 path traversal `statements/download?file` ·
V005 IDOR `GET /accounts/{accountId}` (pas de contrôle de propriété) · V006 BOLA `GET /api/accounts/{id}/balance` ·
V007 bypass 2FA `transfers/{id}/confirm` (otpValidated non vérifié) · V008 `beneficiaries/{id}/edit` activatedAt (mass-assign) ·
V009 cookie session sans HttpOnly/SameSite · V010 open redirect `returnUrl` après login ·
V011 HSTS absent · V012 Host header poisoning `password/reset` · V013 `/actuator/env` exposé ·
V014 TRACE activé · V015 contournement proxy par normalisation de chemin (config Nginx `.platform/`) ·
V016 bannière serveur/version divulguée (Server/X-Application-Version).

## Negative controls (13) — routes comparables SÛRES (taguer `// NC: APP01-Nxxx`)
N001 `transactions?counterparty` requête paramétrée · N002 note bénéficiaire échappée (`th:text`) ·
N003 `statements/by-id/{id}` par id interne + contrôle propriété · N004 `api/accounts/{id}/summary` + contrôle propriété ·
N005 `transfers/{id}/finalize` revérifie l'OTP · N006 cookie remember-me durci (Secure/HttpOnly/SameSite) ·
N007 `password/reset-secure` URL de base canonique (Host ignoré).

## UI / sécurité transverse
- Fragment Thymeleaf « layout » : en-tête (logo textuel « Hélios Banque »), nav, pied de page fictif,
  **bandeau permanent** « Environnement de test – données fictives » (non masquable), design bancaire sobre et accessible (AA).
- Filtre ajoutant l'en-tête `X-Lab-Environment: test-data-fictives` à toutes les réponses.
- Respect de `X-Forwarded-Proto` (HTTPS derrière ALB) ; cookies de session `Secure` en prod SAUF l'écart V009.
- Aucun secret réel ; aucune donnée réelle. `benchmark/` jamais servi par HTTP.

---

# English version

# APP-01 Hélios Banque — Build conventions (shared)

DAST laboratory: intentionally vulnerable banking application, **100% fictitious data**, defensive use
(scanner testing). **Secure by default**; vulnerabilities are **localized deviations** and *only*
those listed. No real brand. Permanent banner "Environnement de test – données fictives".

## Stack and execution
- Java 21, Spring Boot 3.3.x, Maven, Spring Security, Thymeleaf (SSR), Spring Data JPA.
- Base package: `com.lab.helios`.
- Two profiles:
  - `local`: **in-memory H2** database, port **8080** (for local execution and validation).
  - `prod`: **PostgreSQL** via env variables `DB_URL`, `DB_USER`, `DB_PASS`, port **5000** (Elastic Beanstalk).
- Artifact: **executable JAR** (embedded Tomcat), deployable on EB Corretto 21 platform behind Nginx.
- Seed at startup via `CommandLineRunner`; reset through `POST /admin/reset`.

## Lab accounts (BCrypt, password `Password1!`)
- `alice` (CLIENT, owns account **ACC-1001**)
- `bruno` (CLIENT, owns account **ACC-2001**)
- `claire` (ADVISOR)
- `admin` (ADMIN)
- Fixed lab OTP: `123456`.

## Entities
User(role), Account(ACC-xxxx, balance), Operation(label, amount, date, counterparty), Beneficiary(name,
iban, note, activatedAt), Transfer(source, beneficiary, amount, status: PENDING_OTP/EXECUTED, otpValidated),
Statement(fictitious PDF file), PasswordReset(token, reset_url).

## Endpoints (EXACT — taken from VULNERABILITY_GUIDE.md; the routes must not betray the vuln)
- `POST /login` (username, password, returnUrl, otp if 2FA)
- `GET /` dashboard · `GET /search?q=` · `GET /accounts/{accountId}` · `GET /accounts/{accountId}/transactions?label=&counterparty=`
- `GET /beneficiaries` · `POST /beneficiaries` (name, iban, note) · `POST /beneficiaries/{id}/edit` (activatedAt)
- `GET /statements/download?file=` · `GET /statements/by-id/{statementId}` (NC)
- `POST /transfers` (sourceAccount, beneficiaryId, amount) · `POST /transfers/{id}/confirm` (otp) · `POST /transfers/{id}/finalize` (NC)
- `GET /api/accounts/{accountId}/balance` · `GET /api/accounts/{accountId}/summary` (NC)
- `POST /password/forgot` · `POST /password/reset` (email) · `POST /password/reset-secure` (NC)
- `POST /admin/reset`
- Exposed Actuator: `/actuator/env` (vuln V013)

## Vulnerabilities (22) — localized deviations, tag in a `// GT: APP01-Vxxx` comment
V001 SQLi `transactions?label` (concatenation) · V002 stored XSS beneficiary name (`th:utext`) ·
V003 reflected XSS `search?q` (`th:utext`) · V004 path traversal `statements/download?file` ·
V005 IDOR `GET /accounts/{accountId}` (no ownership control) · V006 BOLA `GET /api/accounts/{id}/balance` ·
V007 2FA bypass `transfers/{id}/confirm` (otpValidated not checked) · V008 `beneficiaries/{id}/edit` activatedAt (mass-assign) ·
V009 session cookie without HttpOnly/SameSite · V010 open redirect `returnUrl` after login ·
V011 HSTS missing · V012 Host header poisoning `password/reset` · V013 `/actuator/env` exposed ·
V014 TRACE enabled · V015 proxy bypass through path normalization (Nginx config `.platform/`) ·
V016 server/version banner disclosed (Server/X-Application-Version).

## Negative controls (13) — comparable SAFE routes (tag `// NC: APP01-Nxxx`)
N001 `transactions?counterparty` parameterized query · N002 escaped beneficiary note (`th:text`) ·
N003 `statements/by-id/{id}` by internal id + ownership control · N004 `api/accounts/{id}/summary` + ownership control ·
N005 `transfers/{id}/finalize` re-checks the OTP · N006 hardened remember-me cookie (Secure/HttpOnly/SameSite) ·
N007 `password/reset-secure` canonical base URL (Host ignored).

## UI / cross-cutting security
- Thymeleaf "layout" fragment: header (textual logo "Hélios Banque"), nav, fictitious footer,
  **permanent banner** "Environnement de test – données fictives" (non-dismissible), sober and accessible banking design (AA).
- Filter adding the `X-Lab-Environment: test-data-fictives` header to all responses.
- Respect of `X-Forwarded-Proto` (HTTPS behind ALB); `Secure` session cookies in prod EXCEPT deviation V009.
- No real secret; no real data. `benchmark/` never served over HTTP.
