# -*- coding: utf-8 -*-
# =============================================================================
# portal/views.py — Vues CréditFlux (rendu serveur Django).
# -----------------------------------------------------------------------------
# SÉCURISÉ PAR DÉFAUT. Les SEULS écarts sont les vulnérabilités du corpus,
# tagués `// GT: APP05-Vxxx`. Les negative controls (routes sûres comparables)
# sont tagués `// NC: APP05-Nxxx`. Données 100 % FICTIVES, usage défensif.
# Toutes les charges XXE/SSRF/désérialisation ciblent EXCLUSIVEMENT l'hôte de
# laboratoire.
# =============================================================================
import base64
import csv
import hashlib
import hmac
import io
import json
import mimetypes
import os
import pickle  # nosec — utilisé volontairement pour l'écart APP05-V005.
import secrets
import ssl
import urllib.request

from django.conf import settings
from django.db import connection
from django.http import (
    FileResponse,
    Http404,
    HttpResponse,
    HttpResponseForbidden,
    HttpResponseRedirect,
    JsonResponse,
)
from django.shortcuts import redirect, render
from django.template import Context, Template
from django.utils.http import url_has_allowed_host_and_scheme
from django.views.decorators.clickjacking import xframe_options_exempt
from django.views.decorators.csrf import csrf_exempt
from django.views.decorators.http import require_GET, require_POST
from lxml import etree

from portal.models import (
    Account,
    Application,
    AuditEvent,
    Comment,
    Document,
    ImportRecord,
    Notification,
    OobHit,
    STATUS_APPROVED,
    STATUS_DRAFT,
    STATUS_SUBMITTED,
)
from portal import seed

MEDIA_ROOT = str(settings.MEDIA_ROOT)


# --- Authentification de laboratoire (session) -------------------------------
def current_user(request):
    uid = request.session.get("uid")
    if not uid:
        return None
    return Account.objects.filter(pk=uid).first()


def require_login(request):
    user = current_user(request)
    if user is None:
        return None
    return user


def _login_required_json(request):
    user = current_user(request)
    if user is None:
        return None, JsonResponse({"error": "authentification requise"}, status=401)
    return user, None


# --- Accueil -----------------------------------------------------------------
@require_GET
def home(request):
    # Dépose le cookie CSRF pour les formulaires ; en-têtes gérés par middleware.
    request.META.setdefault("CSRF_COOKIE_NEEDS_UPDATE", True)
    user = current_user(request)
    from django.middleware.csrf import get_token

    get_token(request)  # garantit l'émission du cookie csrftoken
    return render(request, "home.html", {"user": user})


# --- Connexion / déconnexion -------------------------------------------------
def login_view(request):
    from django.middleware.csrf import get_token

    get_token(request)
    if request.method == "GET":
        next_url = request.GET.get("next", "/applications")
        return render(request, "login.html", {"next": next_url})

    # POST
    username = request.POST.get("username", "")
    password = request.POST.get("password", "")
    next_url = request.POST.get("next", "/applications")
    user = Account.objects.filter(username=username).first()
    if user is None or user.password != password:
        return render(
            request, "login.html",
            {"next": next_url, "error": "Identifiants invalides (laboratoire)."},
            status=401,
        )
    request.session["uid"] = user.pk
    # 
    # (voir SESSION_COOKIE_SECURE=False / SESSION_COOKIE_SAMESITE=None).

    # 
    # domaine (aucun appel à url_has_allowed_host_and_scheme). Une URL absolue
    # externe est donc suivie après authentification.
    return HttpResponseRedirect(next_url)


@require_POST
def logout_view(request):
    request.session.flush()
    return redirect("/")


# --- Suivi des dossiers ------------------------------------------------------
@require_GET
def applications_list(request):
    user = require_login(request)
    if user is None:
        return redirect("/login?next=/applications")

    status = request.GET.get("status")
    if status is None:
        apps = Application.objects.filter(owner=user).order_by("pk")
        rows = [(a.pk, a.label, a.amount, a.status, a.owner.username) for a in apps]
    else:
        # 
        # directement dans la requête (pas de requête paramétrée sur status).
        sql = (
            "SELECT a.id, a.label, a.amount, a.status, c.username "
            "FROM portal_application a JOIN portal_account c ON c.id = a.owner_id "
            "WHERE a.owner_id = %s AND a.status = '" + status + "' ORDER BY a.id"
        )
        with connection.cursor() as cur:
            cur.execute(sql, [user.pk])
            rows = cur.fetchall()
    return render(request, "applications.html", {"user": user, "rows": rows, "status": status})


