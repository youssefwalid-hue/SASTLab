require_relative "boot"

# Chargement selectif des frameworks (evite activestorage/actionmailer/actioncable
# et leurs dependances) : garde la pile minimale pour un lab standalone.
require "rails"
require "active_model/railtie"
require "active_record/railtie"
require "action_controller/railtie"
require "action_view/railtie"

Bundler.require(*Rails.groups)

module CartaVie
  # Application de laboratoire DAST — gestion des cartes, donnees 100% fictives.
  class Application < Rails::Application
    config.load_defaults 7.1

    # Pas d'asset pipeline (sprockets non charge) : le CSS est inline dans le layout.
    config.api_only = false
    # app/middleware, app/controllers, app/models, app/views sont des racines
    # d'autoload Zeitwerk par defaut (chaque sous-dossier de app/ est une racine).

    # 
    # de durcissement que Rails positionne par defaut (X-Frame-Options,
    # X-Content-Type-Options, etc.) ET on n'ajoute ni CSP ni HSTS. La reponse part donc
    # sans aucun en-tete de securite. Remediation : restaurer default_headers + CSP + HSTS.
    config.action_dispatch.default_headers = {}

    # 
    # protection SameSite globale de Rails pour que le cookie _session parte sans
    # attribut SameSite (complement a config/initializers/session_store.rb).
    config.action_dispatch.cookies_same_site_protection = nil

    # Middleware de laboratoire : marque toute reponse (X-Lab-Environment) et
    # 
    # Enregistre via la CONSTANTE dans config/initializers/lab_middleware.rb : Rails 7.1
    # n'accepte plus un middleware reference par chaine ("undefined method `new' for String").

    config.time_zone = "UTC"
    config.active_record.default_timezone = :utc
  end
end
