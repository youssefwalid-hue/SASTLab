#!/bin/sh
# APP-10 OnboardR — entrypoint du proxy. TLS termine DANS le conteneur (EB Single Instance, sans ALB).
#   - Si un certificat VALIDE est monte depuis l'hote en lecture seule dans /host-tls (recupere depuis SSM
#     par le hook prebuild .platform/hooks/prebuild/00-tls-fetch.sh en mode letsencrypt), on l'utilise :
#     certificat Let's Encrypt wildcard *.<LAB_DOMAIN>, reconnu nativement par les navigateurs.
#   - Sinon (mode selfsigned, ou cert hote absent/expire) : generation d'un autosigne CN=$TLS_CN.
# Dans les deux cas nginx lit /etc/nginx/certs/dastlab.{crt,key} (nginx.conf inchange). Donnees fictives.
set -e
CERT_DIR=/etc/nginx/certs
HOST_DIR=/host-tls
CN="${TLS_CN:-onboardr.lab.exemple}"
mkdir -p "$CERT_DIR"

used_host=0
if [ -s "$HOST_DIR/dastlab.crt" ] && [ -s "$HOST_DIR/dastlab.key" ]; then
  if openssl x509 -in "$HOST_DIR/dastlab.crt" -noout -checkend 86400 >/dev/null 2>&1; then
    cp "$HOST_DIR/dastlab.crt" "$CERT_DIR/dastlab.crt"
    cp "$HOST_DIR/dastlab.key" "$CERT_DIR/dastlab.key"
    chmod 644 "$CERT_DIR/dastlab.crt"; chmod 600 "$CERT_DIR/dastlab.key"
    used_host=1
    echo "[tls] certificat Let's Encrypt monte depuis l'hote installe"
  else
    echo "[tls] AVERTISSEMENT : certificat hote present mais expire/illisible -> autosigne"
  fi
fi

if [ "$used_host" = "0" ]; then
  echo "[tls] generation du certificat autosigne CN=${CN} (365 j, 2048 bits)"
  openssl req -x509 -newkey rsa:2048 -nodes -days 365 \
    -keyout "$CERT_DIR/dastlab.key" -out "$CERT_DIR/dastlab.crt" \
    -subj "/CN=${CN}/O=Banking DAST Benchmark Lab/OU=Environnement de test - donnees fictives" \
    -addext "subjectAltName=DNS:${CN}" 2>/dev/null
fi

# Garde d'hote canonique : en mode letsencrypt, rediriger l'ancienne URL EB
# (*.elasticbeanstalk.com) vers l'hote canonique $CN (= app10.<LAB_DOMAIN>, couvert par le cert valide).
# En selfsigned, laisser le dossier vide (include /etc/nginx/guard/*.conf tolere l'absence de fichier).
GUARD_DIR=/etc/nginx/guard
mkdir -p "$GUARD_DIR"
rm -f "${GUARD_DIR:?}"/*.conf 2>/dev/null || true
if [ "${TLS_CERT_MODE:-selfsigned}" = "letsencrypt" ]; then
  cat > "$GUARD_DIR/zz-canonical-guard.conf" <<EOF
server {
    listen 443 ssl;
    server_name ~\.elasticbeanstalk\.com\$;
    ssl_certificate     /etc/nginx/certs/dastlab.crt;
    ssl_certificate_key /etc/nginx/certs/dastlab.key;
    ssl_protocols       TLSv1.2 TLSv1.3;
    return 301 https://${CN}\$request_uri;
}
EOF
  echo "[guard] mode letsencrypt : ancienne URL EB -> https://${CN}"
else
  echo "[guard] mode ${TLS_CERT_MODE:-selfsigned} : garde d'hote canonique desactivee"
fi

exec nginx -g 'daemon off;'