@require_GET
def applications_err(request):
    # 
    # d'erreur Django détaillée (traceback, configuration) au lieu d'une 500
    # générique.
    raise RuntimeError("Erreur de laboratoire volontaire (APP05-V011).")


@require_GET
def application_detail(request, app_id):
    user = require_login(request)
    if user is None:
        return redirect(f"/login?next=/applications/{app_id}")
    app = Application.objects.filter(pk=app_id).first()
    if app is None:
        raise Http404("Dossier introuvable")
    comments = app.comments.order_by("pk")
    docs = app.documents.order_by("pk")
    return render(
        request, "application_detail.html",
        {"user": user, "app": app, "comments": comments, "docs": docs},
    )


@require_POST
def set_amount(request, app_id):
    user = require_login(request)
    if user is None:
        return JsonResponse({"error": "authentification requise"}, status=401)
    app = Application.objects.filter(pk=app_id).first()
    if app is None or app.owner_id != user.pk:
        return HttpResponseForbidden("Accès refusé")
    try:
        amount = int(request.POST.get("amount", ""))
    except (TypeError, ValueError):
        return JsonResponse({"error": "montant invalide"}, status=400)

    # 
    # après passage à APPROVED. La transition d'état n'est PAS revérifiée ici
    # (aucun verrou sur app.status == APPROVED).
    app.amount = amount
    app.save(update_fields=["amount"])
    return JsonResponse({"id": app.pk, "amount": app.amount, "status": app.status})


@require_POST
def add_comment(request, app_id):
    user = require_login(request)
    if user is None:
        return JsonResponse({"error": "authentification requise"}, status=401)
    app = Application.objects.filter(pk=app_id).first()
    if app is None:
        raise Http404("Dossier introuvable")
    text = request.POST.get("text", "")
    # Le texte est stocké tel quel ; l'écart de rendu (non-échappement) est dans
    # le template application_detail.html (filtre |safe). 
    Comment.objects.create(application=app, author=user, text=text)
    return redirect(f"/applications/{app_id}")


@csrf_exempt  # 
# protection CSRF (les autres vues POST restent protégées par défaut).
@require_POST
def submit_application(request, app_id):
    user = require_login(request)
    if user is None:
        return JsonResponse({"error": "authentification requise"}, status=401)
    app = Application.objects.filter(pk=app_id).first()
    if app is None or app.owner_id != user.pk:
        return HttpResponseForbidden("Accès refusé")
    app.status = STATUS_SUBMITTED
    app.save(update_fields=["status"])
    return JsonResponse({"id": app.pk, "status": app.status})


@require_POST
def submit_application_secure(request, app_id):
    # // NC: APP05-N006 — Soumission PROTÉGÉE : CSRF exigé (CsrfViewMiddleware
    # actif, pas d'exemption) et contrôle d'état côté serveur. Route sûre
    # distincte de /applications/<id>/submit.
    user = require_login(request)
    if user is None:
        return JsonResponse({"error": "authentification requise"}, status=401)
    app = Application.objects.filter(pk=app_id).first()
    if app is None or app.owner_id != user.pk:
        return HttpResponseForbidden("Accès refusé")
    if app.status != STATUS_DRAFT:
        return JsonResponse({"error": "transition d'état non autorisée"}, status=409)
    app.status = STATUS_SUBMITTED
    app.save(update_fields=["status"])
    return JsonResponse({"id": app.pk, "status": app.status})


@require_POST
def approve_application(request, app_id):
    # Approbation par un analyste (met le dossier à APPROVED). Rôle vérifié.
    user = require_login(request)
    if user is None:
        return JsonResponse({"error": "authentification requise"}, status=401)
    if user.role != "ANALYSTE":
        return HttpResponseForbidden("Réservé aux analystes")
    app = Application.objects.filter(pk=app_id).first()
    if app is None:
        raise Http404("Dossier introuvable")
    app.status = STATUS_APPROVED
    app.save(update_fields=["status"])
    return JsonResponse({"id": app.pk, "status": app.status, "amount": app.amount})


