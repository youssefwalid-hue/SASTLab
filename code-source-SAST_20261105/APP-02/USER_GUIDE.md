> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# Agréga Open — Guide utilisateur

> **Environnement de test – données fictives.** Application de laboratoire ; toutes les données sont fictives.

## Présentation

Agréga Open est une API d'agrégation de comptes bancaires (Open Banking) exposée aux prestataires tiers (TPP). Elle permet, après consentement d'un utilisateur de banque, de consulter les comptes agrégés, les paiements et de générer des exports. C'est une API pure REST décrite en OpenAPI 3 ; elle n'a pas d'interface graphique propre et s'adresse à des applications clientes de TPP.

## Accès

- URL : `https://agrega.lab.exemple` (HTTPS)
- Comptes de démonstration (mot de passe `Password1!`) :
  - `tpp-a` — client OAuth (prestataire tiers A), consentement sur l'utilisateur `alice`
  - `tpp-b` — client OAuth (prestataire tiers B), consentement sur l'utilisateur `bruno`
  - `alice`, `bruno` — utilisateurs sous-jacents (titulaires des comptes agrégés)
  - `admin` — administration de la plateforme (consentements, inventaire)
- Authentification : OAuth2 / OIDC via l'IdP du laboratoire. Les TPP s'authentifient en `client_credentials` (ou via le parcours d'autorisation utilisateur pour recueillir un consentement). Les appels portent un jeton JWT en `Authorization: Bearer <token>`, assorti de `scope` et d'une `aud`. Une clé d'API TPP (`X-API-Key`) est acceptée sur certains endpoints de lecture. Code OTP du laboratoire : `123456`.

## Rôles et droits

| Acteur | Peut faire |
| --- | --- |
| `tpp-a` / `tpp-b` (client OAuth) | Obtenir un token, créer/consulter ses consentements, lire les comptes et paiements couverts par son consentement, générer et télécharger ses exports |
| `alice` / `bruno` (utilisateur) | Donner ou révoquer son consentement à un TPP pour le partage de ses comptes |
| `admin` | Gérer l'ensemble des consentements, consulter l'inventaire d'API, administrer la plateforme |

Scopes OAuth2 types : `accounts:read`, `payments:read`, `payments:write`, `consents:write`.

## Parcours principaux

### 1. Obtenir un jeton d'accès (client_credentials)

```
POST /oauth/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=tpp-a&client_secret=Password1!&scope=accounts:read
```

Réponse :

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6ImxhYi0xIn0...sig",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "accounts:read"
}
```

### 2. Recueillir un consentement utilisateur (parcours d'autorisation OAuth2/OIDC)

1. Le TPP redirige l'utilisateur vers l'IdP :
   `GET /authorize?response_type=code&client_id=tpp-a&redirect_uri=https://tpp-a.exemple/cb&scope=accounts:read&state=xyz`
2. `alice` s'authentifie (mot de passe `Password1!`, OTP `123456`) et valide le partage de ses comptes.
3. L'IdP redirige vers le TPP avec un `code`, échangé contre un token lié au consentement :
   `POST /oauth/token (grant_type=authorization_code&code=...&redirect_uri=...&client_id=tpp-a&client_secret=Password1!)`

### 3. Créer un consentement et un webhook de notification

```
POST /v1/consents
Authorization: Bearer <token tpp-a>
Content-Type: application/json

{
  "user": "alice",
  "permissions": ["accounts:read"],
  "callbackUrl": "https://tpp-a.exemple/webhook"
}
```

Réponse :

```json
{ "consentId": "CONS-3001", "status": "AUTHORISED", "grantedScopes": ["accounts:read"] }
```

### 4. Lister les comptes et consulter le détail

```
GET /v1/accounts
Authorization: Bearer <token tpp-a>
```

```json
{
  "accounts": [
    { "accountId": "ACC-FR-1001", "holder": "alice", "type": "COURANT", "currency": "EUR" }
  ]
}
```

```
GET /v1/accounts/ACC-FR-1001
Authorization: Bearer <token tpp-a>
```

```json
{
  "accountId": "ACC-FR-1001",
  "holder": "alice",
  "iban": "FR76********0123",
  "balance": { "amount": 1842.50, "currency": "EUR" }
}
```

### 5. Consulter les paiements (scope dédié)

```
GET /v1/payments
Authorization: Bearer <token tpp-a avec scope payments:read>
```

```json
{
  "payments": [
    { "paymentId": "PAY-5001", "amount": 59.90, "currency": "EUR", "status": "SETTLED" }
  ]
}
```

### 6. Générer puis télécharger un export agrégé

1. Demande d'export :
   `POST /v1/exports (Authorization: Bearer <token tpp-a>) {"accountId":"ACC-FR-1001","format":"json"}`
   → `{ "exportId": "EXP-1007", "status": "READY" }`
