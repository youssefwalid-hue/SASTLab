# APP-01 — Hélios Banque

> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

> **Environnement de test – données fictives.** Application bancaire de laboratoire du
> **Banking DAST Benchmark Lab**, volontairement vulnérable, à usage **défensif** (évaluation de
> scanners DAST en laboratoire interne). Aucune donnée réelle, aucune marque réelle, aucun secret réel.

Hélios Banque est **sécurisée par défaut** : les **22 vulnérabilités** du corpus sont des **écarts
localisés** taggués en commentaire `// GT: APP01-Vxxx`, et **13 negative controls** (`// NC: APP01-Nxxx`)
fournissent des routes comparables correctement sécurisées. Voir `SECURITY_MODEL.md`.

## Pile technique

- Java 21, Spring Boot 3.3.x, Maven
- Spring Security (form login, rôles CLIENT / ADVISOR / ADMIN)
- Thymeleaf (rendu serveur), Spring Data JPA
- Base : **H2 en mémoire** (profil `local`) / **PostgreSQL** (profil `prod`)
- Déploiement : JAR exécutable sur AWS Elastic Beanstalk (Corretto 21, **Single Instance**) derrière Nginx (HTTPS autosigné sur l'instance, 80 → 443) — chaîne commune `deploy/deploy.sh APP-01`, paramètres `deploy/apps/APP-01.env`

## Comptes de laboratoire

Mot de passe commun : `Password1!` (stocké en BCrypt). OTP de laboratoire fixe : `123456`.

| Identifiant | Rôle    | Compte     |
|-------------|---------|------------|
| `alice`     | CLIENT  | `ACC-1001` |
| `bruno`     | CLIENT  | `ACC-2001` |
| `claire`    | ADVISOR | —          |
| `admin`     | ADMIN   | —          |

## Lancement local

```bash
mvn spring-boot:run -Dspring-boot.run.profiles=local
```

Application disponible sur `http://localhost:8080` (profil `local`, H2 en mémoire).

Build du JAR :

```bash
mvn clean package
java -jar target/helios-banque.jar --spring.profiles.active=local
```

> Note : la compilation nécessite l'accès aux dépôts Maven (Spring Boot, dépendances). Dans un
> environnement sans accès réseau sortant vers Maven Central, pré-alimenter le cache `~/.m2`.

## Réinitialisation de l'état

```bash
curl -X POST http://localhost:8080/admin/reset
```

Recharge l'intégralité des données fictives (utilisateurs, comptes, opérations, bénéficiaires,
virements, relevés).

## Endpoints principaux

- `GET /` tableau de bord · `GET /search?q=` · `GET /accounts/{accountId}` ·
  `GET /accounts/{accountId}/transactions?label=&counterparty=`
- `GET /beneficiaries` · `POST /beneficiaries` · `POST /beneficiaries/{id}/edit`
- `GET /statements` · `GET /statements/download?file=` · `GET /statements/by-id/{statementId}`
- `POST /transfers` · `POST /transfers/{id}/validate-otp` · `POST /transfers/{id}/confirm` ·
  `POST /transfers/{id}/finalize`
- `GET /api/accounts/{accountId}/balance` · `GET /api/accounts/{accountId}/summary`
- `POST /password/forgot` · `POST /password/reset` · `POST /password/reset-secure`
- `POST /admin/reset`
- Actuator : `/actuator/env` (exposé — écart APP01-V013)

## Déploiement Elastic Beanstalk

- `.ebextensions/01-options.config` : variables d'environnement uniquement (`SPRING_PROFILES_ACTIVE=ebstandalone`,
  `SERVER_PORT=5000`) ; type d'instance, Single Instance, Security Group et rôles sont pilotés par `deploy/deploy.sh`
  (options EB passées par la CLI, valides sur Amazon Linux 2023). Profil `ebstandalone` = H2 en mémoire, aucune base externe,
  aucun secret.
- `.platform/nginx/conf.d/` : règles Nginx, dont le blocage `/actuator` **contournable** par
  normalisation de chemin (écart APP01-V015) et `server_tokens on` (APP01-V016).
