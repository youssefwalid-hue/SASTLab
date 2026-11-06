# Client / gestionnaire du service cartes (donnees fictives).
# role: "client" ou "manager" (gestionnaire).
class Client < ActiveRecord::Base
  has_many :cards, dependent: :destroy
  has_many :api_keys, dependent: :destroy

  # Authentification web par formulaire. Mot de passe de lab en clair (donnees
  # fictives, usage defensif) : la comparaison est a temps constant malgre tout.
  def authenticate(candidate)
    ActiveSupport::SecurityUtils.secure_compare(password.to_s, candidate.to_s)
  end

  def manager?
    role == "manager"
  end
end