# --- Upload de justificatifs (écarts V001 / V004 / V015) ---------------------
@require_POST
def upload_document(request, app_id):
    user = require_login(request)
    if user is None:
        return JsonResponse({"error": "authentification requise"}, status=401)
    app = Application.objects.filter(pk=app_id).first()
    if app is None or app.owner_id != user.pk:
        return HttpResponseForbidden("Accès refusé")
    upload = request.FILES.get("file")
    if upload is None:
        return JsonResponse({"error": "champ 'file' manquant"}, status=400)

    # Nom fourni par le client (paramètre 'filename' ou nom du fichier multipart).
    # Peut contenir des séquences ../ : utilisé tel quel (écart APP05-V004).
    filename = request.POST.get("filename") or upload.name

    # 
    #     sont contrôlés, le contenu réel n'est pas vérifié.
    # 
    #     (Content-Type) est accepté tel quel, un fichier à double nature passe.
    # 
    #     DIRECTEMENT à construire le chemin de stockage, sans canonicalisation.
    dest_dir = os.path.join(MEDIA_ROOT, "documents", str(app_id))
    dest_path = os.path.join(dest_dir, filename)  # ../ non neutralisé
    os.makedirs(os.path.dirname(dest_path), exist_ok=True)
    with open(dest_path, "wb") as out:
        for chunk in upload.chunks():
            out.write(chunk)

    relpath = os.path.relpath(dest_path, MEDIA_ROOT)
    doc = Document.objects.create(
        application=app, owner=user, filename=filename,
        relpath=relpath, content_type=upload.content_type or "application/octet-stream",
    )
    media_url = "/media/" + relpath.replace(os.sep, "/")
    return JsonResponse(
        {"id": doc.pk, "filename": filename, "url": media_url}, status=201
    )


# --- Upload d'avatar SÉCURISÉ (negative control N001) ------------------------
def _is_strict_png(data: bytes) -> bool:
    """Valide un PNG réel SANS octets superflus (défait les polyglottes)."""
    sig = b"\x89PNG\r\n\x1a\n"
    if not data.startswith(sig):
        return False
    pos = len(sig)
    n = len(data)
    while pos + 8 <= n:
        length = int.from_bytes(data[pos:pos + 4], "big")
        ctype = data[pos + 4:pos + 8]
        chunk_end = pos + 8 + length + 4  # data + CRC
        if chunk_end > n:
            return False
        if ctype == b"IEND":
            # Aucun octet ne doit suivre IEND (sinon polyglotte).
            return chunk_end == n
        pos = chunk_end
    return False


@require_POST
def profile_avatar(request):
    # // NC: APP05-N001 — Upload PROTÉGÉ : allowlist + vérification du contenu
    # réel, nom généré côté serveur (le nom client est ignoré => pas de
    # traversée), rejet des polyglottes (aucun octet après IEND). HTTP 415 sinon.
    user = require_login(request)
    if user is None:
        return JsonResponse({"error": "authentification requise"}, status=401)
    upload = request.FILES.get("file")
    if upload is None:
        return JsonResponse({"error": "champ 'file' manquant"}, status=400)
    data = upload.read()
    if not _is_strict_png(data):
        return JsonResponse(
            {"error": "type non autorisé : seules les images PNG valides sont acceptées"},
            status=415,
        )
    server_name = secrets.token_hex(16) + ".png"  # nom généré, nom client ignoré
    dest_dir = os.path.join(MEDIA_ROOT, "avatars")
    os.makedirs(dest_dir, exist_ok=True)
    dest_path = os.path.join(dest_dir, server_name)
    # Confinement : le chemin canonique reste sous MEDIA_ROOT/avatars.
    if not os.path.realpath(dest_path).startswith(os.path.realpath(dest_dir) + os.sep):
        return JsonResponse({"error": "chemin invalide"}, status=400)
    with open(dest_path, "wb") as out:
        out.write(data)
    return JsonResponse(
        {"stored": "avatars/" + server_name, "note": "image validée et renommée"},
        status=201,
    )


