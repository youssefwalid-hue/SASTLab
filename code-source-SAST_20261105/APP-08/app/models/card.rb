# Carte bancaire fictive (physique ou virtuelle).
# Champs : label, pan (complet, fictif), cvv (fictif), limit (plafond mensuel),
# available (disponible), status (active|blocked), virtual (bool), kind.
class Card < ActiveRecord::Base
  belongs_to :client
  has_many :disputes, dependent: :destroy

  COLUMNS_SORTABLE = %w[label created_at limit status].freeze

  # Liste blanche d'hotes de confiance pour la recuperation d'un logo de carte
  # (negative control APP08-N012, frere sur de APP08-V022/SSRF).
  ALLOWED_LOGO_HOSTS = %w[cdn.cartavie.lab.exemple].freeze

  # PAN masque : seuls les 4 derniers chiffres (vue securisee par defaut).
  def pan_masked
    digits = pan.to_s.gsub(/\D/, "")
    last4 = digits[-4..] || digits
    "**** **** **** #{last4}"
  end

  # Representation API SECURISEE (serializer a liste blanche) — utilisee par les
  # endpoints frères (negative controls) : jamais de PAN complet ni de CVV.
  def safe_api_hash
    {
      id: id,
      label: label,
      pan_masked: pan_masked,
      limit: limit,
      available: available,
      status: status,
      virtual: virtual
    }
  end
end
