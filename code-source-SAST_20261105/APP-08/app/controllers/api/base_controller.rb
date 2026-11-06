module Api
  class BaseController < ActionController::API
    # API REST JSON authentifiee par cle (X-API-Key). Pas de session ni de CSRF ici
    # (comportement standard d'une API a cle, non vulnerable en soi).
    # Pas d'enveloppe de parametres : les clefs JSON du corps restent au niveau racine.
    wrap_parameters format: []

    before_action :authenticate_api!

    attr_reader :current_client

    private

    # 
    # STATIQUE (Client#api_key) accepte indefiniment. Aucune verification de TTL, de
    # rotation ni de revocation n'est effectuee ; une cle capturee reste valide pour
    # toujours. Remediation : cles hachees a duree de vie limitee + rotation/revocation
    # (cf. Api::KeysController#rotate, negative control N003).
    def authenticate_api!
      key = request.headers["X-API-Key"].to_s
      @current_client = Client.find_by(api_key: key) if key.present?
      return if @current_client

      render json: { error: "unauthorized" }, status: :unauthorized
    end
  end
end
