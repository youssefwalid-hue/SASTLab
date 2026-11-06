class AdminController < ActionController::Base
  # Reinitialisation de l'etat de demonstration (lab). Pas de CSRF : appelee par
  # le validateur de benchmark entre chaque cas. Ne manipule que des donnees fictives.
  skip_forgery_protection

  # POST /admin/reset
  def reset
    LabSchema.ensure_schema!
    LabSeeds.seed!
    render json: { reset: true, at: Time.current.utc.iso8601 }
  end
end
