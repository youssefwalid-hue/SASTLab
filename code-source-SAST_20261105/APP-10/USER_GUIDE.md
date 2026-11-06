> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# OnboardR — Guide utilisateur

> **Environnement de test – données fictives.** Application de laboratoire ; toutes les données sont fictives.

## Présentation
OnboardR est la plateforme d'entrée en relation (KYC) fictive du laboratoire : elle permet à un prospect d'ouvrir un compte en déposant ses pièces d'identité, puis à un agent de conformité de vérifier le dossier et de décider de son acceptation. L'application est composée de microservices Java (Spring Boot) et Node.js (NestJS) orchestrés par Docker, exposés au travers d'un reverse proxy Nginx dédié qui sert de passerelle unique.

## Accès
- URL : `https://onboardr.lab.exemple` (HTTPS)
- Comptes de démonstration (mot de passe `Password1!`) :
  - `prospect1` — PROSPECT (titulaire d'un dossier d'onboarding)
  - `prospect2` — PROSPECT (seconde identité fictive)
  - `conformite1` — CONFORMITE (vérification et décision)
- Authentification : formulaire de connexion via la passerelle, complété par un code à usage unique (OTP) simulé à l'étape de vérification du dossier (code de laboratoire : `123456`). Les appels inter-services sont protégés par une clé de service interne.

## Rôles et droits

| Rôle | Droits |
| --- | --- |
| PROSPECT | Créer et compléter son propre dossier d'onboarding, déposer ses pièces, suivre l'avancement, consulter uniquement son dossier. |
| CONFORMITE | Consulter l'ensemble des dossiers, vérifier les pièces, prendre une décision d'acceptation ou de refus via la zone d'administration. |

## Parcours principaux

### 1. Connexion
1. Ouvrir `https://onboardr.lab.exemple`.
2. Saisir l'identifiant (ex. `prospect1`) et le mot de passe `Password1!`.
3. La passerelle ouvre la session et redirige vers le tableau de bord du prospect.

### 2. Ouverture d'un dossier d'onboarding
1. Depuis le tableau de bord, démarrer un nouveau parcours d'ouverture de compte.
2. L'application crée le dossier : `POST /api/onboarding` → renvoie l'identifiant du dossier (ex. `1001`).
3. Renseigner les informations d'identité fictives demandées, puis enregistrer (`PUT /api/onboarding/1001`).

### 3. Dépôt de pièces justificatives
1. Dans le dossier, choisir « Ajouter une pièce » (justificatif d'identité, de domicile).
2. Téléverser le fichier : `POST /api/onboarding/1001/documents` (multipart, image ou PDF).
3. La pièce apparaît dans la liste des documents du dossier.

### 4. Vérification OTP
1. À l'étape de vérification, saisir le code à usage unique de laboratoire `123456`.
2. L'application confirme l'étape : `POST /api/onboarding/1001/verify`.

### 5. Décision de conformité
1. Se connecter en tant que `conformite1`.
2. Consulter le dossier à traiter puis trancher via la zone d'administration : `POST /api/admin/onboarding/1001/decision` avec `{"decision":"APPROVED"}` ou `{"decision":"REJECTED"}`.
3. Le prospect voit le statut mis à jour dans le suivi de son dossier.

### 6. Exemple d'appel API (REST via la passerelle)
- Consulter son dossier :

  Requête :
  ```
  GET /api/onboarding/1001 HTTP/1.1
  Host: onboardr.lab.exemple
  Cookie: session=<jeton prospect1>
  ```
  Réponse (fictive) :
  ```json
  {
    "id": 1001,
    "subject": "Prospect Un",
    "status": "PENDING",
    "documents": [{ "id": 5001, "type": "ID", "name": "piece.pdf" }]
  }
  ```

## Notes
- Bandeau « Environnement de test – données fictives » présent sur toutes les pages.
- Pour réinitialiser les données de démonstration : `POST /admin/reset`.

---

# English version

# OnboardR — User guide

> **Test environment – fictitious data.** Laboratory application; all data is fictitious.

## Overview
OnboardR is the laboratory's fictitious client onboarding (KYC) platform: it lets a prospect open an account by uploading their ID documents, then a compliance officer verify the dossier and decide whether to accept it. The application is made up of Java (Spring Boot) and Node.js (NestJS) microservices orchestrated by Docker, exposed through a dedicated Nginx reverse proxy that acts as the single gateway.

## Access
- URL: `https://onboardr.lab.exemple` (HTTPS)
- Demo accounts (password `Password1!`):
  - `prospect1` — PROSPECT (holder of an onboarding dossier)
  - `prospect2` — PROSPECT (second fictitious identity)
  - `conformite1` — CONFORMITE (verification and decision)
- Authentication: a login form via the gateway, completed by a simulated one-time code (OTP) at the dossier verification step (lab code: `123456`). Inter-service calls are protected by an internal service key.

## Roles and rights

| Role | Rights |
| --- | --- |
| PROSPECT | Create and complete their own onboarding dossier, upload their documents, track progress, view only their own dossier. |
| CONFORMITE | View all dossiers, verify documents, make an accept or reject decision via the administration area. |

## Main journeys

### 1. Login
1. Open `https://onboardr.lab.exemple`.
2. Enter the username (e.g. `prospect1`) and the password `Password1!`.
3. The gateway opens the session and redirects to the prospect dashboard.

### 2. Opening an onboarding dossier
1. From the dashboard, start a new account opening journey.
2. The application creates the dossier: `POST /api/onboarding` → returns the dossier identifier (e.g. `1001`).
3. Fill in the requested fictitious identity information, then save (`PUT /api/onboarding/1001`).

### 3. Uploading supporting documents
1. In the dossier, choose "Add a document" (proof of identity, of address).
2. Upload the file: `POST /api/onboarding/1001/documents` (multipart, image or PDF).
3. The document appears in the dossier's document list.

### 4. OTP verification
1. At the verification step, enter the laboratory one-time code `123456`.
2. The application confirms the step: `POST /api/onboarding/1001/verify`.

### 5. Compliance decision
1. Log in as `conformite1`.
2. Review the dossier to process, then decide via the administration area: `POST /api/admin/onboarding/1001/decision` with `{"decision":"APPROVED"}` or `{"decision":"REJECTED"}`.
3. The prospect sees the updated status in the tracking of their dossier.

### 6. Example API call (REST via the gateway)
- View your dossier:

  Request:
  ```
  GET /api/onboarding/1001 HTTP/1.1
  Host: onboardr.lab.exemple
  Cookie: session=<jeton prospect1>
  ```
  Response (fictitious):
  ```json
  {
    "id": 1001,
    "subject": "Prospect Un",
    "status": "PENDING",
    "documents": [{ "id": 5001, "type": "ID", "name": "piece.pdf" }]
  }
  ```

## Notes
- "Test environment – fictitious data" banner present on every page.
- To reset the demo data: `POST /admin/reset`.
