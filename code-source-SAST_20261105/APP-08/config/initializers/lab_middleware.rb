# Insère le middleware de laboratoire via la CONSTANTE (Rails 7.1 n'accepte plus un
# middleware référencé par chaîne). La classe vit dans lib/ (hors autoload) et est chargée
# explicitement ici : l'autoload Zeitwerk ne résout pas les constantes de app/ au moment
# où les initializers s'exécutent (→ « uninitialized constant LabEnvironmentMiddleware »).
require Rails.root.join("lib", "lab_environment_middleware").to_s
Rails.application.config.middleware.use LabEnvironmentMiddleware
