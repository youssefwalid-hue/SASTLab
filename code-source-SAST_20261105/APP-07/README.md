> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-07 "PayGate XML" - Passerelle de paiements (SOAP/XML)

> **Environnement de test - donnees 100% fictives.** Application du *Banking DAST Benchmark Lab*,
> **volontairement vulnerable**, a usage **defensif** (evaluation de scanners DAST en laboratoire interne).
> Aucune marque reelle, aucun secret reel. Toutes les reponses portent l'en-tete
> `X-Lab-Environment: test-data-fictives`.

## Pile technique

- **.NET Framework (C#) + WCF**, heberge sous **IIS / Windows Server**.
- Service SOAP `POST /PaymentService.svc`, contrat **WSDL** via `GET /PaymentService.svc?wsdl`.
- Authentification : HTTP + **jeton dans l'en-tete SOAP** (`wsse:UsernameToken`, WS-Security simule).
- Stockage : **depot memoire** par defaut (`DB_PROFILE=memory`, demarrage immediat) ou
  **SQL Server LocalDB / RDS** (`DB_PROFILE=localdb`, `DB_CONNSTRING=...`).

> **Compilation** : le SDK .NET Framework / WCF (MSBuild Windows) n'est pas executable sur
> l'environnement de build Linux. Le code source est complet mais **n'a pas ete compile** ici ;
> il se compile et se deploie sous Windows (Visual Studio / MSBuild + IIS).

## Operations SOAP (contrat public)

| Operation | Role | Note |
|-----------|------|------|
| `SubmitOrder` | metier | flux cible par V001/V002/V006/V007 |
| `GetOrder` | metier | flux cible par V011 (SQLi) et V003 |
| `ListBatches` | metier | consultation des lots |
| `CancelAllBatches` | **admin (fuite)** | V004 (WSDL) + V005 (BFLA) |
| `SubmitOrderV2` | metier | **NC N001** (parseur durci) |
| `QuoteOrder` | metier | **NC N002** (XSD strict) |
| `SubmitDirectDebit` | metier | **NC N006** (anti-rejeu) |
| `GetOrderStatus` | metier | **NC N007** (requete parametree) |
| `GetAccountStatement` | metier | **NC N003** (jeton verifie) |

Endpoint d'administration **interne** (non publie, acces refuse depuis l'exterieur) :
`AdminService.svc` -> `PurgeArchivedBatches` (**NC N004 / N005**).

## Endpoints HTTP complementaires

- `POST /admin/reset` - reinitialise l'etat de demonstration (seed fictif).
- `GET /receipts/download?name=...` - telechargement d'accuse (**V014** : traversee).
- `GET /statements/download?name=...` - telechargement de releve (**NC N007** : confine).
- `GET /batches/` - **V010** (directory listing).
- `GET /PaymentService.svc/err` - **V009** (erreur verbeuse / version).
- `POST /legacy/Report.aspx` - **V015** (ViewState sans MAC).

## Comptes de laboratoire (fictifs)

| Compte | Usage | Mot de passe | OTP |
|--------|-------|--------------|-----|
| `operateur-ent` | jeton SOAP operateur | `Password1!` | `123456` |
| `service-interne` | identite interne / admin | `Password1!` | `123456` |

## Demarrage local (Windows / IIS Express)

1. Ouvrir `PayGate.csproj` sous Visual Studio, lancer sur le port `8080` (IIS Express), ou
   deployer le dossier sous IIS.
2. L'etat est seede au demarrage ; `POST /admin/reset` le reinitialise.
3. WSDL : `GET http://localhost:8080/PaymentService.svc?wsdl`.

## Deploiement AWS (Elastic Beanstalk)

- Plateforme **.NET sur Windows Server (IIS)**, instance unique, HTTPS via ALB + ACM
  (cf. `ARCHITECTURE.md`).
- `.ebextensions/01-options.config` : **variables d'environnement uniquement**
  (`APP_PROFILE`, `DB_PROFILE`, `DB_CONNSTRING`, `LAB_ENVIRONMENT`).
- `.ebignore` exclut `benchmark/`, `fixtures/`, la documentation sensible et les sources.

## Securite