- `.platform/hooks/postdeploy/` : création du répertoire des relevés.
- Au packaging (`deploy/package.sh`), le template TLS commun ajoute `.platform/hooks/prebuild/00-tls-selfsigned.sh` (certificat autosigné généré sur l'instance), `.platform/nginx/conf.d/https.conf` (serveur 443) et `elasticbeanstalk/zz-redirect-https.conf` (80 → 443) ; les fichiers `.platform` propres à l'application sont conservés.

## Avertissement

`VULNERABILITY_GUIDE.md`, `negative-controls.yaml`, `BUILD_SPEC.md` et `ARCHITECTURE.md` sont des
documents de laboratoire. **Ne jamais les exposer par HTTP ni les fournir aux scanners.**

---

# English version

# APP-01 — Hélios Banque

> **Test environment – fictitious data.** Laboratory banking application of the
> **Banking DAST Benchmark Lab**, intentionally vulnerable, for **defensive** use (evaluation of DAST
> scanners in an internal lab). No real data, no real brand, no real secret.

Hélios Banque is **secure by default**: the **22 vulnerabilities** of the corpus are **localized
deviations** tagged in a `// GT: APP01-Vxxx` comment, and **13 negative controls** (`// NC: APP01-Nxxx`)
provide comparable, correctly secured routes. See `SECURITY_MODEL.md`.

## Technical stack

- Java 21, Spring Boot 3.3.x, Maven
- Spring Security (form login, roles CLIENT / ADVISOR / ADMIN)
- Thymeleaf (server-side rendering), Spring Data JPA
- Database: **in-memory H2** (`local` profile) / **PostgreSQL** (`prod` profile)
- Deployment: executable JAR on AWS Elastic Beanstalk (Corretto 21, **Single Instance**) behind Nginx (self-signed HTTPS on the instance, 80 → 443) — shared chain `deploy/deploy.sh APP-01`, parameters `deploy/apps/APP-01.env`

## Lab accounts

Shared password: `Password1!` (stored as BCrypt). Fixed lab OTP: `123456`.

| Identifier  | Role    | Account    |
|-------------|---------|------------|
| `alice`     | CLIENT  | `ACC-1001` |
| `bruno`     | CLIENT  | `ACC-2001` |
| `claire`    | ADVISOR | —          |
| `admin`     | ADMIN   | —          |

## Local launch

```bash
mvn spring-boot:run -Dspring-boot.run.profiles=local
```

Application available at `http://localhost:8080` (`local` profile, in-memory H2).

Build the JAR:

```bash
mvn clean package
java -jar target/helios-banque.jar --spring.profiles.active=local
```

> Note: compilation requires access to the Maven repositories (Spring Boot, dependencies). In an
> environment without outbound network access to Maven Central, pre-populate the `~/.m2` cache.

## State reset

```bash
curl -X POST http://localhost:8080/admin/reset
```

Reloads the full set of fictitious data (users, accounts, operations, beneficiaries,
transfers, statements).

## Main endpoints

- `GET /` dashboard · `GET /search?q=` · `GET /accounts/{accountId}` ·
  `GET /accounts/{accountId}/transactions?label=&counterparty=`
- `GET /beneficiaries` · `POST /beneficiaries` · `POST /beneficiaries/{id}/edit`
- `GET /statements` · `GET /statements/download?file=` · `GET /statements/by-id/{statementId}`
- `POST /transfers` · `POST /transfers/{id}/validate-otp` · `POST /transfers/{id}/confirm` ·
  `POST /transfers/{id}/finalize`
- `GET /api/accounts/{accountId}/balance` · `GET /api/accounts/{accountId}/summary`
- `POST /password/forgot` · `POST /password/reset` · `POST /password/reset-secure`
- `POST /admin/reset`
- Actuator: `/actuator/env` (exposed — deviation APP01-V013)

## Elastic Beanstalk deployment

- `.ebextensions/01-options.config`: environment variables only (`SPRING_PROFILES_ACTIVE=ebstandalone`,
  `SERVER_PORT=5000`); instance type, Single Instance, Security Group and roles are driven by `deploy/deploy.sh`
  (EB options passed via the CLI, valid on Amazon Linux 2023). The `ebstandalone` profile = in-memory H2, no external database,
  no secret.
- `.platform/nginx/conf.d/`: Nginx rules, including the `/actuator` block that is **bypassable** through
  path normalization (deviation APP01-V015) and `server_tokens on` (APP01-V016).
- `.platform/hooks/postdeploy/`: creation of the statements directory.
- At packaging time (`deploy/package.sh`), the shared TLS template adds `.platform/hooks/prebuild/00-tls-selfsigned.sh` (self-signed certificate generated on the instance), `.platform/nginx/conf.d/https.conf` (443 server) and `elasticbeanstalk/zz-redirect-https.conf` (80 → 443); the application-specific `.platform` files are preserved.

## Warning

`VULNERABILITY_GUIDE.md`, `negative-controls.yaml`, `BUILD_SPEC.md` and `ARCHITECTURE.md` are
laboratory documents. **Never expose them over HTTP or provide them to scanners.**
