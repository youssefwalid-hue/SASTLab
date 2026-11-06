# 
# Le cookie _session est emis SANS les attributs de durcissement attendus :
#   - secure:    non positionne (transmis aussi en clair)
#   - httponly:  explicitement desactive (lisible par JavaScript)
#   - same_site: non positionne (envoye en contexte cross-site)
# Combine a un secret_key_base trivial (cf. environments/*.rb), la signature est
# derivable et la session falsifiable/volable.
#
# Remediation (hors lab) : key: "_cartavie_session", secure: true, httponly: true,
# same_site: :lax, et un SECRET_KEY_BASE robuste injecte par l'environnement.
Rails.application.config.session_store :cookie_store,
  key: "_session",
  secure: false,
  httponly: false,
  same_site: nil
