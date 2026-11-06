# Filtrage des parametres sensibles dans les journaux (securise par defaut).
# NB : la clef d'API (X-API-Key) est un en-tete ; la faiblesse V003 tient a son
# absence d'expiration/rotation, pas a la journalisation (filtree ici par prudence).
Rails.application.config.filter_parameters += %i[
  passw secret token _key crypt salt certificate otp ssn cvv pan
]
