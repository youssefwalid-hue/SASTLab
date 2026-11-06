class PasswordsController < ApplicationController
  # Reinitialisation de mot de passe : flux PRE-authentification. Comme pour le
  # formulaire de connexion (cf. SessionsController), le POST est accepte sans jeton
  # CSRF — un appelant non authentifie (et le validateur) le soumet sans token, et la
  # protection CSRF n'a pas de sens avant l'etablissement d'une session.
  skip_before_action :verify_authenticity_token, only: [:reset, :reset_safe]

  # POST /password/reset  (
  def reset
    # 
    # distinct et statut HTTP distinct (200 vs 404). Un attaquant distingue ainsi les
    # identifiants valides des invalides (enumeration de comptes via la reinitialisation).
    # Remediation : reponse et statut UNIFORMES quelle que soit l'existence (cf. reset_safe).
    client = Client.find_by(username: params[:username].to_s)
    if client
      render plain: "Un e-mail de reinitialisation a ete envoye a #{client.email}.", status: :ok
    else
      render plain: "Aucun compte ne correspond a cet identifiant.", status: :not_found
    end
  end

  # POST /password/reset-safe  (// NC: APP08-N010 — reponse uniforme)
  def reset_safe
    # La recherche est effectuee mais la reponse est IDENTIQUE (meme texte, meme statut
    # 200) que le compte existe ou non : aucune information d'existence ne fuite.
    Client.find_by(username: params[:username].to_s)
    render plain: "Si un compte correspond, un e-mail de reinitialisation a ete envoye.", status: :ok
  end
end
