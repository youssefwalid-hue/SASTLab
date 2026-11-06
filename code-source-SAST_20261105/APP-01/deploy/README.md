# APP-01 — déploiement (redirection)

La chaîne de déploiement propre à APP-01 (scripts `00-config.sh` … `90-destroy.sh`, guides CloudShell/EB,
`AUDIT_DEPLOIEMENT.md`) a été **fusionnée en Phase B** dans la chaîne commune aux 10 applications :

- scripts : `deploy/` à la racine du package (`deploy/deploy.sh APP-01`, `deploy/stop.sh APP-01`, …) ;
- paramètres APP-01 : `deploy/apps/APP-01.env` ;
- runbook : `DEPLOYMENT_PLAYBOOK.md/.html`, exploitation : `OPERATIONS_GUIDE.md/.html` ;
- anciens fichiers conservés pour l'historique (mode ALB/ACM/RDS, obsolète) : `deploy/legacy/app-01/`.

Environnement de test — données 100 % fictives.
