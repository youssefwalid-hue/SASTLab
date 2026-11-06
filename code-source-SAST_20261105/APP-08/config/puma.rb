# Configuration Puma — APP-08 CartaVie (lab DAST).
# EB (plateforme Ruby AL2023) : nginx proxy vers upstream unix:///var/run/puma/my_app.sock.
# Puma DOIT écouter CE socket (et non un port TCP), sinon nginx renvoie 502.
# En local : rails server -b 0.0.0.0 -p 8080 (le socket n'est utilisé que sur EB).

max_threads_count = ENV.fetch("RAILS_MAX_THREADS", 5).to_i
min_threads_count = ENV.fetch("RAILS_MIN_THREADS", max_threads_count).to_i
threads min_threads_count, max_threads_count

environment ENV.fetch("RAILS_ENV", "production")

# Socket attendu par le reverse-proxy nginx d'Elastic Beanstalk (Ruby AL2023).
bind "unix:///var/run/puma/my_app.sock"

plugin :tmp_restart
