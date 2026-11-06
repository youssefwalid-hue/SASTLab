# Hélios Banque — Guide utilisateur

> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

> **Environnement de test – données fictives.** Application de laboratoire ; toutes les données sont fictives.

## Présentation

Hélios Banque est une banque en ligne destinée aux particuliers. Les clients y consultent leurs comptes et opérations, gèrent leurs bénéficiaires, téléchargent leurs relevés et émettent des virements sécurisés par un code à usage unique (OTP). Les conseillers disposent d'un accès d'accompagnement, et un administrateur assure la gestion de la plateforme. Toutes les données (comptes, soldes, opérations, identités) sont fictives et réservées aux tests.

## Accès

- URL : `https://helios.lab.exemple` (HTTPS)
- Comptes de démonstration (mot de passe `Password1!`) :
  - `alice` — CLIENT
  - `bruno` — CLIENT
  - `claire` — ADVISOR (conseiller)
  - `admin` — ADMIN (administrateur)
- Authentification : formulaire de connexion classique. Une session serveur est établie via le cookie `JSESSIONID`. Les opérations sensibles (virement) requièrent une seconde vérification par OTP simulé ; le code OTP de laboratoire est `123456`.

## Rôles et droits

| Rôle | Droits principaux |
| --- | --- |
| CLIENT (alice, bruno) | Consulter ses comptes, soldes et opérations ; gérer ses bénéficiaires ; télécharger ses relevés ; émettre des virements avec OTP. |
| ADVISOR (claire) | Accès d'accompagnement aux clients suivis, consultation des opérations dans le cadre du conseil. |
| ADMIN (admin) | Administration de la plateforme et réinitialisation de l'état de démonstration. |

## Parcours principaux

### 1. Connexion
1. Ouvrir `https://helios.lab.exemple`.
2. Saisir l'identifiant (ex. `alice`) et le mot de passe `Password1!`.
3. `POST /login` établit la session (`Set-Cookie: JSESSIONID=...`) et redirige vers le tableau de bord.

### 2. Consulter un compte et ses opérations
1. Depuis le tableau de bord, ouvrir un compte (ex. `ACC-1001`).
2. `GET /accounts/ACC-1001` affiche le détail et le solde.
3. Filtrer les opérations par libellé : `GET /accounts/ACC-1001/transactions?label=courses`.

### 3. Gérer les bénéficiaires
1. Rubrique « Bénéficiaires » → « Ajouter ».
2. Saisir le nom et l'IBAN, puis valider : `POST /beneficiaries` (`name=...&iban=...`).
3. Un nouveau bénéficiaire est soumis à un court délai d'activation avant de pouvoir être utilisé pour un virement.

### 4. Émettre un virement avec OTP
1. Rubrique « Virement » → choisir un bénéficiaire actif et un montant.
2. Créer le virement : `POST /transfers` (`beneficiaryId=BEN-01&amount=50.00`) → virement en état `PENDING`.
3. Saisir le code OTP de laboratoire `123456` pour valider l'étape de sécurité.
4. Confirmer : `POST /transfers/{id}/confirm` (`otp=123456`) → le virement passe à `EXECUTED`.

### 5. Télécharger un relevé
1. Rubrique « Relevés » → sélectionner un mois.
2. `GET /statements/download?file=releve-2026-01.pdf` renvoie le relevé fictif au format PDF.

### 6. Réinitialisation de mot de passe
1. Depuis la page de connexion, « Mot de passe oublié ».
2. `POST /password/reset` (`email=alice@helios.lab.exemple`) envoie un lien de réinitialisation (boîte de laboratoire).

## Notes
- Bandeau « Environnement de test – données fictives » présent sur toutes les pages.
- Pour réinitialiser les données de démonstration : `POST /admin/reset`.

---

# English version

# Hélios Banque — User guide

> **Test environment – fictitious data.** Laboratory application; all data is fictitious.

## Overview

Hélios Banque is an online bank for retail customers. Clients can view their accounts and operations, manage their beneficiaries, download their statements and issue transfers secured by a one-time code (OTP). Advisors have a support access, and an administrator manages the platform. All data (accounts, balances, operations, identities) is fictitious and reserved for testing.

## Access

- URL: `https://helios.lab.exemple` (HTTPS)
- Demo accounts (password `Password1!`):
  - `alice` — CLIENT
  - `bruno` — CLIENT
  - `claire` — ADVISOR
  - `admin` — ADMIN
- Authentication: standard login form. A server session is established via the `JSESSIONID` cookie. Sensitive operations (transfer) require a second verification by simulated OTP; the lab OTP code is `123456`.

## Roles and permissions

| Role | Main permissions |
| --- | --- |
| CLIENT (alice, bruno) | View own accounts, balances and operations; manage own beneficiaries; download own statements; issue transfers with OTP. |
| ADVISOR (claire) | Support access to the clients being followed, viewing operations in an advisory context. |
| ADMIN (admin) | Platform administration and reset of the demo state. |

## Main journeys

### 1. Login
1. Open `https://helios.lab.exemple`.
2. Enter the identifier (e.g. `alice`) and the password `Password1!`.
3. `POST /login` establishes the session (`Set-Cookie: JSESSIONID=...`) and redirects to the dashboard.

### 2. View an account and its operations
1. From the dashboard, open an account (e.g. `ACC-1001`).
2. `GET /accounts/ACC-1001` displays the detail and the balance.
3. Filter operations by label: `GET /accounts/ACC-1001/transactions?label=courses`.

### 3. Manage beneficiaries
1. "Beneficiaries" section → "Add".
2. Enter the name and the IBAN, then validate: `POST /beneficiaries` (`name=...&iban=...`).
3. A new beneficiary is subject to a short activation delay before it can be used for a transfer.

### 4. Issue a transfer with OTP
1. "Transfer" section → choose an active beneficiary and an amount.
2. Create the transfer: `POST /transfers` (`beneficiaryId=BEN-01&amount=50.00`) → transfer in `PENDING` state.
3. Enter the lab OTP code `123456` to validate the security step.
4. Confirm: `POST /transfers/{id}/confirm` (`otp=123456`) → the transfer moves to `EXECUTED`.

### 5. Download a statement
1. "Statements" section → select a month.
2. `GET /statements/download?file=releve-2026-01.pdf` returns the fictitious statement in PDF format.

### 6. Password reset
1. From the login page, "Forgot password".
2. `POST /password/reset` (`email=alice@helios.lab.exemple`) sends a reset link (lab mailbox).

## Notes
- "Environnement de test – données fictives" banner present on all pages.
- To reset the demo data: `POST /admin/reset`.
