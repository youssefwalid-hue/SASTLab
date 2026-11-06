> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# MobiPay — Guide utilisateur

> **Environnement de test – données fictives.** Application de laboratoire ; toutes les données sont fictives.

## Présentation
MobiPay est l'API mobile fictive d'une banque de laboratoire. Elle alimente une application mobile de banque au quotidien : consultation des comptes et des cartes, historique des transactions, notifications et gestion du profil. L'API est exposée en GraphQL pur via un endpoint unique ; elle s'adresse aux clients particuliers de la banque et, pour certaines opérations, à un rôle d'administration interne.

## Accès
- URL : `https://mobipay.lab.exemple` (HTTPS)
- Endpoint unique : `POST /graphql` (corps JSON `{"query": "...", "variables": {...}}`).
- Comptes de démonstration (mot de passe `Password1!`) :
  - `mobile-alice` — client (rôle USER)
  - `mobile-bruno` — client (rôle USER)
  - `mobile-admin` — administration mobile (rôle ADMIN)
- Authentification : par JWT. On obtient un token via la mutation `login`, puis on l'envoie dans l'en-tête `Authorization: Bearer <token>` à chaque requête. Une empreinte d'appareil (`X-Device-Id`) accompagne les requêtes de l'application mobile. Code OTP de laboratoire pour les opérations sensibles : `123456`.

## Rôles et droits

| Rôle | Droits |
| --- | --- |
| USER (mobile-alice, mobile-bruno) | Consulter ses propres comptes, cartes et transactions ; lire ses notifications ; mettre à jour son profil ; déclencher et vérifier un OTP pour les opérations sensibles. |
| ADMIN (mobile-admin) | Opérations d'administration mobile (ex. blocage global des cartes, gel de compte) en plus des droits USER. |

## Parcours principaux

### 1. Connexion et obtention du token
Requête :
```graphql
mutation Login($u: String!, $p: String!) {
  login(username: $u, password: $p) {
    token
    user { id username role }
  }
}
```
Variables : `{"u": "mobile-alice", "p": "Password1!"}`

Réponse (fictive) :
```json
{ "data": { "login": { "token": "eyJhbGciOi...", "user": { "id": "1", "username": "mobile-alice", "role": "USER" } } } }
```
Le token est ensuite transmis dans `Authorization: Bearer <token>`.

### 2. Consultation de ses comptes et de leur solde
```graphql
query {
  me {
    id
    accounts { id iban balance currency }
  }
}
```
Réponse (fictive) : liste des comptes de l'utilisateur courant avec IBAN fictif et solde.

### 3. Affichage des cartes (données masquées)
```graphql
query {
  me {
    cards { id panMasked expiry status }
  }
}
```
Réponse (fictive) : `panMasked = "4000 **** **** 1234"`, sans exposer le numéro complet ni le cryptogramme.

### 4. Historique des transactions filtré
```graphql
query Transactions($cat: TransactionCategory, $from: Date, $to: Date) {
  transactions(category: $cat, from: $from, to: $to) {
    id amount label date category
  }
}
```
Variables : `{"cat": "FOOD", "from": "2026-01-01", "to": "2026-03-31"}`

### 5. Mise à jour du profil
```graphql
mutation UpdateProfile($in: UpdateProfileInput!) {
  updateProfile(input: $in) { id displayName email }
}
```
Variables : `{"in": {"displayName": "Alice D.", "email": "alice@exemple.test"}}`
Seuls les champs autorisés (nom affiché, e-mail) sont modifiables par l'utilisateur.

### 6. Opération sensible avec OTP
1. Déclencher l'envoi de l'OTP :
```graphql
mutation { requestOtp(action: "TRANSFER") { challengeId } }
```
2. Vérifier le code reçu (code de laboratoire `123456`) :
```graphql
mutation VerifyOtp($c: String!) {
  verifyOtp(code: $c) { ok }
}
```
Variables : `{"c": "123456"}`

## Notes
- Bandeau « Environnement de test – données fictives » présent sur toutes les pages (et dans l'écran d'accueil de l'application mobile).
- Pour réinitialiser les données de démonstration : `POST /admin/reset`.

---

# English version

# MobiPay — User guide

> **Test environment – fictitious data.** Lab application; all data is fictitious.

## Overview
MobiPay is the fictitious mobile API of a lab bank. It powers an everyday mobile banking application: viewing accounts and cards, transaction history, notifications and profile management. The API is exposed as pure GraphQL via a single endpoint; it serves the bank's retail customers and, for some operations, an internal administration role.

## Access
- URL: `https://mobipay.lab.exemple` (HTTPS)
- Single endpoint: `POST /graphql` (JSON body `{"query": "...", "variables": {...}}`).
- Demo accounts (password `Password1!`):
  - `mobile-alice` — customer (USER role)
  - `mobile-bruno` — customer (USER role)
  - `mobile-admin` — mobile administration (ADMIN role)
- Authentication: via JWT. A token is obtained through the `login` mutation, then sent in the `Authorization: Bearer <token>` header on every request. A device fingerprint (`X-Device-Id`) accompanies the mobile application's requests. Lab OTP code for sensitive operations: `123456`.

## Roles and rights

| Role | Rights |
| --- | --- |
| USER (mobile-alice, mobile-bruno) | View their own accounts, cards and transactions; read their notifications; update their profile; trigger and verify an OTP for sensitive operations. |
| ADMIN (mobile-admin) | Mobile administration operations (e.g. global card blocking, account freeze) in addition to USER rights. |

## Main journeys

### 1. Login and token retrieval
Request:
```graphql
mutation Login($u: String!, $p: String!) {
  login(username: $u, password: $p) {
    token
    user { id username role }
  }
}
```
Variables: `{"u": "mobile-alice", "p": "Password1!"}`

Response (fictitious):
```json
{ "data": { "login": { "token": "eyJhbGciOi...", "user": { "id": "1", "username": "mobile-alice", "role": "USER" } } } }
```
The token is then sent in `Authorization: Bearer <token>`.

### 2. Viewing one's accounts and balances
```graphql
query {
  me {
    id
    accounts { id iban balance currency }
  }
}
```
Response (fictitious): list of the current user's accounts with fictitious IBAN and balance.

### 3. Displaying cards (masked data)
```graphql
query {
  me {
    cards { id panMasked expiry status }
  }
}
```
Response (fictitious): `panMasked = "4000 **** **** 1234"`, without exposing the full number nor the security code.

### 4. Filtered transaction history
```graphql
query Transactions($cat: TransactionCategory, $from: Date, $to: Date) {
  transactions(category: $cat, from: $from, to: $to) {
    id amount label date category
  }
}
```
Variables: `{"cat": "FOOD", "from": "2026-01-01", "to": "2026-03-31"}`

### 5. Profile update
```graphql
mutation UpdateProfile($in: UpdateProfileInput!) {
  updateProfile(input: $in) { id displayName email }
}
```
Variables: `{"in": {"displayName": "Alice D.", "email": "alice@exemple.test"}}`
Only the allowed fields (display name, e-mail) can be modified by the user.

### 6. Sensitive operation with OTP
1. Trigger the OTP send:
```graphql
mutation { requestOtp(action: "TRANSFER") { challengeId } }
```
2. Verify the received code (lab code `123456`):
```graphql
mutation VerifyOtp($c: String!) {
  verifyOtp(code: $c) { ok }
}
```
Variables: `{"c": "123456"}`

## Notes
- "Test environment – fictitious data" banner present on every page (and on the mobile application's home screen).
- To reset the demo data: `POST /admin/reset`.
