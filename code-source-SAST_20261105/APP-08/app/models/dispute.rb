# Contestation d'operation ouverte par un client.
# Un recredit provisoire est applique en attendant l'instruction par le gestionnaire.
class Dispute < ActiveRecord::Base
  belongs_to :card
end