# --- Téléchargement de justificatif (écart V006 IDOR) ------------------------
@require_GET
def document_download(request, doc_id):
    user = require_login(request)
    if user is None:
        return JsonResponse({"error": "authentification requise"}, status=401)
    doc = Document.objects.filter(pk=doc_id).first()
    if doc is None:
        raise Http404("Pièce introuvable")
    # 
    # N'importe quel utilisateur authentifié récupère n'importe quelle pièce.
    return _document_response(doc)


@require_GET
def document_download_secure(request, doc_id):
    # // NC: APP05-N005 — Téléchargement PROTÉGÉ : vérification de propriété.
    user = require_login(request)
    if user is None:
        return JsonResponse({"error": "authentification requise"}, status=401)
    doc = Document.objects.filter(pk=doc_id).first()
    if doc is None:
        raise Http404("Pièce introuvable")
    if doc.owner_id != user.pk:
        return HttpResponseForbidden("Accès refusé : cette pièce ne vous appartient pas")
    return _document_response(doc)


def _document_response(doc):
    if doc.inline_body:
        return HttpResponse(doc.inline_body, content_type="text/plain; charset=utf-8")
    full = os.path.join(MEDIA_ROOT, doc.relpath)
    if not os.path.isfile(full):
        raise Http404("Fichier manquant")
    return FileResponse(open(full, "rb"), content_type=doc.content_type)


# --- Récupération de pièce distante (écart V003 SSRF) ------------------------
@csrf_exempt
@require_POST
def documents_fetch(request):
    user, err = _login_required_json(request)
    if err:
        return err
    try:
        payload = json.loads(request.body.decode("utf-8") or "{}")
    except (ValueError, UnicodeDecodeError):
        return JsonResponse({"error": "JSON invalide"}, status=400)
    app_id = payload.get("applicationId")
    url = payload.get("url", "")
    app = Application.objects.filter(pk=app_id, owner=user).first()
    if app is None:
        return HttpResponseForbidden("Dossier invalide")

    # 
    # allowlist de domaines ni filtrage des adresses internes/link-local. La
    # réponse est stockée et exposée via la pièce créée. (Charges ciblant
    # exclusivement l'hôte de laboratoire.)
    try:
        # 
        # vulnérable ne vérifie pas la chaîne) : la cible de laboratoire est en HTTPS
        # autosigné, l'absence de vérification fait partie de l'écart.
        _ssl_ctx = ssl._create_unverified_context()
        with urllib.request.urlopen(url, timeout=8, context=_ssl_ctx) as resp:  # nosec
            body = resp.read(65536)
    except Exception as exc:  # noqa: BLE001 — laboratoire
        return JsonResponse({"error": f"récupération échouée: {exc}"}, status=502)
    text = body.decode("utf-8", errors="replace")
    doc = Document.objects.create(
        application=app, owner=user, filename="remote-fetch",
        relpath="", content_type="text/plain", inline_body=text,
    )
    return JsonResponse({"id": doc.pk, "fetched": len(text)}, status=201)


def _is_host_allowlisted(url: str) -> bool:
    from urllib.parse import urlparse

    host = (urlparse(url).hostname or "").lower()
    return host in [h.lower() for h in settings.LAB_FETCH_ALLOWLIST]


@csrf_exempt
@require_POST
def documents_fetch_allowlisted(request):
    # // NC: APP05-N003 — Récupération PROTÉGÉE : allowlist stricte de domaines,
    # blocage implicite des IP internes/link-local (non allowlistées), pas de
    # suivi de redirection vers un hôte hors liste. HTTP 400 sinon.
    user, err = _login_required_json(request)
    if err:
        return err
    try:
        payload = json.loads(request.body.decode("utf-8") or "{}")
    except (ValueError, UnicodeDecodeError):
        return JsonResponse({"error": "JSON invalide"}, status=400)
    url = payload.get("url", "")
    if not _is_host_allowlisted(url):
        return JsonResponse(
            {"error": "domaine non autorisé (allowlist)"}, status=400
        )
    # (En laboratoire, l'allowlist pointe un hôte fictif non résolvable : toute
    # charge SSRF interne est donc rejetée avant tout appel réseau.)
    return JsonResponse({"status": "domaine autorisé — récupération simulée"}, status=200)