Voir `SECURITY_MODEL.md`. L'application est **securisee par defaut** ; les **21 ecarts**
du referentiel (`benchmark/ground-truth.yaml`, non expose) sont **localises et tagues**
(`// GT: APP07-Vxxx`). Six **negative controls** (`// NC: APP07-Nxxx`) fournissent des
routes comparables correctement securisees.

---

# English version

# APP-07 "PayGate XML" - Payment gateway (SOAP/XML)

> **Test environment - 100% fictitious data.** Application of the *Banking DAST Benchmark Lab*,
> **intentionally vulnerable**, for **defensive** use (in-house laboratory evaluation of DAST scanners).
> No real brand, no real secret. Every response carries the header
> `X-Lab-Environment: test-data-fictives`.

## Technical stack

- **.NET Framework (C#) + WCF**, hosted under **IIS / Windows Server**.
- SOAP service `POST /PaymentService.svc`, **WSDL** contract via `GET /PaymentService.svc?wsdl`.
- Authentication: HTTP + **token in the SOAP header** (`wsse:UsernameToken`, simulated WS-Security).
- Storage: **in-memory store** by default (`DB_PROFILE=memory`, immediate startup) or
  **SQL Server LocalDB / RDS** (`DB_PROFILE=localdb`, `DB_CONNSTRING=...`).

> **Compilation**: the .NET Framework / WCF SDK (MSBuild Windows) is not runnable on
> the Linux build environment. The source code is complete but **has not been compiled** here;
> it compiles and deploys under Windows (Visual Studio / MSBuild + IIS).

## SOAP operations (public contract)

| Operation | Role | Note |
|-----------|------|------|
| `SubmitOrder` | business | flow targeted by V001/V002/V006/V007 |
| `GetOrder` | business | flow targeted by V011 (SQLi) and V003 |
| `ListBatches` | business | batch lookup |
| `CancelAllBatches` | **admin (leak)** | V004 (WSDL) + V005 (BFLA) |
| `SubmitOrderV2` | business | **NC N001** (hardened parser) |
| `QuoteOrder` | business | **NC N002** (strict XSD) |
| `SubmitDirectDebit` | business | **NC N006** (anti-replay) |
| `GetOrderStatus` | business | **NC N007** (parameterized query) |
| `GetAccountStatement` | business | **NC N003** (verified token) |

**Internal** administration endpoint (unpublished, access denied from the outside):
`AdminService.svc` -> `PurgeArchivedBatches` (**NC N004 / N005**).

## Additional HTTP endpoints

- `POST /admin/reset` - resets the demonstration state (fictitious seed).
- `GET /receipts/download?name=...` - acknowledgement download (**V014**: traversal).
- `GET /statements/download?name=...` - statement download (**NC N007**: confined).
- `GET /batches/` - **V010** (directory listing).
- `GET /PaymentService.svc/err` - **V009** (verbose error / version).
- `POST /legacy/Report.aspx` - **V015** (ViewState without MAC).

## Laboratory accounts (fictitious)

| Account | Usage | Password | OTP |
|--------|-------|--------------|-----|
| `operateur-ent` | SOAP operator token | `Password1!` | `123456` |
| `service-interne` | internal / admin identity | `Password1!` | `123456` |

## Local startup (Windows / IIS Express)

1. Open `PayGate.csproj` under Visual Studio, run on port `8080` (IIS Express), or
   deploy the folder under IIS.
2. State is seeded at startup; `POST /admin/reset` resets it.
3. WSDL: `GET http://localhost:8080/PaymentService.svc?wsdl`.

## AWS deployment (Elastic Beanstalk)

- **.NET on Windows Server (IIS)** platform, single instance, HTTPS via ALB + ACM
  (see `ARCHITECTURE.md`).
- `.ebextensions/01-options.config`: **environment variables only**
  (`APP_PROFILE`, `DB_PROFILE`, `DB_CONNSTRING`, `LAB_ENVIRONMENT`).
- `.ebignore` excludes `benchmark/`, `fixtures/`, sensitive documentation and the sources.

## Security

See `SECURITY_MODEL.md`. The application is **secure by default**; the **21 gaps**
of the reference set (`benchmark/ground-truth.yaml`, not exposed) are **localized and tagged**
(`// GT: APP07-Vxxx`). Six **negative controls** (`// NC: APP07-Nxxx`) provide
comparable, correctly secured routes.
