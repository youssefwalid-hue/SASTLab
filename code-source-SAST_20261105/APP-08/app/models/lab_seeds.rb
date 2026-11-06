# Donnees de demonstration 100% FICTIVES (aucun secret reel, aucune marque reelle).
# Comptes deterministes : client-a, client-b, gestionnaire (mot de passe Password1!).
# Comptes additionnels (volume) : client-c, client-d, gestionnaire-2.
# Cles d'API statiques : key-client-a-7f3a, key-client-b-9b2c (+ c/d/manager2 pour le volume).
module LabSeeds
  module_function

  PASSWORD = "Password1!".freeze

  def seed!
    ActiveRecord::Base.transaction do
      Dispute.delete_all
      ApiKey.delete_all
      Card.delete_all
      Client.delete_all

      client_a = Client.create!(id: 1, username: "client-a", email: "client-a@lab.exemple",
                                password: PASSWORD, role: "client", api_key: "key-client-a-7f3a")
      client_b = Client.create!(id: 2, username: "client-b", email: "client-b@lab.exemple",
                                password: PASSWORD, role: "client", api_key: "key-client-b-9b2c")
      Client.create!(id: 3, username: "gestionnaire", email: "gestionnaire@lab.exemple",
                     password: PASSWORD, role: "manager", api_key: "key-manager-3d1e")

      # Cartes fictives (PAN/CVV fictifs). Identifiants exacts attendus par le guide.
      Card.create!(id: 1001, client: client_a, label: "Carte perso",
                   pan: "4000123412341234", cvv: "123",
                   limit: 1000, available: 1000, status: "active",
                   virtual: false, kind: "physical", notifications: true, note: "")
      Card.create!(id: 1003, client: client_a, label: "Alpha secours",
                   pan: "4000111122223333", cvv: "456",
                   limit: 2000, available: 2000, status: "active",
                   virtual: false, kind: "physical", notifications: true, note: "")
      Card.create!(id: 1002, client: client_b, label: "Carte voyage",
                   pan: "4000987698769876", cvv: "789",
                   limit: 500, available: 500, status: "active",
                   virtual: true, kind: "virtual", notifications: true, note: "")

      # Cles d'API rotatives (modele securise, pour le negative control N003).
      ApiKey.create!(client: client_a, token_digest: ApiKey.digest("rot-live-a"),
                     expires_at: 30.days.from_now, revoked: false)
      ApiKey.create!(client: client_a, token_digest: ApiKey.digest("rot-old-a"),
                     expires_at: 1.day.ago, revoked: false)        # expiree
      ApiKey.create!(client: client_a, token_digest: ApiKey.digest("rot-revoked-a"),
                     expires_at: 30.days.from_now, revoked: true)  # revoquee

      # -----------------------------------------------------------------------
      # VOLUME DE DONNEES FICTIVES SUPPLEMENTAIRE (surface de crawl / variete).
      # N'IMPACTE PAS les identifiants deterministes attendus par le validateur :
      # clients 1..3, cartes 1001/1002/1003 et cles rotatives rot-* restent intacts.
      # Comptes et cartes additionnels : ids >= 4 / >= 1004. Donnees 100% fictives.
      # -----------------------------------------------------------------------
      extra_clients = [
        Client.create!(id: 4, username: "client-c", email: "client-c@lab.exemple",
                       password: PASSWORD, role: "client", api_key: "key-client-c-1a2b"),
        Client.create!(id: 5, username: "client-d", email: "client-d@lab.exemple",
                       password: PASSWORD, role: "client", api_key: "key-client-d-4c5d"),
        Client.create!(id: 6, username: "gestionnaire-2", email: "gestionnaire-2@lab.exemple",
                       password: PASSWORD, role: "manager", api_key: "key-manager2-6e7f"),
      ]
      client_c, client_d, _mgr2 = extra_clients

      extra_cards = [
        Card.create!(id: 1004, client: client_c, label: "Carte quotidienne",
                     pan: "4000222233334444", cvv: "321",
                     limit: 1500, available: 1500, status: "active",
                     virtual: false, kind: "physical", notifications: true, note: ""),
        Card.create!(id: 1005, client: client_c, label: "Carte e-commerce",
                     pan: "4000333344445555", cvv: "654",
                     limit: 800, available: 300, status: "active",
                     virtual: true, kind: "virtual", notifications: true, note: ""),
        Card.create!(id: 1006, client: client_c, label: "Ancienne carte",
                     pan: "4000444455556666", cvv: "987",
                     limit: 1000, available: 0, status: "blocked",
                     virtual: false, kind: "physical", notifications: false, note: ""),
        Card.create!(id: 1007, client: client_d, label: "Carte principale",
                     pan: "4000555566667777", cvv: "246",
                     limit: 3000, available: 2750, status: "active",
                     virtual: false, kind: "physical", notifications: true, note: ""),
        Card.create!(id: 1008, client: client_d, label: "Carte voyage pro",
                     pan: "4000666677778888", cvv: "135",
                     limit: 5000, available: 5000, status: "active",
                     virtual: false, kind: "physical", notifications: true, note: ""),
        Card.create!(id: 1009, client: client_d, label: "Carte jetable",
                     pan: "4000777788889999", cvv: "802",
                     limit: 200, available: 50, status: "blocked",
                     virtual: true, kind: "virtual", notifications: false, note: ""),
      ]

      # Historique de contestations/alimentations fictif (variete de statuts).
      Dispute.create!(card: extra_cards[0], amount: 120, status: "open")
      Dispute.create!(card: extra_cards[0], amount: 60,  status: "closed")
      Dispute.create!(card: extra_cards[1], amount: 200, idempotency_key: "seed-topup-c1",
                      status: "topup")
      Dispute.create!(card: extra_cards[3], amount: 90,  status: "closed")
      Dispute.create!(card: extra_cards[4], amount: 300, status: "open")

      # Cles rotatives additionnelles (modele securise) pour les comptes supplementaires.
      ApiKey.create!(client: client_c, token_digest: ApiKey.digest("rot-live-c"),
                     expires_at: 30.days.from_now, revoked: false)
      ApiKey.create!(client: client_d, token_digest: ApiKey.digest("rot-live-d"),
                     expires_at: 30.days.from_now, revoked: false)

      reset_sqlite_sequences
    end
  end

  # Recale les sequences SQLite pour que les prochains inserts n'entrent pas en
  # collision avec les ids explicites (1001..1003).
  def reset_sqlite_sequences
    conn = ActiveRecord::Base.connection
    return unless conn.adapter_name =~ /sqlite/i

    %w[clients cards disputes api_keys].each do |tbl|
      max = conn.select_value("SELECT COALESCE(MAX(id),0) FROM #{tbl}").to_i
      conn.execute("UPDATE sqlite_sequence SET seq=#{max} WHERE name='#{tbl}'")
    rescue ActiveRecord::StatementInvalid
      # sqlite_sequence peut ne pas exister tant qu'aucun AUTOINCREMENT n'a servi.
    end
  end
end