# --- Import de documents XML (écart V002 XXE) --------------------------------
@csrf_exempt
@require_POST
def import_xml(request):
    user, err = _login_required_json(request)
    if err:
        return err
    upload = request.FILES.get("file")
    if upload is None:
        return JsonResponse({"error": "champ 'file' manquant"}, status=400)
    raw = upload.read()

    # 
    # (resolve_entities=True, load_dtd=True, no_network=False). La valeur de
    # l'entité (contenu de fichier serveur) est réinjectée dans l'import.
    parser = etree.XMLParser(
        resolve_entities=True, load_dtd=True, no_network=False, dtd_validation=False
    )
    try:
        root = etree.fromstring(raw, parser=parser)
        note = "".join(root.itertext())
    except Exception as exc:  # noqa: BLE001 — laboratoire
        return JsonResponse({"error": f"import invalide: {exc}"}, status=400)
    rec = ImportRecord.objects.create(owner=user, note=note)
    return JsonResponse({"id": rec.pk, "note": note}, status=201)


@csrf_exempt
@require_POST
def import_xml_secure(request):
    # // NC: APP05-N002 — Import PROTÉGÉ : parseur durci, DTD et entités externes
    # désactivées (resolve_entities=False, load_dtd=False, no_network=True). Une
    # DTD déclarée est rejetée (400).
    user, err = _login_required_json(request)
    if err:
        return err
    upload = request.FILES.get("file")
    if upload is None:
        return JsonResponse({"error": "champ 'file' manquant"}, status=400)
    raw = upload.read()
    parser = etree.XMLParser(
        resolve_entities=False, load_dtd=False, no_network=True, dtd_validation=False
    )
    try:
        root = etree.fromstring(raw, parser=parser)
    except Exception as exc:  # noqa: BLE001
        return JsonResponse({"error": f"import rejeté: {exc}"}, status=400)
    # Refus explicite de toute DTD (défense en profondeur).
    if root.getroottree().docinfo.internalDTD or root.getroottree().docinfo.externalDTD:
        return JsonResponse({"error": "DTD interdite"}, status=400)
    note = "".join(root.itertext())
    rec = ImportRecord.objects.create(owner=user, note=note)
    return JsonResponse({"id": rec.pk, "note": note}, status=201)


@require_GET
def import_last(request):
    user = require_login(request)
    if user is None:
        return JsonResponse({"error": "authentification requise"}, status=401)
    rec = ImportRecord.objects.filter(owner=user).order_by("-pk").first()
    if rec is None:
        return JsonResponse({"note": None})
    return JsonResponse({"id": rec.pk, "note": rec.note})


# --- Reprise de session d'import (écart V005 désérialisation pickle) ---------
@csrf_exempt
@require_POST
def import_session(request):
    user, err = _login_required_json(request)
    if err:
        return err
    data = request.POST.get("data", "")
    try:
        raw = base64.b64decode(data)
    except Exception:  # noqa: BLE001
        return JsonResponse({"error": "base64 invalide"}, status=400)

    # 
    # l'utilisateur est désérialisé côté serveur sans aucun contrôle. Un
    # __reduce__ forgé s'exécute (preuve neutre : requête out-of-band vers
    # l'hôte de laboratoire uniquement).
    try:
        state = pickle.loads(raw)  # nosec — écart volontaire APP05-V005
    except Exception as exc:  # noqa: BLE001
        return JsonResponse({"error": f"désérialisation échouée: {exc}"}, status=400)
    return JsonResponse({"status": "session reprise", "state": str(state)[:200]})


def _sign_state(payload_b64: str) -> str:
    sig = hmac.new(settings.LAB_IMPORT_HMAC_KEY, payload_b64.encode("utf-8"),
                   hashlib.sha256).hexdigest()
    return sig


