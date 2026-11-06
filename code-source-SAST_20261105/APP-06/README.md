> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# MobiPay (APP-06) — API mobile GraphQL volontairement vulnérable

> **Environnement de laboratoire — DONNÉES 100 % FICTIVES — usage défensif.**
> MobiPay fait partie du **Banking DAST Benchmark Lab**. L'application est
> **sécurisée par défaut** ; elle ne contient que **22 vulnérabilités
> volontaires** (écarts localisés) destinées à évaluer des scanners DAST
> (Qualys WAS, Escape, …). Aucune marque réelle, aucun secret réel.

## Pile technique

- **Go 1.22** (bibliothèque standard uniquement — serveur GraphQL maison,
  équivalent gqlgen ; aucune dépendance externe, build reproductible hors ligne).
- **GraphQL** : endpoint unique `POST /graphql` (corps JSON `{query, variables, operationName}`).
- **Auth** : JWT (`Authorization: Bearer <token>`) + empreinte d'appareil (`X-Device-Id`).
- **Dépôt mémoire** standalone (démarrage immédiat, données fictives, aucune base externe).

## Endpoints

| Méthode | Chemin          | Rôle                                                   |
|---------|-----------------|-------------------------------------------------------|
| POST    | `/graphql`      | Endpoint GraphQL unique (queries + mutations)         |
| POST    | `/admin/reset`  | Réinitialise l'état en mémoire (données fictives)     |
| GET     | `/healthz`      | Sonde de disponibilité                                 |

Toutes les réponses portent l'en-tête `X-Lab-Environment: test-data-fictives`.

## Comptes de laboratoire

Mot de passe commun : `Password1!` — Code OTP/PIN : `123456`.

| Username        | ID interne | Rôle  | Appareil enrôlé |
|-----------------|------------|-------|-----------------|
| `mobile-alice`  | 2001       | USER  | `device-aaa`    |
| `mobile-bruno`  | 2002       | USER  | `device-bbb`    |
| `mobile-admin`  | 2003       | ADMIN | `device-adm`    |

## Exécution locale

```bash
go build -o mobipay .
./mobipay            # écoute sur :8080 (ou $PORT)
# Obtenir un jeton :
curl -s localhost:8080/graphql -H 'Content-Type: application/json' \
  -d '{"query":"mutation($u:String!,$p:String!){login(username:$u,password:$p){token}}","variables":{"u":"mobile-alice","p":"Password1!"}}'
```

## Exemples GraphQL (thème DAST)

```graphql
# BOLA via node(id) — compte d'autrui (APP06-V001)
query { node(id:"QWNjb3VudDoxMDAy"){ ... on Account { iban balance owner{ username } } } }

# Exposition PAN/CVV (APP06-V002)
query { me { cards { id panMasked pan cvv } } }

# Introspection (APP06-V003)
query { __schema { types { name } mutationType { fields { name } } } }

# Brute force d'OTP par alias/batching (APP06-V008)
mutation { a0:verifyOtp(code:"000000"){ok} a1:verifyOtp(code:"123456"){ok} }

# Injection SQL via le filtre (APP06-V010)
query($f:String!){ transactions(filter:$f){ id label } }   # f = category='FOOD' OR '1'='1
```

## Déploiement AWS Elastic Beanstalk (plateforme Go)

- `Buildfile` : `build: go build -o mobipay .` (compilation côté plateforme).
- `Procfile` : `web: ./mobipay` (le binaire écoute sur `$PORT = 5000`).
- `.ebextensions/01-options.config` : **variables d'environnement uniquement**
  (profil standalone, `PORT=5000`). Pas d'option dépréciée (leçon AL2023).
- `.ebignore` : exclut `benchmark/` (oracle de notation), la documentation
  sensible et les artefacts locaux ; **conserve les sources `.go`** (la
  plateforme Go compile depuis les sources).

## Validation (vérité terrain)

```bash
cd benchmark/validation
pip install -r requirements.txt
python3 validate.py --base http://localhost:8080
```

1 test par vulnérabilité (PASS = comportement vulnérable confirmé) et 1 test par
negative control (PASS = attaque bloquée), avec `POST /admin/reset` entre chaque
cas. Rapports `validation-report.json` / `validation-report.html`.
Résultat attendu : **23/23 PASS** (22 vulnérabilités + 13 contrôles négatifs).

