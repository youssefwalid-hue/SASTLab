# APP-10 OnboardR — reverse proxy Nginx dedie (termine le TLS sur l'instance, EB Single Instance).
FROM nginx:1.27-alpine
# openssl : certificat autosigne genere au demarrage (CN=$TLS_CN).
RUN apk add --no-cache openssl
# Config du proxy (porte les ecarts de couche Reverse Proxy du corpus ; servie en 443, redirection 80->443).
COPY nginx.conf /etc/nginx/nginx.conf
# Racine statique dediee (/static/) + ressources frere correctement ancrees (/assets/).
COPY static/ /usr/share/nginx/html/static/
COPY assets/ /usr/share/nginx/html/assets/
# Fichier situe HORS de la racine statique (cible de la traversee de chemin V015).
COPY outside-static-note.txt /usr/share/nginx/html/outside-static-note.txt
# Entrypoint : genere le certificat autosigne puis lance nginx.
COPY docker-entrypoint-tls.sh /docker-entrypoint-tls.sh
RUN chmod +x /docker-entrypoint-tls.sh && mkdir -p /var/cache/nginx/labcache /etc/nginx/certs
EXPOSE 80 443
ENTRYPOINT ["/docker-entrypoint-tls.sh"]
