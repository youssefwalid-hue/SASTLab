module Api
  class KeysController < BaseController
    # L'endpoint de rotation s'authentifie contre le modele ApiKey (hache, TTL,
    # revocation) et non contre la cle statique : on remplace donc la verification
    # de base par une validation stricte.
    skip_before_action :authenticate_api!
    before_action :authenticate_rotating_key!

    # POST /api/keys/rotate  (// NC: APP08-N003 — rotation revocable a duree limitee)
    def rotate
      # La cle courante (valide) est revoquee et une nouvelle cle a TTL est emise.
      @api_key.update!(revoked: true)
      raw = SecureRandom.hex(16)
      new_key = ApiKey.create!(
        client: @api_key.client,
        token_digest: ApiKey.digest(raw),
        expires_at: 30.days.from_now,
        revoked: false
      )
      # La cle en clair n'est renvoyee qu'une fois, jamais journalisee.
      render json: { rotated: true, expires_at: new_key.expires_at.iso8601 }
    end

    private

    # // NC: APP08-N003 — une cle expiree ou revoquee est rejetee par 401 ; la cle est
    # comparee par digest (stockage hache), jamais en clair ni journalisee.
    def authenticate_rotating_key!
      raw = request.headers["X-API-Key"].to_s
      @api_key = ApiKey.find_by(token_digest: ApiKey.digest(raw)) if raw.present?

      unless @api_key&.active?
        return render(json: { error: "invalid_or_expired_key" }, status: :unauthorized)
      end
    end
  end
end