@csrf_exempt
@require_POST
def import_session_secure(request):
    # // NC: APP05-N004 — Reprise PROTÉGÉE : état en JSON, signé (HMAC) et vérifié
    # avant lecture ; types restreints ; AUCUNE désérialisation d'objet arbitraire.
    # Un flux pickle (non signé / mauvaise signature) est rejeté (400).
    user, err = _login_required_json(request)
    if err:
        return err
    data = request.POST.get("data", "")
    if "." not in data:
        return JsonResponse({"error": "format d'état invalide (attendu payload.signature)"}, status=400)
    payload_b64, sig = data.rsplit(".", 1)
    expected = _sign_state(payload_b64)
    if not hmac.compare_digest(expected, sig):
        return JsonResponse({"error": "signature invalide"}, status=400)
    try:
        state = json.loads(base64.b64decode(payload_b64).decode("utf-8"))
    except Exception:  # noqa: BLE001
        return JsonResponse({"error": "payload JSON invalide"}, status=400)
    if not isinstance(state, dict):
        return JsonResponse({"error": "type d'état non autorisé"}, status=400)
    return JsonResponse({"status": "session reprise (sûre)", "keys": list(state.keys())})


# --- Service de fichiers / statiques -----------------------------------------
@require_GET
def serve_media(request, subpath):
    base = os.path.normpath(MEDIA_ROOT)
    full = os.path.normpath(os.path.join(base, subpath))
    # Lecture confinée sous MEDIA_ROOT (le vecteur de traversée est à l'écriture).
    if not (full == base or full.startswith(base + os.sep)):
        raise Http404("Chemin invalide")
    if not os.path.isfile(full):
        raise Http404("Fichier introuvable")
    ctype, _ = mimetypes.guess_type(full)
    return FileResponse(open(full, "rb"), content_type=ctype or "application/octet-stream")


APP_JS = (
    "// CréditFlux — script de laboratoire (données fictives).\n"
    "console.log('CréditFlux lab UI');\n"
)


@require_GET
def static_app_js(request):
    # 
    # (le middleware retire 'nosniff' pour ce chemin précis). MIME sniffing.
    return HttpResponse(APP_JS, content_type="application/javascript")


# --- Administration de laboratoire -------------------------------------------
@csrf_exempt
@require_POST
def admin_reset(request):
    seed.reset_state()
    return JsonResponse({"status": "reset", "note": "état de laboratoire réinitialisé"})


# --- Canaux de validation (marqueurs out-of-band + métadonnées internes) -----
@require_GET
def internal_metadata(request):
    # Ressource « interne » de laboratoire (cible des preuves SSRF). Fictif.
    return HttpResponse(seed.INTERNAL_METADATA, content_type="text/plain; charset=utf-8")


@require_GET
def xoob_hit(request, marker):
    OobHit.objects.get_or_create(marker=marker)
    return HttpResponse("ok", content_type="text/plain")


@require_GET
def xoob_status(request, marker):
    hit = OobHit.objects.filter(marker=marker).exists()
    return JsonResponse({"marker": marker, "hit": hit})


# =============================================================================
# USE CASES FONCTIONNELS BÉNINS (sans écart) — élargissent la surface de crawl.
# Sécurisés par défaut : auth exigée, requêtes ORM paramétrées, rendu échappé.
# =============================================================================
@require_GET
def profile(request):
    user = require_login(request)
    if user is None:
        return redirect("/login?next=/profile")
    return render(request, "profile.html", {"user": user})


@require_GET
def notifications(request):
    user = require_login(request)
    if user is None:
        return redirect("/login?next=/notifications")
    items = Notification.objects.filter(account=user).order_by("-created_at", "-pk")
    return render(request, "notifications.html", {"user": user, "items": items})


@require_GET
def applications_export(request):
    # Export CSV des dossiers DE L'UTILISATEUR (filtré par propriétaire — sûr).
    user = require_login(request)
    if user is None:
        return redirect("/login?next=/applications/export")
    buf = io.StringIO()
    w = csv.writer(buf)
    w.writerow(["id", "libelle", "montant", "statut"])
    for a in Application.objects.filter(owner=user).order_by("pk"):
        w.writerow([a.pk, a.label, a.amount, a.status])
    resp = HttpResponse(buf.getvalue(), content_type="text/csv; charset=utf-8")
    resp["Content-Disposition"] = 'attachment; filename="mes-dossiers.csv"'
    return resp


