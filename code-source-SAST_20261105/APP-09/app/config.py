# -*- coding: utf-8 -*-
"""
APP-09 Novabourse — Configuration de laboratoire.

Environnement de TEST, donnees 100% FICTIVES, usage defensif (banc de test DAST).
Aucune valeur sensible reelle. Les constantes OIDC/JWT sont fictives et destinees
uniquement au laboratoire local / Elastic Beanstalk.
"""
import os

# --- Identite applicative ----------------------------------------------------
APP_ID = "APP-09"
APP_NAME = "Novabourse — Portail titres"
LAB_HOST = "https://novabourse.lab.exemple"

# En-tete de laboratoire appose sur toutes les reponses.
LAB_ENV_HEADER = "X-Lab-Environment"
LAB_ENV_VALUE = "test-data-fictives"

# --- Profil de stockage ------------------------------------------------------
# standalone/local : depot memoire + SQLite (demarrage immediat, sans base externe).
APP_PROFILE = os.environ.get("APP_PROFILE", "standalone")

# --- OIDC (IdP de laboratoire) ----------------------------------------------
OIDC_ISSUER = os.environ.get("OIDC_ISSUER", LAB_HOST)
OIDC_CLIENT_ID = os.environ.get("OIDC_CLIENT_ID", "novabourse-spa")
OIDC_AUDIENCE = os.environ.get("OIDC_AUDIENCE", "novabourse-api")

# redirect_uri enregistres (liste blanche stricte utilisee par /token — securise, NC N002).
REGISTERED_REDIRECT_URIS = [
    f"{LAB_HOST}/callback",
    "http://localhost:8080/callback",
]
# Origines « enregistrees » utilisees par la verification FAIBLE de /authorize (V002).
# La comparaison vulnerable est un startsWith sur l'origine (correspondance partielle).
REGISTERED_REDIRECT_ORIGINS = [
    LAB_HOST,
    "http://localhost:8080",
]

# Origine legitime de la SPA (CORS strict sur les routes securisees v2).
SPA_ORIGIN = os.environ.get("SPA_ORIGIN", LAB_HOST)

# --- JWT de laboratoire (HS256, secret fictif) ------------------------------
# Secret FICTIF de laboratoire. En profil prod reel, injecter via variable d'env.
LAB_JWT_SECRET = os.environ.get("LAB_JWT_SECRET", "lab-novabourse-secret-fictif-ne-pas-utiliser-en-prod")
JWT_ALG = "HS256"
DEFAULT_TOKEN_TTL = 3600  # secondes

# --- Comptes de laboratoire (mot de passe fictif commun) --------------------
LAB_PASSWORD = "Password1!"
LAB_OTP = "123456"