2. Téléchargement :
   `GET /v1/exports/EXP-1007 (Authorization: Bearer <token tpp-a>)` → fichier d'export des opérations agrégées.

## Notes

- Bandeau « Environnement de test – données fictives » présent sur toutes les pages (console de démonstration et réponses de diagnostic).
- Pour réinitialiser les données de démonstration : `POST /admin/reset`.

---

# English version

# Agréga Open — User guide

> **Test environment – fictitious data.** Laboratory application; all data is fictitious.

## Introduction

Agréga Open is a bank account aggregation API (Open Banking) exposed to third-party providers (TPP). After a bank user's consent, it allows consulting aggregated accounts and payments and generating exports. It is a pure REST API described in OpenAPI 3; it has no graphical interface of its own and is aimed at TPP client applications.

## Access

- URL: `https://agrega.lab.exemple` (HTTPS)
- Demo accounts (password `Password1!`):
  - `tpp-a` — OAuth client (third-party provider A), consent on user `alice`
  - `tpp-b` — OAuth client (third-party provider B), consent on user `bruno`
  - `alice`, `bruno` — underlying users (holders of the aggregated accounts)
  - `admin` — platform administration (consents, inventory)
- Authentication: OAuth2 / OIDC via the lab IdP. TPPs authenticate with `client_credentials` (or via the user authorization flow to collect a consent). Calls carry a JWT token in `Authorization: Bearer <token>`, together with a `scope` and an `aud`. A TPP API key (`X-API-Key`) is accepted on certain read endpoints. Lab OTP code: `123456`.

## Roles and permissions

| Actor | Can do |
| --- | --- |
| `tpp-a` / `tpp-b` (OAuth client) | Obtain a token, create/consult its consents, read the accounts and payments covered by its consent, generate and download its exports |
| `alice` / `bruno` (user) | Grant or revoke consent to a TPP for sharing their accounts |
| `admin` | Manage all consents, consult the API inventory, administer the platform |

Typical OAuth2 scopes: `accounts:read`, `payments:read`, `payments:write`, `consents:write`.

## Main journeys

### 1. Obtain an access token (client_credentials)

```
POST /oauth/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=tpp-a&client_secret=Password1!&scope=accounts:read
```

Response:

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6ImxhYi0xIn0...sig",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "accounts:read"
}
```

### 2. Collect a user consent (OAuth2/OIDC authorization flow)

1. The TPP redirects the user to the IdP:
   `GET /authorize?response_type=code&client_id=tpp-a&redirect_uri=https://tpp-a.exemple/cb&scope=accounts:read&state=xyz`
2. `alice` authenticates (password `Password1!`, OTP `123456`) and approves the sharing of her accounts.
3. The IdP redirects to the TPP with a `code`, exchanged for a token bound to the consent:
   `POST /oauth/token (grant_type=authorization_code&code=...&redirect_uri=...&client_id=tpp-a&client_secret=Password1!)`

### 3. Create a consent and a notification webhook

```
POST /v1/consents
Authorization: Bearer <token tpp-a>
Content-Type: application/json

{
  "user": "alice",
  "permissions": ["accounts:read"],
  "callbackUrl": "https://tpp-a.exemple/webhook"
}
```

Response:

```json
{ "consentId": "CONS-3001", "status": "AUTHORISED", "grantedScopes": ["accounts:read"] }
```

### 4. List accounts and consult the detail

```
GET /v1/accounts
Authorization: Bearer <token tpp-a>
```

```json
{
  "accounts": [
    { "accountId": "ACC-FR-1001", "holder": "alice", "type": "COURANT", "currency": "EUR" }
  ]
}
```

```
GET /v1/accounts/ACC-FR-1001
Authorization: Bearer <token tpp-a>
```

```json
{
  "accountId": "ACC-FR-1001",
  "holder": "alice",
  "iban": "FR76********0123",
  "balance": { "amount": 1842.50, "currency": "EUR" }
}
```

### 5. Consult payments (dedicated scope)

```
GET /v1/payments
Authorization: Bearer <token tpp-a avec scope payments:read>
```

```json
{
  "payments": [
    { "paymentId": "PAY-5001", "amount": 59.90, "currency": "EUR", "status": "SETTLED" }
  ]
}
```

### 6. Generate then download an aggregated export

1. Export request:
   `POST /v1/exports (Authorization: Bearer <token tpp-a>) {"accountId":"ACC-FR-1001","format":"json"}`
   → `{ "exportId": "EXP-1007", "status": "READY" }`
2. Download:
   `GET /v1/exports/EXP-1007 (Authorization: Bearer <token tpp-a>)` → export file of the aggregated operations.

## Notes

- "Test environment – fictitious data" banner present on all pages (demo console and diagnostic responses).
- To reset the demo data: `POST /admin/reset`.
