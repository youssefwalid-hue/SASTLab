Rails.application.routes.draw do
  root "home#index"

  # --- Authentification web (formulaire + session) ---------------------------
  get    "/login",  to: "sessions#new"
  post   "/login",  to: "sessions#create"    # 
  delete "/logout", to: "sessions#destroy"
  post   "/logout", to: "sessions#destroy"

  # --- Cartes (interface web ERB, session) -----------------------------------
  # Routes litterales AVANT /cards/:id pour eviter la capture par :id.
  get  "/cards",                 to: "cards#index"          # 
  get  "/cards/search",          to: "cards#search"         # // NC: APP08-N004 (param filter)
  get  "/cards/err",             to: "cards#err"            # 
  # Use cases benins (surface de crawl, auth exigee, rendu echappe).
  get  "/cards/export",          to: "cards#export"         # use case benin (export JSON scope)
  get  "/cards/stats",           to: "cards#stats"          # use case benin (synthese chiffree)
  # Recherche/apercu litteraux AVANT /cards/:id.
  get  "/cards/lookup",          to: "cards#lookup"         # 
  get  "/cards/lookup-safe",     to: "cards#lookup_safe"    # // NC: APP08-N007 (param q echappe)
  post "/cards/preview",         to: "cards#preview"        # 
  post "/cards/preview-safe",    to: "cards#preview_safe"   # // NC: APP08-N011 (template inerte)
  get  "/cards/:id",             to: "cards#show",   as: :card
  get  "/cards/:id/edit",        to: "cards#edit",   as: :edit_card
  patch "/cards/:id",            to: "cards#update"         # 
  post "/cards/:id/label",       to: "cards#label"          # 
  post "/cards/:id/note",        to: "cards#note"           # // NC: APP08-N005 (param note)
  post "/cards/:id/toggle",      to: "cards#toggle"         # 
  post "/cards/:id/rename",      to: "cards#rename"         # // NC: APP08-N004 (CSRF on)
  post "/cards/:id/dispute",     to: "cards#dispute"        # 
  post "/cards/:id/virtual-topup", to: "cards#virtual_topup" # // NC: APP08-N006 (param amount)
  get  "/cards/:id/history",     to: "cards#history",  as: :card_history # use case benin (scope)
  get  "/cards/:id/embed",       to: "cards#embed"          # 
  get  "/cards/:id/embed-safe",  to: "cards#embed_safe"     # // NC: APP08-N009 (X-Frame-Options)
  post "/cards/:id/logo",        to: "cards#logo"           # 
  post "/cards/:id/logo-safe",   to: "cards#logo_safe"      # // NC: APP08-N012 (allowlist d'hotes)

  # --- Espace compte (use cases benins) --------------------------------------
  get  "/profile",       to: "home#profile"        # use case benin (profil, echappe)
  get  "/notifications", to: "home#notifications"  # use case benin (liste paginee)
  get  "/help",          to: "home#help"           # use case benin (aide/a-propos, public)

  # --- Reinitialisation de mot de passe (pre-auth) ---------------------------
  post "/password/reset",      to: "passwords#reset"       # 
  post "/password/reset-safe", to: "passwords#reset_safe"  # // NC: APP08-N010 (reponse uniforme)

  # --- API REST JSON (cle d'API X-API-Key) -----------------------------------
  namespace :api do
    get  "/cards",               to: "cards#index"          # 
    put  "/cards/:id",           to: "cards#update"         # 
    get  "/cards/:id",           to: "cards#show"           # 
    post "/cards/:id/block",     to: "cards#block"          # 
    put  "/cards/:id/preferences", to: "cards#preferences"  # // NC: APP08-N001 (param notifications)
    get  "/cards/:id/summary",   to: "cards#summary"        # // NC: APP08-N002 (scoped + masque)
    post "/keys/rotate",         to: "keys#rotate"          # // NC: APP08-N003 (TTL/revocation)
    get  "/profile",             to: "profile#show"         # 
    get  "/profile-safe",        to: "profile#show_safe"    # // NC: APP08-N008 (CORS allowlist)
  end

  # --- Administration de laboratoire -----------------------------------------
  post "/admin/reset", to: "admin#reset"

  # 
  # en production) ; derriere le proxy, atteignable via normalisation de chemin.
  get "/rails/info/properties", to: "rails/info#properties"
  get "/rails/info",            to: "rails/info#index"
end
