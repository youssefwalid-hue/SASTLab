> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# CartaVie — Guide utilisateur

> **Environnement de test – données fictives.** Application de laboratoire ; toutes les données sont fictives.

## Présentation
CartaVie est un service fictif de gestion des cartes bancaires permettant aux clients de piloter leurs cartes physiques et virtuelles : plafonds, opposition, création de cartes virtuelles et contestations. Il s'adresse à des clients particuliers et à des gestionnaires du service cartes, et expose à la fois une interface web et une API REST JSON (décrite par une spécification OpenAPI).

## Accès
- URL : `https://cartavie.lab.exemple` (HTTPS)
- Comptes de démonstration (mot de passe `Password1!`) :
  - `client-a` — client, propriétaire de cartes
  - `client-b` — client, propriétaire de cartes
  - `gestionnaire` — gestionnaire du service cartes
- Authentification : formulaire web avec cookie de session pour l'interface ; clé d'API transmise dans l'en-tête `X-API-Key` pour la partie REST. Les actions web sensibles sont protégées par un jeton anti-CSRF.

## Rôles et droits
| Rôle | Droits |
|------|--------|
| Client (`client-a`, `client-b`) | Consulter ses propres cartes, modifier les libellés, demander une évolution de plafond, mettre en opposition ses cartes, créer une carte virtuelle, ouvrir une contestation, appeler l'API REST avec sa clé |
| Gestionnaire (`gestionnaire`) | Consulter les cartes des clients, instruire les demandes de plafond, traiter les oppositions et les contestations |

## Parcours principaux

### 1. Connexion
1. Ouvrir `https://cartavie.lab.exemple` et saisir l'identifiant (ex. `client-a`) et le mot de passe `Password1!`.
2. Un cookie de session est émis ; le tableau de bord des cartes s'affiche.

### 2. Consulter et renommer une carte (web)
1. Depuis le tableau de bord, ouvrir la carte souhaitée (seuls les 4 derniers chiffres du numéro sont affichés).
2. Modifier le libellé via le formulaire d'édition (`card[label]`) et enregistrer ; le libellé apparaît dans la liste.

### 3. Mettre une carte en opposition
1. Ouvrir la carte concernée et choisir « Mettre en opposition ».
2. Le statut de la carte passe à `blocked` ; une confirmation est affichée.

### 4. Créer une carte virtuelle
1. Choisir « Créer une carte virtuelle » et définir un plafond initial dans le cadre autorisé.
2. La nouvelle carte virtuelle apparaît dans la liste avec son propre identifiant.

### 5. Ouvrir une contestation
1. Sur une opération, choisir « Contester » et indiquer le montant contesté.
2. Un recrédit provisoire est appliqué en attendant l'instruction par le gestionnaire.

### 6. Utiliser l'API REST
1. Obtenir sa clé d'API dans l'espace client et l'utiliser dans l'en-tête `X-API-Key`.
2. Lister ses cartes :
   - Requête : `GET /api/cards` avec en-tête `X-API-Key: key-client-a-7f3a`
   - Réponse (exemple fictif) :
     ```json
     {
       "cards": [
         {"id": 1001, "label": "Carte perso", "pan_masked": "**** **** **** 1234", "limit": 1000, "status": "active"}
       ]
     }
     ```
3. Consulter une carte : `GET /api/cards/1001`
   - Réponse (exemple fictif) :
     ```json
     {"id": 1001, "label": "Carte perso", "pan_masked": "**** **** **** 1234", "limit": 1000, "available": 1000, "status": "active"}
     ```
4. Renommer une carte : `PUT /api/cards/1001` avec le corps `{"label": "Vacances"}` et l'en-tête `X-API-Key`.

## Notes
- Bandeau « Environnement de test – données fictives » présent sur toutes les pages.
- Pour réinitialiser les données de démonstration : `POST /admin/reset`.

---

# English version

# CartaVie — User guide

> **Test environment – fictitious data.** Lab application; all data is fictitious.

## Introduction
CartaVie is a fictitious bank-card management service letting clients manage their physical and virtual cards: limits, card blocking, virtual-card creation and disputes. It targets individual clients and card-service managers, and exposes both a web interface and a JSON REST API (described by an OpenAPI specification).

## Access
- URL: `https://cartavie.lab.exemple` (HTTPS)
- Demo accounts (password `Password1!`):
  - `client-a` — client, card owner
  - `client-b` — client, card owner
  - `gestionnaire` — card-service manager
- Authentication: web form with a session cookie for the interface; API key passed in the `X-API-Key` header for the REST part. Sensitive web actions are protected by an anti-CSRF token.

## Roles and permissions
| Role | Permissions |
|------|--------|
| Client (`client-a`, `client-b`) | View their own cards, edit labels, request a limit change, block their cards, create a virtual card, open a dispute, call the REST API with their key |
| Manager (`gestionnaire`) | View clients' cards, process limit requests, handle card blockings and disputes |

## Main workflows

### 1. Login
1. Open `https://cartavie.lab.exemple` and enter the username (e.g. `client-a`) and the password `Password1!`.
2. A session cookie is issued; the card dashboard is displayed.

### 2. View and rename a card (web)
1. From the dashboard, open the desired card (only the last 4 digits of the number are shown).
2. Edit the label via the edit form (`card[label]`) and save; the label appears in the list.

### 3. Block a card
1. Open the relevant card and choose "Block".
2. The card status changes to `blocked`; a confirmation is displayed.

### 4. Create a virtual card
1. Choose "Create a virtual card" and set an initial limit within the allowed range.
2. The new virtual card appears in the list with its own identifier.

### 5. Open a dispute
1. On a transaction, choose "Dispute" and enter the disputed amount.
2. A provisional re-credit is applied while awaiting the manager's review.

### 6. Use the REST API
1. Obtain your API key from the client area and use it in the `X-API-Key` header.
2. List your cards:
   - Request: `GET /api/cards` with header `X-API-Key: key-client-a-7f3a`
   - Response (fictitious example):
     ```json
     {
       "cards": [
         {"id": 1001, "label": "Carte perso", "pan_masked": "**** **** **** 1234", "limit": 1000, "status": "active"}
       ]
     }
     ```
3. View a card: `GET /api/cards/1001`
   - Response (fictitious example):
     ```json
     {"id": 1001, "label": "Carte perso", "pan_masked": "**** **** **** 1234", "limit": 1000, "available": 1000, "status": "active"}
     ```
4. Rename a card: `PUT /api/cards/1001` with body `{"label": "Vacances"}` and the `X-API-Key` header.

## Notes
- "Test environment – fictitious data" banner present on all pages.
- To reset the demo data: `POST /admin/reset`.