@require_GET
def application_history(request, app_id):
    user = require_login(request)
    if user is None:
        return redirect(f"/login?next=/applications/{app_id}/history")
    app = Application.objects.filter(pk=app_id).first()
    if app is None:
        raise Http404("Dossier introuvable")
    # Propriétaire ou analyste uniquement (contrôle d'accès correct — sûr).
    if app.owner_id != user.pk and user.role != "ANALYSTE":
        return HttpResponseForbidden("Accès refusé")
    events = app.events.order_by("pk")
    return render(request, "history.html", {"user": user, "app": app, "events": events})


@require_GET
def help_page(request):
    return render(request, "info.html", {"user": current_user(request),
                                          "page_title": "Aide",
                                          "body": "Portail de crédit de laboratoire — données 100 % fictives."})


@require_GET
def about_page(request):
    return render(request, "info.html", {"user": current_user(request),
                                         "page_title": "À propos",
                                         "body": "CréditFlux — banc d'essai DAST, usage défensif (fictif)."})


# =============================================================================
# NOUVELLES VULNÉRABILITÉS (complexité avancée) — APP05-V018..V023
# =============================================================================
@require_GET
def applications_search(request):
    # 
    # dans la page via le filtre |safe (voir search.html), sans échappement.
    # La recherche elle-même est paramétrée (ORM) — seul le rendu de 'q' est l'écart.
    user = require_login(request)
    if user is None:
        return redirect("/login?next=/applications/search")
    q = request.GET.get("q", "")
    results = Application.objects.filter(owner=user, label__icontains=q).order_by("pk")
    return render(request, "search.html",
                  {"user": user, "q": q, "results": results, "safe_echo": True})


@require_GET
def applications_search_secure(request):
    # // NC: APP05-N007 — Recherche PROTÉGÉE : 'q' rendu avec l'auto-échappement
    # Django (pas de |safe). Même fonctionnalité, charge XSS neutralisée.
    user = require_login(request)
    if user is None:
        return redirect("/login?next=/applications/search-secure")
    q = request.GET.get("q", "")
    results = Application.objects.filter(owner=user, label__icontains=q).order_by("pk")
    return render(request, "search_secure.html",
                  {"user": user, "q": q, "results": results, "safe_echo": False})


def _account_public(user):
    return {"username": user.username, "role": user.role,
            "display_name": user.display_name, "email": user.email,
            "phone": user.phone, "bio": user.bio}


@csrf_exempt
@require_POST
def profile_update(request):
    # 
    # champs POST sont affectés à l'objet Account sans allowlist. Un emprunteur
    # peut donc fixer role=ANALYSTE (ou password) via un champ non prévu.
    user, err = _login_required_json(request)
    if err:
        return err
    if request.content_type and "application/json" in request.content_type:
        try:
            payload = json.loads(request.body.decode("utf-8") or "{}")
        except (ValueError, UnicodeDecodeError):
            return JsonResponse({"error": "JSON invalide"}, status=400)
    else:
        payload = request.POST.dict()
    for key, value in payload.items():
        if hasattr(user, key) and key not in ("id", "pk"):
            setattr(user, key, value)  # affectation massive, aucun filtre
    user.save()
    return JsonResponse({"updated": True, "profile": _account_public(user)})


@csrf_exempt
@require_POST
def profile_update_secure(request):
    # // NC: APP05-N008 — Update PROTÉGÉ : allowlist STRICTE de champs éditables.
    # role / password / username sont ignorés -> aucune élévation possible.
    user, err = _login_required_json(request)
    if err:
        return err
    if request.content_type and "application/json" in request.content_type:
        try:
            payload = json.loads(request.body.decode("utf-8") or "{}")
        except (ValueError, UnicodeDecodeError):
            return JsonResponse({"error": "JSON invalide"}, status=400)
    else:
        payload = request.POST.dict()
    allowed = {"display_name", "email", "phone", "bio"}
    for key in allowed:
        if key in payload:
            setattr(user, key, payload[key])
    user.save(update_fields=list(allowed))
    return JsonResponse({"updated": True, "profile": _account_public(user)})


