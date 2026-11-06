> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# APP-05 — CréditFlux (Portail de crédit)

> **Environnement de test — DONNÉES 100 % FICTIVES — usage défensif.**
> Application de laboratoire du *Banking DAST Benchmark Lab*, délibérément
> vulnérable pour évaluer des scanners DAST. Aucune marque réelle, aucun secret
> réel. **Sécurisée par défaut** : les seules faiblesses sont les écarts
> localisés du corpus (APP05-V001..V023).

## Pile technique

- Python 3.12 / **Django 5.1**, rendu serveur (SSR).
- Base **SQLite** en profil autonome (démarrage immédiat, état fictif seedé).
- Upload multipart de justificatifs, import de documents XML, récupération de
  pièce distante — **accent : traitement de fichiers**.
- Démarrage production : `web: gunicorn creditflux.wsgi --bind 0.0.0.0:5000`.

## Démarrage local

```bash
pip install -r requirements.txt
python manage.py migrate          # (optionnel : la 1re requête migre+seed)
python manage.py runserver 0.0.0.0:8080
```

L'application migre et **seed l'état de laboratoire automatiquement** à la
première requête. Pour réinitialiser l'état : `POST /admin/reset`.

### Comptes de laboratoire (mot de passe fictif `Password1!`)

| Compte       | Rôle       |
|--------------|------------|
| emprunteur1  | EMPRUNTEUR |
| emprunteur2  | EMPRUNTEUR |
| analyste1    | ANALYSTE   |

État seedé : dossier `1001` (DRAFT, 10000, emprunteur1), dossier `1002`
(SUBMITTED, emprunteur2), pièces `2001` (emprunteur1) et `2002` (emprunteur2).

## Endpoints principaux

| Méthode | Chemin                                  | Rôle |
|---------|-----------------------------------------|------|
| GET     | `/`                                     | Accueil |
| GET/POST| `/login` (`next`)                       | Connexion |
| GET     | `/applications` (`status`)              | Suivi des dossiers |
| GET     | `/applications/<id>`                    | Détail dossier |
| POST    | `/applications/<id>/documents`          | Upload de justificatif |
| POST    | `/applications/<id>/amount`             | Montant |
| POST    | `/applications/<id>/comment`            | Commentaire analyste |
| POST    | `/applications/<id>/submit`             | Soumission |
| POST    | `/applications/<id>/approve`            | Accord (analyste) |
| GET     | `/documents/<id>`                       | Téléchargement de pièce |
| POST    | `/documents/fetch`                      | Récupération de pièce distante |
| POST    | `/import`                               | Import XML |
| POST    | `/import/session`                       | Reprise de session d'import |
| POST    | `/profile/avatar`                       | Avatar (upload sécurisé — NC) |
| POST    | `/admin/reset`                          | Réinitialisation de l'état |

Routes *negative controls* (sûres) : `/profile/avatar`, `/import/secure`,
`/documents/fetch-allowlisted`, `/import/session-secure`,
`/documents/secure/<id>`, `/applications/<id>/submit-secure`.

## Déploiement Elastic Beanstalk (plateforme Python, AL2023)

- `Procfile` : démarrage gunicorn (port 5000).
- `.ebextensions/01-options.config` : **variables d'environnement UNIQUEMENT**
  (aucune option de conteneur dépréciée — leçon APP-01). Type d'instance,
  instance profile, single-instance/ALB et HTTPS sont gérés par la CLI `eb`.
- `.ebignore` : exclut `benchmark/` et la documentation sensible du paquet
  déployé (l'oracle de notation n'est **jamais** exposé).

## Benchmark (oracle — NE JAMAIS EXPOSER)

