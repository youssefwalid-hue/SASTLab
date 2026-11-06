#!/usr/bin/env bash
# Cree le repertoire des releves fictifs utilise par l'application (profil prod).
set -euo pipefail
mkdir -p /var/app/data/statements
chown -R webapp:webapp /var/app/data || true