@csrf_exempt
@require_POST
def password_reset(request):
    # 
    # compte existe (200 "lien envoyé") ou non (404 "aucun compte"), ce qui
    # permet d'énumérer les identifiants valides. (Aucun email réel — fictif.)
    username = (request.POST.get("username", "") or "").strip()
    if not username and request.body:
        try:
            username = (json.loads(request.body.decode("utf-8") or "{}").get("username") or "").strip()
        except (ValueError, UnicodeDecodeError):
            username = ""
    exists = Account.objects.filter(username=username).exists()
    if exists:
        return JsonResponse(
            {"status": "sent", "exists": True,
             "message": "Un lien de réinitialisation a été envoyé (fictif)."},
            status=200,
        )
    return JsonResponse(
        {"status": "unknown", "exists": False,
         "message": "Aucun compte ne correspond à cet identifiant."},
        status=404,
    )


@csrf_exempt
@require_POST
def password_reset_secure(request):
    # // NC: APP05-N009 — Reset PROTÉGÉ : réponse UNIFORME quel que soit l'état du
    # compte (même code HTTP, même message) -> aucune énumération possible.
    JsonUniform = {
        "status": "sent",
        "message": "Si un compte correspond, un lien de réinitialisation a été envoyé.",
    }
    return JsonResponse(JsonUniform, status=200)


@require_GET
def api_profile(request):
    # 
    # dans Access-Control-Allow-Origin AVEC Access-Control-Allow-Credentials: true.
    # N'importe quel site tiers peut lire ce profil authentifié avec les cookies.
    user = require_login(request)
    if user is None:
        return JsonResponse({"error": "authentification requise"}, status=401)
    resp = JsonResponse({"profile": _account_public(user)})
    origin = request.headers.get("Origin")
    if origin:
        resp["Access-Control-Allow-Origin"] = origin  # reflet non validé
        resp["Access-Control-Allow-Credentials"] = "true"
        resp["Vary"] = "Origin"
    return resp


@require_GET
def api_profile_secure(request):
    # // NC: APP05-N010 — CORS STRICT : aucune origine tierce reflétée, pas de
    # Access-Control-Allow-Credentials. La ressource n'est lisible qu'en same-origin.
    user = require_login(request)
    if user is None:
        return JsonResponse({"error": "authentification requise"}, status=401)
    return JsonResponse({"profile": _account_public(user)})


@xframe_options_exempt  # 
# de X-Frame-Options (le middleware en pose DENY partout ailleurs) et ne définit
# aucune CSP frame-ancestors -> la page peut être embarquée dans une iframe tierce.
@require_GET
def application_print(request, app_id):
    user = require_login(request)
    if user is None:
        return redirect(f"/login?next=/applications/{app_id}/print")
    app = Application.objects.filter(pk=app_id).first()
    if app is None:
        raise Http404("Dossier introuvable")
    return render(request, "print.html", {"user": user, "app": app})


@require_GET
def application_print_secure(request, app_id):
    # // NC: APP05-N011 — Page PROTÉGÉE contre le framing : NON exemptée, donc
    # X-Frame-Options: DENY (XFrameOptionsMiddleware) -> framing bloqué.
    user = require_login(request)
    if user is None:
        return redirect(f"/login?next=/applications/{app_id}/print-secure")
    app = Application.objects.filter(pk=app_id).first()
    if app is None:
        raise Http404("Dossier introuvable")
    return render(request, "print.html", {"user": user, "app": app})


@csrf_exempt
@require_POST
def notes_preview(request):
    # 
    # un TEMPLATE Django côté serveur (Template(...).render(Context(...))). Les
    # expressions {{ ... }} / tags {% ... %} sont évaluées côté serveur.
    user, err = _login_required_json(request)
    if err:
        return err
    tpl_src = request.POST.get("template", "")
    try:
        rendered = Template(tpl_src).render(Context({"user": user.username}))
    except Exception as exc:  # noqa: BLE001 — laboratoire
        return JsonResponse({"error": f"rendu échoué: {exc}"}, status=400)
    return JsonResponse({"rendered": rendered})


@csrf_exempt
@require_POST
def notes_preview_secure(request):
    # // NC: APP05-N012 — Preview PROTÉGÉE : l'entrée est traitée comme DONNÉE
    # (variable de contexte d'un template fixe), jamais compilée. Les expressions
    # restent littérales et échappées -> aucune évaluation côté serveur.
    user, err = _login_required_json(request)
    if err:
        return err
    note = request.POST.get("template", "")
    rendered = Template("{{ note }}").render(Context({"note": note}))
    return JsonResponse({"rendered": rendered})
