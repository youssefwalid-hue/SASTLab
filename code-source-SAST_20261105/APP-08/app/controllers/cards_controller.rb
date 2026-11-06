require "net/http"
require "uri"
require "erb"

class CardsController < ApplicationController
  before_action :require_login, except: [:err]
  before_action :set_card, only: [:show, :edit, :update, :label, :note, :toggle, :rename, :dispute,
                                   :virtual_topup, :history, :embed, :embed_safe, :logo, :logo_safe]

  # 
  # explicitement desactivee sur l'action toggle (seule cette action). Combinee au
  # cookie _session sans SameSite (V011), une requete forgee cross-site aboutit.
  skip_before_action :verify_authenticity_token, only: [:toggle]

  # GET /cards?order=...
  def index
    scope = current_client.cards
    # 
    # enveloppee dans Arel.sql (contournant la protection ORDER BY de Rails) puis
    # interpolee dans la clause ORDER BY. Une expression SQL conditionnelle ou une
    # sonde temporelle est executee telle quelle.
    @cards =
      if params[:order].present?
        scope.order(Arel.sql(params[:order]))
      else
        scope.order(:id)
      end
    render :index
  end

  # GET /cards/search?filter=...  (// NC: APP08-N004 — liste blanche + requete sure)
  def search
    # Seules les colonnes et le sens autorises sont acceptes ; toute autre valeur
    # retombe sur le tri par defaut. Aucune interpolation de chaine utilisateur.
    col = Card::COLUMNS_SORTABLE.include?(params[:filter].to_s) ? params[:filter].to_s : "label"
    @cards = current_client.cards.order(col => :asc)
    render :index
  end

  # GET /cards/err  (
  def err
    raise "Erreur de demonstration CartaVie (page d'erreur Rails verbeuse)."
  end

  def show
    render :show
  end

  def edit
    render :edit
  end

  # PATCH /cards/:id  (
  def update
    # 
    # un attribut d'etat sensible qui ne devrait pas etre modifiable via ce formulaire,
    # permettant de reactiver une carte bloquee. Remediation : permit(:label) seul.
    @card.update(params.require(:card).permit(:label, :status))
    redirect_to card_path(@card)
  end

  # POST /cards/:id/label  (
  def label
    # Le libelle est stocke tel quel ; il est ensuite rendu SANS echappement
    # (html_safe) dans la liste des cartes (cf. app/views/cards/index.html.erb).
    @card.update_column(:label, params[:label].to_s)
    redirect_to cards_path
  end

  # POST /cards/:id/note  (// NC: APP08-N005 — note echappee par defaut)
  def note
    # Champ voisin du libelle : stocke puis rendu avec l'echappement ERB par defaut
    # (pas de html_safe). Aucune execution de charge HTML/JS.
    @card.update_column(:note, params[:note].to_s)
    redirect_to card_path(@card)
  end

  # POST /cards/:id/toggle  (
  def toggle
    @card.update_column(:status, @card.status == "active" ? "blocked" : "active")
    redirect_to card_path(@card)
  end

  # POST /cards/:id/rename  (// NC: APP08-N004 — action web protegee par CSRF)
  def rename
    # Protection CSRF ACTIVE (verify_authenticity_token non skippe ici) : une requete
    # forgee cross-site sans jeton valide est rejetee (ActionController::InvalidAuthenticityToken).
    @card.update_column(:label, params[:label].to_s)
    redirect_to card_path(@card)
  end

  # POST /cards/:id/dispute  (
  def dispute
    amount = params[:amount].to_i
    # 
    # cumule un recredit au disponible sans le lier a un debit reel unique ni plafonner
    # le cumul. L'enchainement fait depasser le plafond (available > limit).
    Dispute.create!(card: @card, amount: amount, status: "open")
    @card.update_column(:available, @card.available + amount)
    redirect_to card_path(@card)
  end

  # POST /cards/:id/virtual-topup  (// NC: APP08-N006 — alimentation bornee + idempotente)
  def virtual_topup
    amount = params[:amount].to_i
    key = params[:idempotency_key].to_s

    # Idempotence : une meme cle n'applique l'operation qu'une fois.
    if key.present? && Dispute.exists?(card_id: @card.id, idempotency_key: key)
      return redirect_to card_path(@card)
    end

    # Bornage : le disponible ne peut jamais depasser le plafond de la carte.
    new_available = [@card.available + amount, @card.limit].min
    new_available = [new_available, 0].max
    Dispute.create!(card: @card, amount: amount, idempotency_key: key.presence, status: "topup")
    @card.update_column(:available, new_available)
    redirect_to card_path(@card)
  end

  # ---------------------------------------------------------------------------
  # Use cases BENINS (surface de crawl, sans ecart ni NC). Auth exigee, scope
  # proprietaire, rendu echappe : aucun comportement vulnerable ici.
  # ---------------------------------------------------------------------------

  # GET /cards/export  (export JSON des cartes de l'appelant, vue masquee)
  def export
    cards = current_client.cards.order(:id).map(&:safe_api_hash)
    render json: { client: current_client.username, count: cards.size, cards: cards }
  end

  # GET /cards/stats  (synthese chiffree du portefeuille de l'appelant)
  def stats
    scope = current_client.cards
    @total = scope.count
    @active = scope.where(status: "active").count
    @blocked = scope.where(status: "blocked").count
    @available_sum = scope.sum(:available)
    render :stats
  end

  # GET /cards/:id/history  (historique des contestations de la carte, scope)
  def history
    @disputes = @card.disputes.order(created_at: :desc)
    render :history
  end

  # GET /cards/lookup?q=...  (
  def lookup
    # Le terme de recherche est repris tel quel dans la page de resultats et rendu
    # SANS echappement (raw) : une charge HTML/JS fournie dans q s'execute au rendu.
    # Variante REFLECHIE (distincte du XSS stocke APP08-V007 sur le libelle).
    @q = params[:q].to_s
    @results = current_client.cards.where("label LIKE ?", "%#{@q}%").order(:id)
    render :lookup
  end

  # GET /cards/lookup-safe?q=...  (// NC: APP08-N007 — echo echappe)
  def lookup_safe
    # Meme fonction de recherche, mais le terme est rendu avec l'echappement ERB par
    # defaut (pas de raw) : une charge HTML/JS est affichee comme texte inerte.
    @q = params[:q].to_s
    @results = current_client.cards.where("label LIKE ?", "%#{@q}%").order(:id)
    render :lookup_safe
  end

  # POST /cards/preview  (
  def preview
    # 
    # l'utilisateur est compile et EVALUE par ERB (ERB.new(...).result). Toute expression
    # Ruby entre <%= %> est executee cote serveur. Remediation : ne jamais evaluer une
    # entree comme gabarit ; rendre le texte echappe (cf. preview_safe, NC N011).
    rendered = ERB.new(params[:template].to_s).result(binding)
    render html: ("Apercu : " + rendered).html_safe
  end

  # POST /cards/preview-safe  (// NC: APP08-N011 — apercu inerte)
  def preview_safe
    # Le gabarit n'est JAMAIS evalue : il est affiche echappe (texte inerte).
    safe = ERB::Util.html_escape(params[:template].to_s)
    render html: ("Apercu : " + safe).html_safe
  end

  # GET /cards/:id/embed  (
  def embed
    # 
    # anti-cadrage n'est emis (ni X-Frame-Options, ni CSP frame-ancestors). La page
    # peut etre placee dans une <iframe> tierce et superposee (clickjacking).
    # (Distinct de l'absence globale d'en-tetes V014 : ici la cible est une page
    # d'action sensible dont le cadrage doit etre explicitement interdit.)
    render :embed, layout: false
  end

  # GET /cards/:id/embed-safe  (// NC: APP08-N009 — cadrage interdit)
  def embed_safe
    # Meme widget, mais le cadrage est explicitement interdit : X-Frame-Options: DENY
    # et Content-Security-Policy: frame-ancestors 'none'.
    response.set_header("X-Frame-Options", "DENY")
    response.set_header("Content-Security-Policy", "frame-ancestors 'none'")
    render :embed_safe, layout: false
  end

  # POST /cards/:id/logo  (
  def logo
    # 
    # serveur SANS validation de schema, d'hote ni de plage d'adresses. Le serveur
    # emet donc une requete vers une cible arbitraire choisie par l'appelant
    # (y compris une adresse interne/loopback ou un service de metadonnees cloud).
    uri = URI.parse(params[:url].to_s)
    resp = Net::HTTP.get_response(uri)
    render json: { fetched: true, status: resp.code.to_i,
                   bytes: resp.body.to_s.bytesize, host: uri.host }
  rescue => e
    render json: { fetched: false, error: e.class.name }, status: :unprocessable_entity
  end

  # POST /cards/:id/logo-safe  (// NC: APP08-N012 — allowlist d'hotes)
  def logo_safe
    # Recuperation UNIQUEMENT si la cible est HTTP(S) ET son hote figure dans une
    # liste blanche de CDN de confiance ; toute autre cible (interne, loopback,
    # metadonnees) est refusee avant toute requete reseau.
    uri = URI.parse(params[:url].to_s)
    unless uri.is_a?(URI::HTTP) && Card::ALLOWED_LOGO_HOSTS.include?(uri.host)
      return render(json: { fetched: false, error: "host_not_allowed" }, status: :unprocessable_entity)
    end
    resp = Net::HTTP.get_response(uri)
    render json: { fetched: true, status: resp.code.to_i, host: uri.host }
  rescue => e
    render json: { fetched: false, error: e.class.name }, status: :unprocessable_entity
  end

  private

  def set_card
    # Securise par defaut cote web : recherche SCOPEE au proprietaire (pas d'IDOR ici ;
    # l'IDOR volontaire est localise cote API, cf. Api::CardsController#show / #block).
    @card = current_client.cards.find_by(id: params[:id])
    redirect_to cards_path, alert: "Carte introuvable." if @card.nil?
  end
end
