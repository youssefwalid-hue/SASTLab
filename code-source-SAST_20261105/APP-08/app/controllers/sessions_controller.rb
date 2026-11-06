class SessionsController < ApplicationController
  # Le formulaire de connexion accepte un POST sans jeton CSRF : un client non authentifie
  # (et le validateur) POSTe /login sans token, et la protection CSRF n'a pas de sens avant
  # l'etablissement de la session. Les actions AUTHENTIFIEES restent protegees par defaut
  # (seule CardsController#toggle est volontairement exemptee, cf. GT APP08-V010).
  skip_before_action :verify_authenticity_token, only: [:create, :destroy]

  def new
    # Le parametre redirect est conserve pour l'apres-connexion.
    @redirect = params[:redirect]
    render :new
  end

  def create
    client = Client.find_by(username: params[:username].to_s)

    if client && client.authenticate(params[:password])
      reset_session
      session[:client_id] = client.id
      # 
      # quel, sans validation de domaine ni restriction aux chemins relatifs internes.
      # Un deploiement durci n'accepterait qu'un chemin relatif interne ou une allowlist.
      target = params[:redirect].presence || "/cards"
      redirect_to target, allow_other_host: true
    else
      flash.now[:alert] = "Identifiants invalides."
      @redirect = params[:redirect]
      render :new, status: :unauthorized
    end
  end

  def destroy
    reset_session
    redirect_to "/login"
  end
end
