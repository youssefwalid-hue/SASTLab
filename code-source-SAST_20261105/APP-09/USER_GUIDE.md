> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# Novabourse — Guide utilisateur

> **Environnement de test – données fictives.** Application de laboratoire ; toutes les données sont fictives.

## Présentation

Novabourse est un portail titres fictif permettant à des investisseurs particuliers de consulter leur portefeuille, de rechercher des instruments financiers, de passer et de suivre des ordres de bourse, puis de consulter leurs avis d'opéré. Les utilisateurs sont des investisseurs (rôle INVESTOR) et des agents de back-office (rôle BACKOFFICE) chargés de la supervision. L'application se compose d'une interface React (SPA) adossée à une API REST FastAPI documentée en OpenAPI.

## Accès

- URL : `https://novabourse.lab.exemple` (HTTPS)
- Comptes de démonstration (mot de passe `Password1!`) :
  - `invest-a` — rôle INVESTOR (portefeuille PF-1001, compte titres ACC-2001)
  - `invest-b` — rôle INVESTOR (portefeuille PF-1002, compte titres ACC-2002)
  - `backoffice` — rôle BACKOFFICE (supervision des ordres)
- Authentification : OIDC avec PKCE via l'IdP du laboratoire. La SPA génère un `code_verifier`, calcule le `code_challenge` (méthode S256), redirige l'utilisateur vers `/authorize`, puis échange le `code` d'autorisation contre un jeton d'accès JWT sur `/token`. Les appels API portent l'en-tête `Authorization: Bearer <jwt>`. Le code OTP de laboratoire, si demandé par l'IdP, est `123456`.

## Rôles et droits

| Rôle | Droits |
| --- | --- |
| INVESTOR | Consulter son portefeuille, rechercher des instruments, passer/annuler ses ordres (états autorisés), consulter ses avis d'opéré |
| BACKOFFICE | Superviser l'ensemble des ordres via les endpoints d'administration, consulter les comptes clients |

## Parcours principaux

### 1. Connexion OIDC PKCE
1. L'utilisateur clique sur « Se connecter ». La SPA génère `code_verifier` et `code_challenge` (S256).
2. Redirection : `GET /authorize?response_type=code&client_id=novabourse-spa&redirect_uri=https://novabourse.lab.exemple/callback&code_challenge=<cc>&code_challenge_method=S256&state=<s>`.
3. L'utilisateur s'authentifie auprès de l'IdP (identifiant + `Password1!`, OTP `123456` si demandé).
4. Retour : `GET /callback?code=<code>&state=<s>`. La SPA vérifie le `state`, puis échange le code :
   `POST /token` avec `{"grant_type":"authorization_code","code":"<code>","redirect_uri":"https://novabourse.lab.exemple/callback","client_id":"novabourse-spa","code_verifier":"<cv>"}`.
5. Réponse (fictive) : `{"access_token":"<jwt>","token_type":"Bearer","expires_in":3600,"id_token":"<jwt>"}`.

### 2. Consulter son portefeuille
1. `GET /api/portfolios` (en-tête `Authorization: Bearer <jwt>`) renvoie la liste des portefeuilles de l'utilisateur.
2. `GET /api/portfolios/PF-1001` renvoie le détail :
   ```json
   {
     "id": "PF-1001",
     "ownerId": "invest-a",
     "cash": 12500.00,
     "positions": [
       {"instrument": "FR-NOVA", "quantity": 120, "avgPrice": 41.80}
     ]
   }
   ```

### 3. Rechercher un instrument
1. `GET /api/instruments?q=NOVA` renvoie les instruments correspondants :
   ```json
   [{"code": "FR-NOVA", "name": "Novabourse Industries", "lastPrice": 42.50}]
   ```

### 4. Passer un ordre
1. `POST /api/orders` avec le corps :
   ```json
   {"accountId": "ACC-2001", "instrument": "FR-NOVA", "side": "BUY", "quantity": 10, "price": 42.50}
   ```
2. Réponse `201 Created` :
   ```json
   {"id": "ORD-3001", "accountId": "ACC-2001", "status": "PENDING", "createdBy": "invest-a"}
   ```

### 5. Suivre et annuler un ordre
1. `GET /api/orders/ORD-3001` renvoie l'état courant (`PENDING`, `PARTIAL`, `EXECUTED`, `CANCELLED`).
2. `POST /api/orders/ORD-3001/cancel` annule un ordre encore annulable (`PENDING`/`PARTIAL`) et renvoie `200 OK`.

