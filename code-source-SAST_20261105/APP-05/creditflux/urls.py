# -*- coding: utf-8 -*-
# =============================================================================
# creditflux/urls.py — Table de routage EXACTE du corpus APP-05.
# Endpoints et paramètres conformes au VULNERABILITY_GUIDE / corpus_master.
# =============================================================================
from django.urls import path, re_path

from portal import views

urlpatterns = [
    # --- Accueil / session ---------------------------------------------------
    path("", views.home, name="home"),  # 
    path("login", views.login_view, name="login"),  # 
    path("logout", views.logout_view, name="logout"),
    # --- Suivi des dossiers --------------------------------------------------
    path("applications", views.applications_list, name="applications"),  # 
    path("applications/err", views.applications_err, name="applications_err"),  # 
    # Recherche (routes statiques AVANT le convertisseur <int:app_id>).
    path("applications/search", views.applications_search, name="applications_search"),  # 
    path("applications/search-secure", views.applications_search_secure, name="applications_search_secure"),  # // NC: APP05-N007
    path("applications/export", views.applications_export, name="applications_export"),  # use case bénin
    path("applications/<int:app_id>", views.application_detail, name="application_detail"),
    path("applications/<int:app_id>/history", views.application_history, name="application_history"),  # use case bénin
    path("applications/<int:app_id>/print", views.application_print, name="application_print"),  # 
    path("applications/<int:app_id>/print-secure", views.application_print_secure, name="application_print_secure"),  # // NC: APP05-N011
    path("applications/<int:app_id>/documents", views.upload_document, name="upload_document"),  # 
    path("applications/<int:app_id>/amount", views.set_amount, name="set_amount"),  # 
    path("applications/<int:app_id>/comment", views.add_comment, name="add_comment"),  # 
    path("applications/<int:app_id>/submit", views.submit_application, name="submit"),  # 
    path("applications/<int:app_id>/approve", views.approve_application, name="approve"),
    # Negative control N006 (CSRF + contrôle d'état) — route sûre distincte.
    path("applications/<int:app_id>/submit-secure", views.submit_application_secure, name="submit_secure"),  # // NC: APP05-N006
    # --- Justificatifs -------------------------------------------------------
    path("documents/fetch", views.documents_fetch, name="documents_fetch"),  # 
    path("documents/fetch-allowlisted", views.documents_fetch_allowlisted, name="documents_fetch_allowlisted"),  # // NC: APP05-N003
    path("documents/secure/<int:doc_id>", views.document_download_secure, name="document_download_secure"),  # // NC: APP05-N005
    path("documents/<int:doc_id>", views.document_download, name="document_download"),  # 
    # --- Import de documents -------------------------------------------------
    path("import", views.import_xml, name="import"),  # 
    path("import/secure", views.import_xml_secure, name="import_secure"),  # // NC: APP05-N002
    path("import/last", views.import_last, name="import_last"),
    path("import/session", views.import_session, name="import_session"),  # 
    path("import/session-secure", views.import_session_secure, name="import_session_secure"),  # // NC: APP05-N004
    # --- Profil (upload SÉCURISÉ — negative control N001) --------------------
    path("profile/avatar", views.profile_avatar, name="profile_avatar"),  # // NC: APP05-N001
    # --- Profil / compte (use cases + écarts avancés) ------------------------
    path("profile", views.profile, name="profile"),  # use case bénin
    path("profile/update", views.profile_update, name="profile_update"),  # 
    path("profile/update-secure", views.profile_update_secure, name="profile_update_secure"),  # // NC: APP05-N008
    path("api/profile", views.api_profile, name="api_profile"),  # 
    path("api/profile-secure", views.api_profile_secure, name="api_profile_secure"),  # // NC: APP05-N010
    path("password/reset", views.password_reset, name="password_reset"),  # 
    path("password/reset-secure", views.password_reset_secure, name="password_reset_secure"),  # // NC: APP05-N009
    path("notes/preview", views.notes_preview, name="notes_preview"),  # 
    path("notes/preview-secure", views.notes_preview_secure, name="notes_preview_secure"),  # // NC: APP05-N012
    # --- Notifications / aide (use cases bénins) -----------------------------
    path("notifications", views.notifications, name="notifications"),
    path("help", views.help_page, name="help"),
    path("about", views.about_page, name="about"),
    # --- Service de fichiers / statiques ------------------------------------
    path("static/app.js", views.static_app_js, name="static_app_js"),  # 
    re_path(r"^media/(?P<subpath>.+)$", views.serve_media, name="serve_media"),
    # --- Administration de laboratoire --------------------------------------
    path("admin/reset", views.admin_reset, name="admin_reset"),
    # --- Canaux de validation de laboratoire (marqueurs out-of-band) ---------
    # Non exposés dans l'UI ; utilisés par les preuves XXE/SSRF/désérialisation.
    path("__internal/metadata", views.internal_metadata, name="internal_metadata"),
    path("__xoob/<str:marker>", views.xoob_hit, name="xoob_hit"),
    path("__xoob-status/<str:marker>", views.xoob_status, name="xoob_status"),
]
