# Corpus SAST — code source des 10 applications (données fictives)

> 🇫🇷 / 🇬🇧 — Code source **déployé** des 10 applications bancaires volontairement vulnérables,
> extrait pour servir de **corpus de benchmark SAST** (analyse statique). Données 100 % fictives.

## FR
- Un dossier par application : `APP-01` … `APP-10`, chacun avec son code source et ses manifestes
  de build (package.json / pom.xml / *.csproj / go.mod / requirements / Gemfile…), sans artefacts
  compilés ni dépendances téléchargées (à restaurer : `npm/pip/go/dotnet/bundle install` selon la pile).
- **L'oracle n'est pas inclus** (ground-truth, negative-controls, VULNERABILITY_GUIDE, SECURITY_MODEL) :
  le corpus est « aveugle » pour ne pas biaiser les scanners. L'oracle reste dans le lab principal.
- `_GT_MARKERS_REPORT.txt` liste les éventuels marqueurs `GT: APPxx-Vnn` restés en commentaire.
  Les retirer (`make-source-package.sh --strip-gt`) avant de fournir le code à un outil SAST.
- Piles : APP-01 Java/Spring Boot · APP-02 Node/NestJS · APP-03 .NET · APP-04 PHP/Symfony ·
  APP-05 Python/Django · APP-06 Go · APP-07 .NET Framework/WCF · APP-08 Ruby/Rails ·
  APP-09 Python/FastAPI · APP-10 microservices (nginx + Java/Node).

## EN
- One folder per application (`APP-01`…`APP-10`) with source code and build manifests, no compiled
  artifacts nor downloaded dependencies (restore with the stack's package manager).
- **The oracle is excluded** (ground-truth, negative-controls, VULNERABILITY_GUIDE, SECURITY_MODEL):
  the corpus is "blind" so it does not bias scanners. The oracle stays in the main lab.
- `_GT_MARKERS_REPORT.txt` lists any remaining `GT: APPxx-Vnn` comment markers; strip them
  (`make-source-package.sh --strip-gt`) before handing the code to a SAST tool.

## Attention / Note
Le code reste **volontairement vulnérable** : à n'analyser qu'en environnement contrôlé, jamais à déployer.
The code is **deliberately vulnerable**: analyze in a controlled environment only, never deploy it.
