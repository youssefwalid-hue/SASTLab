#!/usr/bin/env bash
# =============================================================================
# APP-10 OnboardR — hook prebuild (plateforme Docker AL2023).
# Récupère SUR L'HÔTE le certificat valide (Let's Encrypt wildcard *.<LAB_DOMAIN>) depuis SSM Parameter
# Store (${TLS_SSM_PREFIX}/{fullchain,privkey}, SecureString) et l'écrit dans /etc/dastlab-tls, monté en
# LECTURE SEULE dans le conteneur proxy (docker-compose → /host-tls). L'entrypoint du proxy l'utilise si
# présent et valide, sinon il génère un autosigné.
#   - mode letsencrypt : fetch + validation (couvre l'hôte, non expiré, clé cohérente) → /etc/dastlab-tls.
#   - mode selfsigned / échec (IAM, SSM, cert invalide) : on RETIRE les fichiers → le proxy autosigne.
# Ne casse jamais le déploiement (exit 0). Environnement de test — données fictives — usage défensif.
# =============================================================================
set -uo pipefail
OUT=/etc/dastlab-tls
cfg() { /opt/elasticbeanstalk/bin/get-config environment -k "$1" 2>/dev/null || true; }
MODE="$(cfg TLS_CERT_MODE)"; MODE="${MODE:-selfsigned}"
PREFIX="$(cfg TLS_SSM_PREFIX)"; PREFIX="${PREFIX:-/dastlab/tls}"
REGION="$(cfg TLS_SSM_REGION)"
CN="$(cfg TLS_CN)"
mkdir -p "$OUT"; chmod 700 "$OUT"
rm -f "$OUT/dastlab.crt" "$OUT/dastlab.key"

if [ "$MODE" != "letsencrypt" ]; then
  echo "[tls] mode ${MODE} : le proxy utilisera un certificat autosigné"; exit 0
fi

# Région via IMDSv2 si non fournie par la config EB.
if [ -z "$REGION" ]; then
  TOKEN="$(curl -s -X PUT 'http://169.254.169.254/latest/api/token' -H 'X-aws-ec2-metadata-token-ttl-seconds: 60' || true)"
  REGION="$(curl -s -H "X-aws-ec2-metadata-token: $TOKEN" http://169.254.169.254/latest/meta-data/placement/region || true)"
fi

# L'AWS CLI n'est pas garantie sur l'hôte Docker AL2023 : l'installer au besoin.
if ! command -v aws >/dev/null 2>&1; then
  echo "[tls] AWS CLI absente → installation…"
  dnf install -y -q awscli-2 >/dev/null 2>&1 || dnf install -y -q awscli >/dev/null 2>&1 || pip3 install -q awscli >/dev/null 2>&1 || true
  export PATH="/usr/local/bin:/usr/bin:$PATH"
fi

T="$(mktemp -d)"; trap 'rm -rf "$T"' EXIT
if command -v aws >/dev/null 2>&1 \
 && aws ssm get-parameter --region "$REGION" --name "${PREFIX}/fullchain" --with-decryption --query Parameter.Value --output text > "$T/crt" 2>/dev/null \
 && aws ssm get-parameter --region "$REGION" --name "${PREFIX}/privkey"  --with-decryption --query Parameter.Value --output text > "$T/key" 2>/dev/null \
 && [ -s "$T/crt" ] && [ -s "$T/key" ] \
 && openssl x509 -in "$T/crt" -noout >/dev/null 2>&1 \
 && openssl x509 -in "$T/crt" -noout -checkend 86400 >/dev/null 2>&1 \
 && { [ -z "$CN" ] || openssl x509 -in "$T/crt" -noout -checkhost "$CN" 2>/dev/null | grep -q "does match"; }; then
  c="$(openssl x509 -in "$T/crt" -noout -pubkey 2>/dev/null | openssl sha256)"
  k="$(openssl pkey -in "$T/key" -pubout 2>/dev/null | openssl sha256)"
  if [ -n "$c" ] && [ "$c" = "$k" ]; then
    install -m 644 "$T/crt" "$OUT/dastlab.crt"
    install -m 600 "$T/key" "$OUT/dastlab.key"
    echo "[tls] certificat Let's Encrypt récupéré depuis SSM ${PREFIX} pour ${CN:-(hôte)} → ${OUT}"
    exit 0
  fi
fi
echo "[tls] AVERTISSEMENT : certificat SSM indisponible/invalide → le proxy autosignera (REPLI)"
exit 0
