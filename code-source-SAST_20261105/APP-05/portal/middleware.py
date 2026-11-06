# -*- coding: utf-8 -*-
# =============================================================================
# portal/middleware.py
# - InitMiddleware     : migration + seed paresseux et idempotents.
# - LabHeadersMiddleware : en-tête X-Lab-Environment (toutes réponses) et
#   écarts d'en-têtes volontaires (V013 pas de HSTS, V014 pas de nosniff sur
#   /static/app.js, V016 bannière Server). Sécurisé par défaut ailleurs.
# =============================================================================
import threading

_INIT_LOCK = threading.Lock()
_INITIALIZED = False


class InitMiddleware:
    def __init__(self, get_response):
        self.get_response = get_response

    def __call__(self, request):
        global _INITIALIZED
        if not _INITIALIZED:
            with _INIT_LOCK:
                if not _INITIALIZED:
                    from portal.seed import ensure_initialized

                    ensure_initialized()
                    _INITIALIZED = True
        return self.get_response(request)


class LabHeadersMiddleware:
    def __init__(self, get_response):
        self.get_response = get_response

    def __call__(self, request):
        response = self.get_response(request)
        self._apply(request, response)
        return response

    def process_exception(self, request, exception):
        # L'en-tête de laboratoire doit figurer même sur les réponses d'erreur ;
        # le process_response ci-dessus s'en charge pour la 500 DEBUG générée.
        return None

    @staticmethod
    def _apply(request, response):
        # En-tête obligatoire de laboratoire sur TOUTES les réponses.
        response["X-Lab-Environment"] = "test-data-fictives"

        # 
        # Sécurisé par défaut masquerait le serveur ; ici on l'expose.
        response["Server"] = "gunicorn/21.2"

        # 
        # (Aucune ligne SECURE_HSTS_SECONDS, aucun en-tête ajouté : écart volontaire.)

        # X-Content-Type-Options: nosniff appliqué PARTOUT (sécurisé par défaut)
        # SAUF sur la ressource statique /static/app.js (écart volontaire V014).
        path = request.path
        if path == "/static/app.js":
            # 
            if "X-Content-Type-Options" in response:
                del response["X-Content-Type-Options"]
        else:
            response.setdefault("X-Content-Type-Options", "nosniff")

        # Durcissement générique restant (hors périmètre des écarts).
        # NB: 'no-referrer' fait émettre par le navigateur `Origin: null` sur un POST
        # de navigation (formulaire de login) -> le contrôle d'Origin CSRF de Django
        # rejette (« null does not match any trusted origins ») même en same-origin.
        # 'same-origin' conserve l'Origin réel sur nos propres requêtes same-origin
        # (login OK) tout en bloquant toujours les POST cross-site forgés -> CSRF intacte.
        response.setdefault("Referrer-Policy", "same-origin")
