class HomeController < ApplicationController
  before_action :require_login, only: [:profile, :notifications]

  # Page d'accueil publique (bandeau de lab). Observable pour les controles
  # passifs d'en-tetes : 
  def index
    render :index
  end

  # GET /profile  (use case benin : profil de l'appelant, rendu echappe)
  def profile
    @client = current_client
    render :profile
  end

  # GET /notifications  (use case benin : liste simple, rendu echappe)
  def notifications
    @items = [
      { at: "2026-01-04", text: "Bienvenue sur CartaVie (environnement de test)." },
      { at: "2026-01-05", text: "Votre carte « #{current_client.cards.first&.label} » est active." },
      { at: "2026-01-06", text: "Aucune contestation en attente." }
    ]
    render :notifications
  end

  # GET /help  (use case benin : aide / a-propos, public)
  def help
    render :help
  end
end
