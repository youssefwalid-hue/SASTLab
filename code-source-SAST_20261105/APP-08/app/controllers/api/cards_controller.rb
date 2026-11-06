module Api
  class CardsController < BaseController
    # GET /api/cards  (
    # realisee dans BaseController ; la liste elle-meme est scopee au proprietaire.)
    def index
      cards = current_client.cards.order(:id).map(&:safe_api_hash)
      render json: { cards: cards }
    end

    # GET /api/cards/:id  (
    def show
      # 
      # propriete (pas de current_client.cards.find). Un client lit la carte d'autrui.
      card = Card.find_by(id: params[:id])
      return render(json: { error: "not_found" }, status: :not_found) if card.nil?

      # 
      # attributs du modele, dont le PAN complet et le CVV, au lieu d'une vue filtree.
      render json: card.attributes.merge(
        "pan_masked" => card.pan_masked,
        "holder" => card.client&.username
      )
    end

    # PUT /api/cards/:id  (
    def update
      card = current_client.cards.find_by(id: params[:id])
      return render(json: { error: "not_found" }, status: :not_found) if card.nil?

      # 
      # qui devrait relever d'un workflow d'autorisation. Le plafond est modifie sans
      # controle. Remediation : params.permit(:label) et endpoint dedie pour le plafond.
      card.update(params.permit(:label, :limit))
      render json: card.safe_api_hash
    end

    # POST /api/cards/:id/block  (
    def block
      # 
      # appartient a l'appelant : seul le fait d'etre authentifie est controle.
      card = Card.find_by(id: params[:id])
      return render(json: { error: "not_found" }, status: :not_found) if card.nil?

      card.update_column(:status, "blocked")
      render json: { id: card.id, status: card.status }
    end

    # PUT /api/cards/:id/preferences  (// NC: APP08-N001 — strong params stricts)
    def preferences
      card = current_client.cards.find_by(id: params[:id])
      return render(json: { error: "not_found" }, status: :not_found) if card.nil?

      # Liste blanche STRICTE : seul :notifications est accepte. Les champs limit et
      # status soumis sont ignores (la ressource reste inchangee sur ces champs).
      card.update(params.permit(:notifications))
      render json: card.safe_api_hash.merge(notifications: card.notifications)
    end

    # GET /api/cards/:id/summary  (// NC: APP08-N002 — scope proprietaire + PAN masque)
    def summary
      # Recherche SCOPEE : 404 hors perimetre (pas d'IDOR). Vue filtree : jamais de
      # PAN complet ni de CVV, uniquement pan_masked et le statut.
      card = current_client.cards.find_by(id: params[:id])
      return render(json: { error: "not_found" }, status: :not_found) if card.nil?

      render json: {
        id: card.id,
        pan_masked: card.pan_masked,
        status: card.status,
        limit: card.limit
      }
    end
  end
end
