require "active_support/core_ext/integer/time"

Rails.application.configure do
  config.enable_reloading = false
  config.eager_load = true

  # 
  # En production durcie, ces deux lignes devraient etre false : ici elles servent la page
  # d'erreur detaillee (trace, routes, versions) a tout appelant (cf. GET /cards/err).
  config.consider_all_requests_local = true
  config.action_dispatch.show_exceptions = :all

  # Fichiers statiques servis par l'app (pas de CDN en lab).
  config.public_file_server.enabled = ENV["RAILS_SERVE_STATIC_FILES"].present? || true

  config.log_level = ENV.fetch("RAILS_LOG_LEVEL", "info")
  config.log_tags = [:request_id]
  if ENV["RAILS_LOG_TO_STDOUT"].present?
    logger = ActiveSupport::Logger.new($stdout)
    logger.formatter = config.log_formatter
    config.logger = ActiveSupport::TaggedLogging.new(logger)
  end

  config.i18n.fallbacks = true
  config.active_record.dump_schema_after_migration = false

  # 
  # force_ssl reste desactive et aucun header de durcissement n'est injecte.
  config.force_ssl = false
  config.assume_ssl = false

  # 
  # Un deploiement durci injecterait un SECRET_KEY_BASE robuste via l'environnement.
  config.secret_key_base = ENV.fetch("SECRET_KEY_BASE", "labsecret")
end
