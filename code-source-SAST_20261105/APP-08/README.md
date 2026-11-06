> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# CartaVie — Gestion des cartes (APP-08)

> **Environnement de test — données 100 % fictives — usage défensif (laboratoire DAST interne).**
> Application **volontairement vulnérable** destinée à évaluer des scanners DAST.
> Aucune donnée réelle, aucun secret réel, aucune marque réelle. Toutes les réponses
> portent l'en-tête `X-Lab-Environment: test-data-fictives`.

CartaVie est un service fictif de gestion des cartes bancaires (plafonds, opposition,
cartes virtuelles, contestations) exposant une **interface web (ERB)** et une **API REST JSON**.
L'application est **sécurisée par défaut** ; les **22 vulnérabilités** du référentiel sont des
**écarts localisés** et tagués (`// GT: APP08-Vxxx`). Des **negative controls** (`// NC: APP08-Nxxx`)
fournissent des routes sœurs correctement sécurisées.

## Pile

- Ruby 3.3, Ruby on Rails (frameworks chargés sélectivement : active_record, action_controller, action_view)
- Base **SQLite** en profil standalone (démarrage immédiat, données fictives amorcées au boot)
- Serveur **Puma** ; plateforme cible **Elastic Beanstalk Ruby**

## Démarrage local

```bash
bundle install
bin/rails server -b 0.0.0.0 -p 8080
```

Le schéma et les données fictives sont créés automatiquement au démarrage
(`config/initializers/zz_bootstrap_db.rb`). Réinitialisation à la demande :

```bash
curl -X POST http://localhost:8080/admin/reset
```

## Comptes de démonstration (mot de passe `Password1!`)

| Compte | Rôle | Clé d'API REST |
|--------|------|----------------|
| `client-a` | client | `key-client-a-7f3a` |
| `client-b` | client | `key-client-b-9b2c` |
| `gestionnaire` | gestionnaire | `key-manager-3d1e` |

- **Web** : formulaire + cookie de session `_session`.
- **API REST** : clé transmise dans l'en-tête `X-API-Key`.

## Cartes fictives

| ID | Propriétaire | Libellé | Type | Plafond |
|----|--------------|---------|------|---------|
| 1001 | client-a | Carte perso | physique | 1000 € |
| 1003 | client-a | Alpha secours | physique | 2000 € |
| 1002 | client-b | Carte voyage | virtuelle | 500 € |

## Accent de sécurité (mixte Web/REST)

Mass assignment des plafonds (API), IDOR sur carte virtuelle (API), clé d'API rejouable,
contournement des strong params (web). Voir `SECURITY_MODEL.md` pour la cartographie
complète des 22 écarts et des 12 negative controls.

## Déploiement Elastic Beanstalk (Ruby)

- `Procfile` : `web: bundle exec puma -p 5000`
- `.ebextensions/01-options.config` : **variables d'environnement uniquement** (AL2023)
- `.ebignore` : exclut `benchmark/` et la documentation sensible du bundle déployé

## Benchmark (NE JAMAIS EXPOSER PAR HTTP)

- `benchmark/ground-truth.yaml` : vérité terrain des 22 vulnérabilités (oracle de notation)
- `benchmark/validation/validate.py` : validation automatisée (1 test/vuln + 1 test/NC)

> Le dossier `benchmark/` et les fichiers `VULNERABILITY_GUIDE.md`, `negative-controls.yaml`,
> `ground-truth.yaml` ne doivent **jamais** être servis par HTTP ni fournis aux scanners.

---

# English version

# CartaVie — Card Management (APP-08)

> **Test environment — 100% fictitious data — defensive use (internal DAST lab).**
> **Deliberately vulnerable** application intended to evaluate DAST scanners.
> No real data, no real secrets, no real brands. All responses carry the
> `X-Lab-Environment: test-data-fictives` header.

CartaVie is a fictitious bank-card management service (limits, card blocking,
virtual cards, disputes) exposing a **web interface (ERB)** and a **JSON REST API**.
The application is **secure by default**; the **22 vulnerabilities** of the reference set are
**localized deviations**, each tagged (`// GT: APP08-Vxxx`). **Negative controls** (`// NC: APP08-Nxxx`)
provide correctly secured sibling routes.

## Stack

- Ruby 3.3, Ruby on Rails (frameworks loaded selectively: active_record, action_controller, action_view)
- **SQLite** database in standalone profile (instant startup, fictitious data seeded at boot)
- **Puma** server; target platform **Elastic Beanstalk Ruby**

## Local startup

```bash
bundle install
bin/rails server -b 0.0.0.0 -p 8080
```

The schema and fictitious data are created automatically at startup
(`config/initializers/zz_bootstrap_db.rb`). Reset on demand:

```bash
curl -X POST http://localhost:8080/admin/reset
```

## Demo accounts (password `Password1!`)

| Account | Role | REST API key |
|--------|------|----------------|
| `client-a` | client | `key-client-a-7f3a` |
| `client-b` | client | `key-client-b-9b2c` |
| `gestionnaire` | manager | `key-manager-3d1e` |

- **Web**: form + `_session` session cookie.
- **REST API**: key passed in the `X-API-Key` header.

## Fictitious cards

| ID | Owner | Label | Type | Limit |
|----|--------------|---------|------|---------|
| 1001 | client-a | Carte perso | physical | 1000 € |
| 1003 | client-a | Alpha secours | physical | 2000 € |
| 1002 | client-b | Carte voyage | virtual | 500 € |

## Security focus (mixed Web/REST)

Mass assignment of limits (API), IDOR on a virtual card (API), replayable API key,
strong-params bypass (web). See `SECURITY_MODEL.md` for the complete mapping
of the 16 deviations and the 12 negative controls.

## Elastic Beanstalk deployment (Ruby)

- `Procfile`: `web: bundle exec puma -p 5000`
- `.ebextensions/01-options.config`: **environment variables only** (AL2023)
- `.ebignore`: excludes `benchmark/` and sensitive documentation from the deployed bundle

## Benchmark (NEVER EXPOSE OVER HTTP)

- `benchmark/ground-truth.yaml`: ground truth of the 22 vulnerabilities (scoring oracle)
- `benchmark/validation/validate.py`: automated validation (1 test/vuln + 1 test/NC)

> The `benchmark/` folder and the files `VULNERABILITY_GUIDE.md`, `negative-controls.yaml`,
> `ground-truth.yaml` must **never** be served over HTTP nor provided to scanners.
