> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# Trésoria — Guide utilisateur

> **Environnement de test – données fictives.** Application de laboratoire ; toutes les données sont fictives.

## Présentation
Trésoria est un portail de trésorerie pour entreprises permettant de préparer et d'exécuter des virements de masse selon un circuit de double validation (saisie puis validation). Il s'adresse aux opérateurs de saisie, aux valideurs et aux administrateurs d'entreprise qui gèrent les lots de paiement, le suivi des plafonds et le reporting. L'application se compose d'une interface Angular (SPA) adossée à une API REST ASP.NET Core.

## Accès
- URL : `https://tresoria.lab.exemple` (HTTPS)
- Comptes de démonstration (mot de passe `Password1!`) :
  - `saisie1` — rôle saisie, entreprise A
  - `valideur1` — rôle validation, entreprise A
  - `admin-ent` — rôle administrateur, entreprise A
  - `saisie2` — rôle saisie, entreprise B
- Authentification : connexion par formulaire renvoyant un couple de jetons JWT (access token court + refresh token). L'access token est transmis en en-tête `Authorization: Bearer <token>` sur chaque appel API ; le refresh token permet d'obtenir un nouvel access token via `POST /api/token/refresh`. Le code OTP de laboratoire est `123456` lorsque demandé.

## Rôles et droits

| Rôle | Droits principaux |
|------|-------------------|
| saisie | Importer et créer des lots de virements, éditer un lot en brouillon, soumettre un lot pour approbation, consulter le reporting de son entreprise. Ne peut pas approuver ni valider. |
| validation (valideur) | Consulter les lots en attente, approuver puis valider les lots (circuit des quatre yeux), consulter le reporting. |
| administrateur d'entreprise (admin-ent) | Gérer les utilisateurs et les plafonds de l'entreprise, consulter tous les lots de l'entreprise, accéder aux exports. |

Chaque utilisateur est rattaché à une entreprise (tenant) ; les données sont cloisonnées par entreprise.

## Parcours principaux

### 1. Connexion et obtention des jetons
1. Accéder à `https://tresoria.lab.exemple` et saisir identifiant et mot de passe (`Password1!`).
2. L'appel `POST /api/login` renvoie un access token et un refresh token.
3. Exemple de réponse fictive :
   ```json
   { "access_token": "<jwt>", "refresh_token": "<rt>", "expires_in": 900, "role": "saisie", "company": "A" }
   ```
4. Les appels suivants portent l'en-tête `Authorization: Bearer <access_token>`.

### 2. Import d'un lot de virements de masse
1. Dans « Lots », cliquer sur « Importer » et déposer un fichier, ou créer un lot manuellement.
2. Appel : `POST /api/batches`
   ```json
   {
     "label": "Loyers octobre",
     "transfers": [
       { "iban": "FR7630000000000000000000001", "amount": 1500.00, "label": "Loyer siège" },
       { "iban": "FR7630000000000000000000002", "amount": 820.00, "label": "Loyer agence" }
     ]
   }
   ```
3. Réponse fictive : `201 { "id": 9001, "status": "DRAFT", "createdBy": "saisie1", "company": "A" }`

### 3. Soumission puis workflow de double validation
1. L'opérateur de saisie soumet le lot : `POST /api/batches/9001/submit` → statut `PENDING_APPROVAL`.
2. Un valideur (distinct du créateur) approuve : `POST /api/batches/9001/approve` → statut `APPROVED`.
3. Le même valideur valide l'exécution : `POST /api/batches/9001/validate` → statut `VALIDATED`.
4. Le circuit respecte la règle des quatre yeux : le créateur du lot ne peut pas l'approuver lui-même.

### 4. Création d'un virement unitaire
1. Dans « Virements », saisir le compte émetteur, le bénéficiaire (IBAN), le montant et le libellé.
2. Appel : `POST /api/transfers`
   ```json
   { "accountId": "A-001", "amount": 250.00, "iban": "FR7630000000000000000000003", "label": "Fournisseur" }
   ```
