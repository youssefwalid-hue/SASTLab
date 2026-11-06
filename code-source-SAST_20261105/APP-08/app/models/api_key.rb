# Cle d'API REST a duree de vie limitee et revocable (modele securise, utilise par
# l'endpoint frère de rotation — negative control APP08-N003).
#
# ATTENTION : la cle d'API statique utilisee par l'authentification REST principale
# (
# Ce modele-ci represente au contraire la bonne pratique : hachage, expiration, revocation.
class ApiKey < ActiveRecord::Base
  belongs_to :client

  def self.digest(raw)
    Digest::SHA256.hexdigest("cartavie:#{raw}")
  end

  # Cle valide = non revoquee ET non expiree (comportement attendu / securise).
  def active?
    !revoked && (expires_at.nil? || expires_at > Time.current)
  end
end