> ⚠️ Le dossier `benchmark/` (ground-truth, guide, negative controls) est
> l'**oracle de notation** : il ne doit **jamais** être servi par HTTP ni fourni
> à un scanner.

---

# English version

# MobiPay (APP-06) — Intentionally vulnerable GraphQL mobile API

> **Lab environment — 100 % FICTITIOUS DATA — defensive use.**
> MobiPay is part of the **Banking DAST Benchmark Lab**. The application is
> **secure by default**; it contains only **16 intentional vulnerabilities**
> (localized deviations) meant to evaluate DAST scanners
> (Qualys WAS, Escape, …). No real brand, no real secret.

## Technology stack

- **Go 1.22** (standard library only — in-house GraphQL server,
  gqlgen-equivalent; no external dependency, reproducible offline build).
- **GraphQL**: single endpoint `POST /graphql` (JSON body `{query, variables, operationName}`).
- **Auth**: JWT (`Authorization: Bearer <token>`) + device fingerprint (`X-Device-Id`).
- **In-memory repository** standalone (instant startup, fictitious data, no external database).

## Endpoints

| Method  | Path            | Role                                                   |
|---------|-----------------|-------------------------------------------------------|
| POST    | `/graphql`      | Single GraphQL endpoint (queries + mutations)         |
| POST    | `/admin/reset`  | Resets the in-memory state (fictitious data)          |
| GET     | `/healthz`      | Liveness probe                                         |

All responses carry the header `X-Lab-Environment: test-data-fictives`.

## Lab accounts

Shared password: `Password1!` — OTP/PIN code: `123456`.

| Username        | Internal ID | Role  | Enrolled device |
|-----------------|-------------|-------|-----------------|
| `mobile-alice`  | 2001        | USER  | `device-aaa`    |
| `mobile-bruno`  | 2002        | USER  | `device-bbb`    |
| `mobile-admin`  | 2003        | ADMIN | `device-adm`    |

## Local run

```bash
go build -o mobipay .
./mobipay            # écoute sur :8080 (ou $PORT)
# Obtenir un jeton :
curl -s localhost:8080/graphql -H 'Content-Type: application/json' \
  -d '{"query":"mutation($u:String!,$p:String!){login(username:$u,password:$p){token}}","variables":{"u":"mobile-alice","p":"Password1!"}}'
```

## GraphQL examples (DAST theme)

```graphql
# BOLA via node(id) — compte d'autrui (APP06-V001)
query { node(id:"QWNjb3VudDoxMDAy"){ ... on Account { iban balance owner{ username } } } }

# Exposition PAN/CVV (APP06-V002)
query { me { cards { id panMasked pan cvv } } }

# Introspection (APP06-V003)
query { __schema { types { name } mutationType { fields { name } } } }

# Brute force d'OTP par alias/batching (APP06-V008)
mutation { a0:verifyOtp(code:"000000"){ok} a1:verifyOtp(code:"123456"){ok} }

# Injection SQL via le filtre (APP06-V010)
query($f:String!){ transactions(filter:$f){ id label } }   # f = category='FOOD' OR '1'='1
```

## AWS Elastic Beanstalk deployment (Go platform)

- `Buildfile`: `build: go build -o mobipay .` (platform-side compilation).
- `Procfile`: `web: ./mobipay` (the binary listens on `$PORT = 5000`).
- `.ebextensions/01-options.config`: **environment variables only**
  (standalone profile, `PORT=5000`). No deprecated option (AL2023 lesson).
- `.ebignore`: excludes `benchmark/` (scoring oracle), the sensitive
  documentation and local artifacts; **keeps the `.go` sources** (the
  Go platform compiles from sources).

## Validation (ground truth)

```bash
cd benchmark/validation
pip install -r requirements.txt
python3 validate.py --base http://localhost:8080
```

1 test per vulnerability (PASS = vulnerable behavior confirmed) and 1 test per
negative control (PASS = attack blocked), with `POST /admin/reset` between each
case. Reports `validation-report.json` / `validation-report.html`.
Expected result: **23/23 PASS** (22 vulnerabilities + 13 negative controls).

> ⚠️ The `benchmark/` folder (ground-truth, guide, negative controls) is the
> **scoring oracle**: it must **never** be served over HTTP nor supplied
> to a scanner.
