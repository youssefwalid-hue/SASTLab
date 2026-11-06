# -*- coding: utf-8 -*-
# WSGI — CréditFlux (démarrage EB/gunicorn). Données fictives, usage défensif.
import os

from django.core.wsgi import get_wsgi_application

os.environ.setdefault("DJANGO_SETTINGS_MODULE", "creditflux.settings")

application = get_wsgi_application()
