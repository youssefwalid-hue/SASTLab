require "active_support/core_ext/integer/time"

Rails.application.configure do
  config.enable_reloading = false
  config.eager_load = false
  config.public_file_server.enabled = true
  config.consider_all_requests_local = true
  config.action_dispatch.show_exceptions = :none
  config.action_controller.allow_forgery_protection = false
  config.active_support.deprecation = :stderr
  config.secret_key_base = ENV.fetch("SECRET_KEY_BASE", "labsecret")
end
