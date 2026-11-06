# Schema de laboratoire cree au demarrage (profil standalone SQLite).
# Idempotent : ne cree les tables que si elles sont absentes.
module LabSchema
  module_function

  def ensure_schema!
    conn = ActiveRecord::Base.connection

    unless conn.table_exists?(:clients)
      conn.create_table :clients do |t|
        t.string  :username, null: false
        t.string  :email
        t.string  :password
        t.string  :role, null: false, default: "client"
        t.string  :api_key           # 
        t.timestamps
      end
      conn.add_index :clients, :username, unique: true
      conn.add_index :clients, :api_key
    end

    unless conn.table_exists?(:cards)
      conn.create_table :cards do |t|
        t.references :client, null: false
        t.string  :label
        t.string  :pan             # PAN complet fictif (ne doit jamais etre expose via l'API)
        t.string  :cvv             # CVV fictif
        t.integer :limit, default: 1000
        t.integer :available, default: 1000
        t.string  :status, default: "active"
        t.boolean :virtual, default: false
        t.string  :kind, default: "physical"
        t.boolean :notifications, default: true
        t.string  :note            # champ voisin echappe (negative control N005)
        t.timestamps
      end
    end

    unless conn.table_exists?(:disputes)
      conn.create_table :disputes do |t|
        t.references :card, null: false
        t.integer :amount, default: 0
        t.string  :idempotency_key
        t.string  :status, default: "open"
        t.timestamps
      end
    end

    unless conn.table_exists?(:api_keys)
      conn.create_table :api_keys do |t|
        t.references :client, null: false
        t.string   :token_digest, null: false
        t.datetime :expires_at
        t.boolean  :revoked, default: false
        t.timestamps
      end
    end
  end
end
