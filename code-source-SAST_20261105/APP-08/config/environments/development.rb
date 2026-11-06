require "active_support/core_ext/integer/time"

Rails.application.configure do
  config.enable_reloading = true
  config.eager_load = false

  # Pages d'erreur detaillees (comportement standard en developpement ;
  # en production l'ecart V012 maintient volontairement ce comportement).
  config.consider_all_requests_local = true
  config.action_dispatch.show_exceptions = :all

  config.public_file_server.enabled = true
  config.active_record.migration_error = :page_load
  config.active_record.verbose_query_logs = true

  config.secret_key_base = ENV.fetch("SECRET_KEY_BASE", "labsecret")
end
