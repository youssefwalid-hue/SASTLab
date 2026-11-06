# Amorçage de la base standalone au démarrage (profil lab, SQLite).
# Cree le schema s'il manque, puis seed les donnees 100% FICTIVES.
# Le reseed a la demande se fait par POST /admin/reset (cf. AdminController).
#
# Ce bloc s'execute apres l'initialisation ; il est idempotent et sans effet si
# le schema et les donnees sont deja presents.
Rails.application.config.after_initialize do
  begin
    LabSchema.ensure_schema!
    LabSeeds.seed! if Client.count.zero?
  rescue ActiveRecord::NoDatabaseError
    # Cree le fichier SQLite puis reessaie une fois.
    ActiveRecord::Tasks::DatabaseTasks.create_current
    LabSchema.ensure_schema!
    LabSeeds.seed! if Client.count.zero?
  rescue => e
    Rails.logger.warn("[bootstrap_db] amorçage differe : #{e.class}: #{e.message}")
  end
end
