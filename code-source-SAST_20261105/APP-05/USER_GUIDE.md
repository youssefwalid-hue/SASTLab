> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# CréditFlux — Guide utilisateur

> **Environnement de test – données fictives.** Application de laboratoire ; toutes les données sont fictives.

## Présentation
CréditFlux est un portail de crédit fictif permettant à des emprunteurs de déposer une demande de prêt, de joindre leurs justificatifs et de suivre l'avancement de leur dossier. Les analystes crédit y instruisent les demandes, commentent les dossiers et rendent une décision. L'application repose sur une pile Python/Django avec rendu serveur et dépôt de pièces en multipart.

## Accès
- URL : `https://creditflux.lab.exemple` (HTTPS)
- Comptes de démonstration (mot de passe `Password1!`) :
  - `emprunteur1` — rôle EMPRUNTEUR
  - `emprunteur2` — rôle EMPRUNTEUR
  - `analyste1` — rôle ANALYSTE
- Authentification par formulaire de connexion, session serveur et jeton anti-CSRF sur les actions à effet de bord. Code OTP de laboratoire si demandé : `123456`.

## Rôles et droits
| Rôle | Droits |
| --- | --- |
| EMPRUNTEUR | Créer une demande de prêt, saisir le montant en phase brouillon, déposer et consulter ses propres justificatifs, importer une pièce depuis une URL, soumettre son dossier, suivre son avancement. |
| ANALYSTE | Consulter les dossiers soumis, importer des documents structurés, commenter un dossier, approuver ou refuser une demande. |

## Parcours principaux

### 1. Connexion
1. Ouvrir `https://creditflux.lab.exemple/login`.
2. Saisir l'identifiant (ex. `emprunteur1`) et le mot de passe `Password1!`.
3. Après authentification, redirection vers le tableau de bord des dossiers (paramètre `next` interne).

### 2. Créer une demande de prêt et fixer le montant
1. Depuis le tableau de bord, choisir « Nouvelle demande » : un dossier est créé au statut `DRAFT`.
2. Renseigner le montant souhaité : `POST /applications/{id}/amount` avec `amount=10000`.
3. Le dossier reste modifiable tant qu'il est en brouillon.

### 3. Déposer un justificatif
1. Ouvrir le détail du dossier puis « Ajouter une pièce ».
2. Téléverser un fichier : `POST /applications/{id}/documents` (multipart, champ `file`), par exemple un bulletin au format PDF.
3. La pièce apparaît dans la liste des justificatifs du dossier et reste consultable par son propriétaire via `GET /documents/{id}`.

### 4. Importer une pièce depuis une URL
1. Dans l'écran de dépôt, choisir « Importer depuis une URL ».
2. Fournir l'adresse d'un justificatif de laboratoire : `POST /documents/fetch` avec `{"applicationId":{id},"url":"https://creditflux.lab.exemple/media/sample.pdf"}`.
3. Le serveur récupère la ressource et la rattache au dossier.

### 5. Soumettre et suivre le dossier
1. Une fois les pièces déposées, soumettre la demande : `POST /applications/{id}/submit` (jeton anti-CSRF requis). Le statut passe à `SUBMITTED`.
2. Suivre l'avancement depuis le tableau de bord ou en filtrant la liste : `GET /applications?status=SUBMITTED`.

### 6. Instruction par l'analyste
1. L'analyste se connecte (`analyste1`) et ouvre un dossier soumis.
2. Il peut importer un document structuré : `POST /import` (multipart, champ `file`).
3. Il ajoute un commentaire : `POST /applications/{id}/comment` avec `text=...`.
4. Il rend une décision ; à l'approbation, le dossier passe au statut `APPROVED` et le montant est verrouillé.

## Notes
- Bandeau « Environnement de test – données fictives » présent sur toutes les pages.
- Pour réinitialiser les données de démonstration : `POST /admin/reset`.

---

# English version

# CréditFlux — User guide

> **Test environment – fictitious data.** Lab application; all data is fictitious.

## Overview
CréditFlux is a fictitious credit portal that lets borrowers file a loan application, attach their supporting documents and track the progress of their file. Credit analysts review the applications, comment on the files and issue a decision. The application is built on a Python/Django stack with server-side rendering and multipart attachment submission.

## Access
- URL: `https://creditflux.lab.exemple` (HTTPS)
- Demo accounts (password `Password1!`):
  - `emprunteur1` — EMPRUNTEUR role
  - `emprunteur2` — EMPRUNTEUR role
  - `analyste1` — ANALYSTE role
- Authentication via a login form, a server session and an anti-CSRF token on side-effecting actions. Lab OTP code if requested: `123456`.

## Roles and permissions
| Role | Permissions |
| --- | --- |
| EMPRUNTEUR | Create a loan application, enter the amount in the draft phase, submit and view their own supporting documents, import an attachment from a URL, submit their file, track its progress. |
| ANALYSTE | View submitted files, import structured documents, comment on a file, approve or reject an application. |

## Main journeys

### 1. Login
1. Open `https://creditflux.lab.exemple/login`.
2. Enter the username (e.g. `emprunteur1`) and the password `Password1!`.
3. After authentication, redirect to the applications dashboard (internal `next` parameter).

### 2. Create a loan application and set the amount
1. From the dashboard, choose "New application": a file is created with status `DRAFT`.
2. Enter the desired amount: `POST /applications/{id}/amount` with `amount=10000`.
3. The file remains editable as long as it is a draft.

### 3. Submit a supporting document
1. Open the file detail then "Add an attachment".
2. Upload a file: `POST /applications/{id}/documents` (multipart, field `file`), for example a pay slip in PDF format.
3. The attachment appears in the file's list of supporting documents and remains viewable by its owner via `GET /documents/{id}`.

### 4. Import an attachment from a URL
1. In the submission screen, choose "Import from a URL".
2. Provide the address of a lab supporting document: `POST /documents/fetch` with `{"applicationId":{id},"url":"https://creditflux.lab.exemple/media/sample.pdf"}`.
3. The server fetches the resource and attaches it to the file.

### 5. Submit and track the file
1. Once the attachments are submitted, submit the application: `POST /applications/{id}/submit` (anti-CSRF token required). The status changes to `SUBMITTED`.
2. Track progress from the dashboard or by filtering the list: `GET /applications?status=SUBMITTED`.

### 6. Review by the analyst
1. The analyst logs in (`analyste1`) and opens a submitted file.
2. They can import a structured document: `POST /import` (multipart, field `file`).
3. They add a comment: `POST /applications/{id}/comment` with `text=...`.
4. They issue a decision; on approval, the file changes to status `APPROVED` and the amount is locked.

## Notes
- "Test environment – fictitious data" banner present on all pages.
- To reset the demo data: `POST /admin/reset`.