### 6. Consulter l'avis d'opéré
1. Une fois l'ordre `EXECUTED`, `GET /api/orders/ORD-3001` expose l'avis d'opéré (quantité exécutée, prix moyen, horodatage).

## Notes

- Bandeau « Environnement de test – données fictives » présent sur toutes les pages.
- Pour réinitialiser les données de démonstration : `POST /admin/reset`.

---

# English version

# Novabourse — User guide

> **Test environment – fictitious data.** Lab application; all data is fictitious.

## Introduction

Novabourse is a fictitious securities portal that lets retail investors view their portfolio, search for financial instruments, place and track stock-market orders, then view their trade confirmations. Users are investors (INVESTOR role) and back-office agents (BACKOFFICE role) in charge of supervision. The application consists of a React (SPA) interface backed by a FastAPI REST API documented with OpenAPI.

## Access

- URL: `https://novabourse.lab.exemple` (HTTPS)
- Demo accounts (password `Password1!`):
  - `invest-a` — INVESTOR role (portfolio PF-1001, securities account ACC-2001)
  - `invest-b` — INVESTOR role (portfolio PF-1002, securities account ACC-2002)
  - `backoffice` — BACKOFFICE role (order supervision)
- Authentication: OIDC with PKCE via the lab IdP. The SPA generates a `code_verifier`, computes the `code_challenge` (S256 method), redirects the user to `/authorize`, then exchanges the authorization `code` for a JWT access token at `/token`. API calls carry the `Authorization: Bearer <jwt>` header. The lab OTP code, if requested by the IdP, is `123456`.

## Roles and permissions

| Role | Permissions |
| --- | --- |
| INVESTOR | View own portfolio, search instruments, place/cancel own orders (allowed states), view own trade confirmations |
| BACKOFFICE | Supervise all orders via the administration endpoints, view client accounts |

## Main journeys

### 1. OIDC PKCE sign-in
1. The user clicks "Sign in". The SPA generates `code_verifier` and `code_challenge` (S256).
2. Redirection: `GET /authorize?response_type=code&client_id=novabourse-spa&redirect_uri=https://novabourse.lab.exemple/callback&code_challenge=<cc>&code_challenge_method=S256&state=<s>`.
3. The user authenticates against the IdP (username + `Password1!`, OTP `123456` if requested).
4. Return: `GET /callback?code=<code>&state=<s>`. The SPA verifies the `state`, then exchanges the code:
   `POST /token` with `{"grant_type":"authorization_code","code":"<code>","redirect_uri":"https://novabourse.lab.exemple/callback","client_id":"novabourse-spa","code_verifier":"<cv>"}`.
5. Response (fictitious): `{"access_token":"<jwt>","token_type":"Bearer","expires_in":3600,"id_token":"<jwt>"}`.

### 2. View your portfolio
1. `GET /api/portfolios` (header `Authorization: Bearer <jwt>`) returns the list of the user's portfolios.
2. `GET /api/portfolios/PF-1001` returns the detail:
   ```json
   {
     "id": "PF-1001",
     "ownerId": "invest-a",
     "cash": 12500.00,
     "positions": [
       {"instrument": "FR-NOVA", "quantity": 120, "avgPrice": 41.80}
     ]
   }
   ```

### 3. Search for an instrument
1. `GET /api/instruments?q=NOVA` returns the matching instruments:
   ```json
   [{"code": "FR-NOVA", "name": "Novabourse Industries", "lastPrice": 42.50}]
   ```

### 4. Place an order
1. `POST /api/orders` with the body:
   ```json
   {"accountId": "ACC-2001", "instrument": "FR-NOVA", "side": "BUY", "quantity": 10, "price": 42.50}
   ```
2. `201 Created` response:
   ```json
   {"id": "ORD-3001", "accountId": "ACC-2001", "status": "PENDING", "createdBy": "invest-a"}
   ```

### 5. Track and cancel an order
1. `GET /api/orders/ORD-3001` returns the current state (`PENDING`, `PARTIAL`, `EXECUTED`, `CANCELLED`).
2. `POST /api/orders/ORD-3001/cancel` cancels an order that is still cancellable (`PENDING`/`PARTIAL`) and returns `200 OK`.

### 6. View the trade confirmation
1. Once the order is `EXECUTED`, `GET /api/orders/ORD-3001` exposes the trade confirmation (executed quantity, average price, timestamp).

## Notes

- "Test environment – fictitious data" banner present on all pages.
- To reset the demo data: `POST /admin/reset`.