- `benchmark/ground-truth.yaml` : vérité terrain des 23 vulnérabilités.
- `benchmark/validation/validate.py` : rejoue 1 test par vulnérabilité (PASS si
  comportement vulnérable confirmé) et 1 test par negative control (PASS si
  l'attaque est bloquée), avec `POST /admin/reset` entre chaque cas et
  production de `validation-report.json` / `.html`.

```bash
pip install -r benchmark/validation/requirements.txt
python benchmark/validation/validate.py --base http://localhost:8080
```

> Le dossier `benchmark/` contient l'oracle de notation : il ne doit **jamais**
> être servi par HTTP ni fourni à un scanner.

## Note sur le décompte

Certains documents de cadrage mentionnent « 16 » vulnérabilités ; le corpus
(`corpus_master.yaml`), `VULNERABILITY_GUIDE.md` et `ground-truth.yaml` font foi
avec **23 vulnérabilités** (V001..V023).

---

# English version

# APP-05 — CréditFlux (Credit portal)

> **Test environment — 100% FICTITIOUS DATA — defensive use.**
> Lab application of the *Banking DAST Benchmark Lab*, deliberately
> vulnerable to evaluate DAST scanners. No real brand, no real
> secret. **Secure by default**: the only weaknesses are the
> localized deviations of the corpus (APP05-V001..V023).

## Technology stack

- Python 3.12 / **Django 5.1**, server-side rendering (SSR).
- **SQLite** database in standalone profile (immediate startup, seeded fictitious state).
- Multipart upload of supporting documents, XML document import, remote
  attachment fetching — **focus: file handling**.
- Production startup: `web: gunicorn creditflux.wsgi --bind 0.0.0.0:5000`.

## Local startup

```bash
pip install -r requirements.txt
python manage.py migrate          # (optionnel : la 1re requête migre+seed)
python manage.py runserver 0.0.0.0:8080
```

The application migrates and **seeds the lab state automatically** on the
first request. To reset the state: `POST /admin/reset`.

### Lab accounts (fictitious password `Password1!`)

| Account      | Role       |
|--------------|------------|
| emprunteur1  | EMPRUNTEUR |
| emprunteur2  | EMPRUNTEUR |
| analyste1    | ANALYSTE   |

Seeded state: application `1001` (DRAFT, 10000, emprunteur1), application `1002`
(SUBMITTED, emprunteur2), attachments `2001` (emprunteur1) and `2002` (emprunteur2).

## Main endpoints

| Method  | Path                                    | Purpose |
|---------|-----------------------------------------|---------|
| GET     | `/`                                     | Home |
| GET/POST| `/login` (`next`)                       | Login |
| GET     | `/applications` (`status`)              | Application tracking |
| GET     | `/applications/<id>`                    | Application detail |
| POST    | `/applications/<id>/documents`          | Supporting document upload |
| POST    | `/applications/<id>/amount`             | Amount |
| POST    | `/applications/<id>/comment`            | Analyst comment |
| POST    | `/applications/<id>/submit`             | Submission |
| POST    | `/applications/<id>/approve`            | Approval (analyst) |
| GET     | `/documents/<id>`                       | Attachment download |
| POST    | `/documents/fetch`                      | Remote attachment fetching |
| POST    | `/import`                               | XML import |
| POST    | `/import/session`                       | Resume import session |
| POST    | `/profile/avatar`                       | Avatar (secure upload — NC) |
| POST    | `/admin/reset`                          | State reset |

*Negative control* routes (safe): `/profile/avatar`, `/import/secure`,
`/documents/fetch-allowlisted`, `/import/session-secure`,
`/documents/secure/<id>`, `/applications/<id>/submit-secure`.

## Elastic Beanstalk deployment (Python platform, AL2023)

- `Procfile`: gunicorn startup (port 5000).
- `.ebextensions/01-options.config`: **environment variables ONLY**
  (no deprecated container option — APP-01 lesson). Instance type,
  instance profile, single-instance/ALB and HTTPS are managed by the `eb` CLI.
- `.ebignore`: excludes `benchmark/` and sensitive documentation from the
  deployed package (the scoring oracle is **never** exposed).

## Benchmark (oracle — NEVER EXPOSE)

- `benchmark/ground-truth.yaml`: ground truth of the 23 vulnerabilities.
- `benchmark/validation/validate.py`: replays 1 test per vulnerability (PASS if
  vulnerable behavior confirmed) and 1 test per negative control (PASS if
  the attack is blocked), with `POST /admin/reset` between each case and
  production of `validation-report.json` / `.html`.

```bash
pip install -r benchmark/validation/requirements.txt
python benchmark/validation/validate.py --base http://localhost:8080
```

> The `benchmark/` folder contains the scoring oracle: it must **never**
> be served over HTTP nor provided to a scanner.

## Note on the count

Some scoping documents mention "16" vulnerabilities; the corpus
(`corpus_master.yaml`), `VULNERABILITY_GUIDE.md` and `ground-truth.yaml` are
authoritative with **23 vulnerabilities** (V001..V023).