3. Le montant est contrôlé (positif, dans le plafond quotidien de l'entreprise) avant acceptation.

### 5. Reporting et export
1. Consulter le reporting trié : `GET /api/reports?sort=amount` renvoie les mouvements agrégés de l'entreprise.
2. Exporter un lot : `GET /api/batches/9001/export` fournit les données fonctionnelles du lot (bénéficiaires, montants, libellés) au format exportable.

### 6. Rafraîchissement de session et déconnexion
1. À l'expiration de l'access token : `POST /api/token/refresh` avec `{ "refresh_token": "<rt>" }` → nouvel access token.
2. Déconnexion : `POST /api/logout` invalide la session et le refresh token.

## Notes
- Bandeau « Environnement de test – données fictives » présent sur toutes les pages.
- Pour réinitialiser les données de démonstration : `POST /admin/reset`.

---

# English version

# Trésoria — User guide

> **Test environment – fictitious data.** Lab application; all data is fictitious.

## Overview
Trésoria is a corporate treasury portal for preparing and executing mass transfers through a dual-validation circuit (entry then validation). It is aimed at entry operators, validators and company administrators who manage payment batches, cap tracking and reporting. The application consists of an Angular interface (SPA) backed by an ASP.NET Core REST API.

## Access
- URL: `https://tresoria.lab.exemple` (HTTPS)
- Demo accounts (password `Password1!`):
  - `saisie1` — saisie role, company A
  - `valideur1` — validation role, company A
  - `admin-ent` — administrator role, company A
  - `saisie2` — saisie role, company B
- Authentication: form login returning a pair of JWT tokens (short access token + refresh token). The access token is sent in the `Authorization: Bearer <token>` header on every API call; the refresh token allows obtaining a new access token via `POST /api/token/refresh`. The lab OTP code is `123456` when requested.

## Roles and permissions

| Role | Main permissions |
|------|------------------|
| saisie | Import and create transfer batches, edit a draft batch, submit a batch for approval, view the reporting of their company. Cannot approve or validate. |
| validation (validator) | View pending batches, approve then validate batches (four-eyes circuit), view reporting. |
| company administrator (admin-ent) | Manage the company's users and caps, view all the company's batches, access exports. |

Each user is attached to a company (tenant); data is isolated per company.

## Main journeys

### 1. Login and obtaining tokens
1. Go to `https://tresoria.lab.exemple` and enter username and password (`Password1!`).
2. The `POST /api/login` call returns an access token and a refresh token.
3. Example fictitious response:
   ```json
   { "access_token": "<jwt>", "refresh_token": "<rt>", "expires_in": 900, "role": "saisie", "company": "A" }
   ```
4. Subsequent calls carry the `Authorization: Bearer <access_token>` header.

### 2. Importing a mass-transfer batch
1. In "Batches", click "Import" and drop a file, or create a batch manually.
2. Call: `POST /api/batches`
   ```json
   {
     "label": "Loyers octobre",
     "transfers": [
       { "iban": "FR7630000000000000000000001", "amount": 1500.00, "label": "Loyer siège" },
       { "iban": "FR7630000000000000000000002", "amount": 820.00, "label": "Loyer agence" }
     ]
   }
   ```
3. Fictitious response: `201 { "id": 9001, "status": "DRAFT", "createdBy": "saisie1", "company": "A" }`

### 3. Submission then dual-validation workflow
1. The entry operator submits the batch: `POST /api/batches/9001/submit` → status `PENDING_APPROVAL`.
2. A validator (distinct from the creator) approves: `POST /api/batches/9001/approve` → status `APPROVED`.
3. The same validator validates execution: `POST /api/batches/9001/validate` → status `VALIDATED`.
4. The circuit enforces the four-eyes rule: the batch creator cannot approve it themselves.

### 4. Creating a single transfer
1. In "Transfers", enter the originating account, the beneficiary (IBAN), the amount and the label.
2. Call: `POST /api/transfers`
   ```json
   { "accountId": "A-001", "amount": 250.00, "iban": "FR7630000000000000000000003", "label": "Fournisseur" }
   ```
3. The amount is checked (positive, within the company's daily cap) before acceptance.

### 5. Reporting and export
1. View sorted reporting: `GET /api/reports?sort=amount` returns the company's aggregated movements.
2. Export a batch: `GET /api/batches/9001/export` provides the batch's functional data (beneficiaries, amounts, labels) in an exportable format.

### 6. Session refresh and logout
1. When the access token expires: `POST /api/token/refresh` with `{ "refresh_token": "<rt>" }` → new access token.
2. Logout: `POST /api/logout` invalidates the session and the refresh token.

## Notes
- "Test environment – fictitious data" banner present on all pages.
- To reset the demo data: `POST /admin/reset`.
