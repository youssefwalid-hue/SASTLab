module Api
  class ProfileController < BaseController
    # Liste blanche d'origines de confiance pour le partage de ressources cross-origin
    # (negative control APP08-N008, frere sur de APP08-V018).
    ALLOWED_ORIGINS = %w[https://app.cartavie.lab.exemple].freeze

    # GET /api/profile  (
    def show
      # 
      # REFLETEE telle quelle dans Access-Control-Allow-Origin ET les identifiants sont
      # autorises (Access-Control-Allow-Credentials: true). N'importe quel site tiers
      # peut donc lire la reponse authentifiee de la victime. Remediation : allowlist
      # stricte d'origines, jamais de reflet + credentials (cf. show_safe, NC N008).
      origin = request.headers["Origin"].to_s
      if origin.present?
        response.set_header("Access-Control-Allow-Origin", origin)
        response.set_header("Access-Control-Allow-Credentials", "true")
        response.set_header("Vary", "Origin")
      end
      render json: profile_payload
    end

    # GET /api/profile-safe  (// NC: APP08-N008 — CORS allowlist stricte)
    def show_safe
      # L'origine n'est reflete QUE si elle figure dans une liste blanche ; sinon aucun
      # en-tete Access-Control-Allow-Origin n'est emis (la reponse n'est pas lisible
      # cross-origin). Les credentials ne sont autorises que pour une origine permise.
      origin = request.headers["Origin"].to_s
      if ALLOWED_ORIGINS.include?(origin)
        response.set_header("Access-Control-Allow-Origin", origin)
        response.set_header("Access-Control-Allow-Credentials", "true")
      end
      response.set_header("Vary", "Origin")
      render json: profile_payload
    end

    private

    def profile_payload
      {
        username: current_client.username,
        email: current_client.email,
        role: current_client.role,
        cards: current_client.cards.count
      }
    end
  end
end
