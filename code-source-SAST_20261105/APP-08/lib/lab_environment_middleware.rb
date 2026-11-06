# Middleware de laboratoire CartaVie (déplacé de app/middleware vers lib/ : chargé
# explicitement par config/initializers/lab_middleware.rb via require, car l'autoload
# Zeitwerk ne résout pas encore les constantes de app/ au moment où l'initializer tourne).
# - Marque TOUTES les reponses avec X-Lab-Environment: test-data-fictives (exigence lab).
# - 
#   le serveur applicatif et sa version, et laisse Puma annoncer sa banniere Server par defaut.
#   (Un deploiement durci neutraliserait Server et supprimerait X-Powered-By.)
#
# NOTE — 
# securite (CSP, X-Content-Type-Options, X-Frame-Options, HSTS) : leur absence est
# l'ecart V014. Tout le reste de l'application reste securise par defaut.
class LabEnvironmentMiddleware
  def initialize(app)
    @app = app
  end

  def call(env)
    status, headers, response = @app.call(env)
    headers["X-Lab-Environment"] = "test-data-fictives"
    # 
    headers["X-Powered-By"] = "Puma 6.4 (Rails #{Rails::VERSION::STRING})"
    [status, headers, response]
  end
end
