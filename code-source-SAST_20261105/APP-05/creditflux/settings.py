# -*- coding: utf-8 -*-
# =============================================================================
# creditflux/settings.py — APP-05 CréditFlux (Banking DAST Benchmark Lab)
# -----------------------------------------------------------------------------
# Application de laboratoire volontairement vulnérable, DONNÉES 100 % FICTIVES,
# usage défensif (test de scanners DAST). SÉCURISÉE PAR DÉFAUT : les seuls
# écarts sont les vulnérabilités du corpus (APP05-V001..V017), localisées.
# `.ebextensions` ne fournit QUE des variables d'environnement.
# =============================================================================
import os
from pathlib import Path

BASE_DIR = Path(__file__).resolve().parent.parent


def _env_bool(name, default):
    val = os.environ.get(name)
    if val is None:
        return default
    return val.strip().lower() in ("1", "true", "yes", "on")


# Clé fictive de laboratoire (aucun secret réel ; données 100 % fictives).
SECRET_KEY = os.environ.get(
    "DJANGO_SECRET_KEY", "lab-fictif-creditflux-app05-ne-pas-utiliser-en-prod"
)

# 
# En déploiement réel ce serait False ; ici c'est une vulnérabilité volontaire.
DEBUG = _env_bool("DJANGO_DEBUG", True)

# Accès ouvert (laboratoire derrière ALB, sans filtrage IP) — données fictives.
ALLOWED_HOSTS = ["*"]

# Origines de confiance CSRF : requis par Django 4+ pour que la connexion LÉGITIME
# depuis un navigateur passe la vérification d'Origin en HTTPS (sinon 403 « Origin
# checking failed »). Renseigné au déploiement via CSRF_TRUSTED_ORIGINS (le CNAME réel
# est substitué). N'affecte PAS les écarts volontaires : la vue CSRF V010 reste exemptée.
CSRF_TRUSTED_ORIGINS = [
    o.strip()
    for o in os.environ.get("CSRF_TRUSTED_ORIGINS", "").split(",")
    if o.strip()
]

INSTALLED_APPS = [
    "django.contrib.contenttypes",
    "django.contrib.sessions",
    "portal",
]

MIDDLEWARE = [
    # Initialisation paresseuse (migrate + seed) — idempotente.
    "portal.middleware.InitMiddleware",
    # En-têtes de laboratoire + écarts d'en-têtes volontaires (V013/V014/V016).
    "portal.middleware.LabHeadersMiddleware",
    "django.contrib.sessions.middleware.SessionMiddleware",
    "django.middleware.common.CommonMiddleware",
    # Protection CSRF active PARTOUT (sécurisé par défaut) ; seule la vue
    # /applications/<id>/submit est exemptée (écart volontaire APP05-V010).
    "django.middleware.csrf.CsrfViewMiddleware",
    "django.middleware.clickjacking.XFrameOptionsMiddleware",
]

ROOT_URLCONF = "creditflux.urls"

TEMPLATES = [
    {
        "BACKEND": "django.template.backends.django.DjangoTemplates",
        "DIRS": [BASE_DIR / "portal" / "templates"],
        "APP_DIRS": True,
        "OPTIONS": {
            "context_processors": [
                "django.template.context_processors.request",
                "django.template.context_processors.csrf",
            ],
        },
    },
]

WSGI_APPLICATION = "creditflux.wsgi.application"

# Profil standalone : SQLite, démarrage immédiat, données fictives seedées.
DATABASES = {
    "default": {
        "ENGINE": "django.db.backends.sqlite3",
        "NAME": BASE_DIR / "db.sqlite3",
    }
}

LANGUAGE_CODE = "fr-fr"
TIME_ZONE = "UTC"
USE_I18N = True
USE_TZ = True

STATIC_URL = "/static/"
MEDIA_URL = "/media/"
MEDIA_ROOT = BASE_DIR / "media"

DEFAULT_AUTO_FIELD = "django.db.models.BigAutoField"

# --- Durcissement « sécurisé par défaut » (sauf écarts listés) ---------------
# Cookie CSRF exploitable en HTTP local ; protection CSRF réellement active.
CSRF_COOKIE_SECURE = False  # laboratoire HTTP local ; sous ALB le TLS termine en amont.
CSRF_COOKIE_HTTPONLY = False  # requis pour le double-submit côté formulaire.
CSRF_COOKIE_SAMESITE = "Lax"

# 
# Sécurisé par défaut imposerait Secure + SameSite ; ici ils sont retirés.
SESSION_COOKIE_SECURE = False
SESSION_COOKIE_SAMESITE = None  # None (objet) => Django N'ÉMET PAS l'attribut SameSite.
SESSION_COOKIE_HTTPONLY = True  # HttpOnly conservé (hors périmètre de l'écart).
SESSION_ENGINE = "django.contrib.sessions.backends.db"

# En-têtes de sécurité HTTP : HSTS et nosniff sont gérés dans LabHeadersMiddleware
# pour matérialiser précisément les écarts V013 (pas de HSTS) et V014 (pas de
# nosniff sur /static/app.js) tout en restant sûr ailleurs.
X_FRAME_OPTIONS = "DENY"

# Secret HMAC (laboratoire) pour la reprise de session d'import SÉCURISÉE (N004).
LAB_IMPORT_HMAC_KEY = os.environ.get(
    "LAB_IMPORT_HMAC_KEY", "lab-hmac-key-app05-fictif"
).encode("utf-8")

# Allowlist de domaines pour la récupération de pièce SÉCURISÉE (N003).
LAB_FETCH_ALLOWLIST = [
    h.strip()
    for h in os.environ.get(
        "LAB_FETCH_ALLOWLIST", "creditflux.lab.exemple,media.creditflux.lab.exemple"
    ).split(",")
    if h.strip()
]
