> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# ConseilDesk — Guide utilisateur

> **Environnement de test – données fictives.** Application de laboratoire ; toutes les données sont fictives.

## Présentation
ConseilDesk est le back-office des conseillers d'une banque de détail fictive. Il permet de rechercher des clients, de consulter et tenir à jour leurs dossiers et notes internes, de produire des exports, et — pour l'encadrement — d'administrer les rôles des collaborateurs. Les utilisateurs sont des conseillers de clientèle et des directeurs d'agence.

## Accès
- URL : `https://conseildesk.lab.exemple` (HTTPS)
- Comptes de démonstration (mot de passe `Password1!`) :
  - `conseiller1` — rôle Conseiller
  - `conseiller2` — rôle Conseiller
  - `directeur1` — rôle Directeur
- Authentification par formulaire web (`POST /login`), session serveur portée par le cookie `PHPSESSID`. Pas de 2FA sur ce périmètre.

## Rôles et droits

| Rôle | Droits |
| --- | --- |
| Conseiller | Rechercher les clients de son portefeuille, consulter dossiers et notes de ses clients, ajouter des notes, produire des exports standard, faire progresser les dossiers selon le workflow. |
| Directeur | Tous les droits du conseiller, plus : accès transverse aux dossiers, exports complets (champs restreints), clôture des dossiers validés, et administration des rôles des utilisateurs (`/admin/users`). |

## Parcours principaux

### 1. Connexion
1. Ouvrir `https://conseildesk.lab.exemple`.
2. Saisir l'identifiant (`conseiller1`) et le mot de passe (`Password1!`).
3. Après `POST /login`, redirection vers le tableau de bord ; la session est portée par `PHPSESSID`.

### 2. Recherche et consultation d'un client
1. Depuis le tableau de bord, utiliser le champ de recherche.
2. `GET /clients?search=Durand` affiche la liste filtrée du portefeuille.
3. Cliquer sur un client ouvre sa fiche ; `GET /clients/{id}/notes` affiche l'historique des notes internes.

### 3. Ajout d'une note interne
1. Depuis la fiche client, ouvrir « Nouvelle note ».
2. Saisir le texte et valider : `POST /clients/{id}/notes` avec le champ `body`.
3. La note apparaît dans l'historique, visible par les collaborateurs habilités sur ce dossier.

### 4. Export d'un dossier
1. Depuis la fiche client, choisir « Exporter » et sélectionner un gabarit.
2. `GET /export?template=dossier&id={id}` génère le document de synthèse.
3. L'export standard masque les champs réservés ; l'export complet n'est destiné qu'aux directeurs.

### 5. Clôture d'un dossier
1. Un dossier validé propose l'action « Clôturer ».
2. `POST /files/{id}/close` enregistre la clôture.
3. Le dossier passe à l'état `CLOTURE` et sort de la file active.

### 6. Administration des rôles (directeur)
1. Ouvrir « Administration » → « Utilisateurs » (visible pour le rôle Directeur).
2. Sélectionner un collaborateur et choisir son nouveau rôle.
3. `POST /admin/users/{id}/role` avec le champ `role` enregistre le changement.

### API légère (REST)
Le front consomme une API JSON interne, portée par le cookie de session.
- Requête : `GET /api/clients/{id}` avec `Accept: application/json`.
- Réponse (fictive) :
  ```json
  { "id": 1500, "nom": "Durand", "segment": "Particulier", "conseiller": "conseiller1" }
  ```

## Notes
- Bandeau « Environnement de test – données fictives » présent sur toutes les pages.
- Pour réinitialiser les données de démonstration : `POST /admin/reset`.

---

# English version

# ConseilDesk — User guide

> **Test environment – fictitious data.** Lab application; all data is fictitious.

## Introduction
ConseilDesk is the advisor back-office of a fictitious retail bank. It lets you search for clients, view and maintain their case files and internal notes, produce exports, and — for management — administer staff roles. The users are client advisors and branch directors.

## Access
- URL: `https://conseildesk.lab.exemple` (HTTPS)
- Demo accounts (password `Password1!`):
  - `conseiller1` — Advisor role
  - `conseiller2` — Advisor role
  - `directeur1` — Director role
- Web form authentication (`POST /login`), server session carried by the `PHPSESSID` cookie. No 2FA on this scope.

## Roles and rights

| Role | Rights |
| --- | --- |
| Advisor | Search the clients in their portfolio, view their clients' case files and notes, add notes, produce standard exports, advance case files according to the workflow. |
| Director | All advisor rights, plus: cross-cutting access to case files, full exports (restricted fields), closure of validated case files, and administration of user roles (`/admin/users`). |

## Main journeys

### 1. Login
1. Open `https://conseildesk.lab.exemple`.
2. Enter the username (`conseiller1`) and the password (`Password1!`).
3. After `POST /login`, redirect to the dashboard; the session is carried by `PHPSESSID`.

### 2. Searching and viewing a client
1. From the dashboard, use the search field.
2. `GET /clients?search=Durand` displays the filtered list of the portfolio.
3. Clicking a client opens their record; `GET /clients/{id}/notes` displays the history of internal notes.

### 3. Adding an internal note
1. From the client record, open "New note".
2. Enter the text and submit: `POST /clients/{id}/notes` with the `body` field.
3. The note appears in the history, visible to the staff authorized on this case file.

### 4. Exporting a case file
1. From the client record, choose "Export" and select a template.
2. `GET /export?template=dossier&id={id}` generates the summary document.
3. The standard export hides the reserved fields; the full export is intended for directors only.

### 5. Closing a case file
1. A validated case file offers the "Close" action.
2. `POST /files/{id}/close` records the closure.
3. The case file moves to the `CLOTURE` state and leaves the active queue.

### 6. Role administration (director)
1. Open "Administration" → "Users" (visible to the Director role).
2. Select a staff member and choose their new role.
3. `POST /admin/users/{id}/role` with the `role` field records the change.

### Lightweight API (REST)
The front end consumes an internal JSON API, carried by the session cookie.
- Request: `GET /api/clients/{id}` with `Accept: application/json`.
- Response (fictitious):
  ```json
  { "id": 1500, "nom": "Durand", "segment": "Particulier", "conseiller": "conseiller1" }
  ```

## Notes
- "Test environment – fictitious data" banner present on every page.
- To reset the demo data: `POST /admin/reset`.
