# -*- coding: utf-8 -*-
# =============================================================================
# portal/seed.py — Initialisation et réinitialisation de l'état de laboratoire.
# Données 100 % FICTIVES. État déterministe pour la validation reproductible.
# =============================================================================
import os
import shutil

from django.conf import settings
from django.core.management import call_command
from django.db import connection

PWD = "Password1!"

# Identifiants fixes requis par le guide (endpoints/états exacts).
APP_EMPRUNTEUR1 = 1001
APP_EMPRUNTEUR2 = 1002
DOC_EMPRUNTEUR1 = 2001
DOC_EMPRUNTEUR2 = 2002

# Marqueur lu par l'entité externe XXE (fichier créé sous MEDIA_ROOT).
XXE_TARGET_NAME = "xxe_target.txt"
XXE_TARGET_CONTENT = "XXE-LAB-SECRET-APP05"

# Secret « interne » exposé par /__internal/metadata (preuve SSRF).
INTERNAL_METADATA = "LAB-INTERNAL-METADATA-APP05\nrole=creditflux-lab\n"


def _tables_exist():
    return "portal_account" in connection.introspection.table_names()


def ensure_initialized():
    """Migre la base si besoin puis seed si la base est vide (idempotent)."""
    if not _tables_exist():
        call_command("migrate", run_syncdb=True, verbosity=0, interactive=False)
    from portal.models import Account

    if not Account.objects.exists():
        seed_all()


def _media_root():
    return str(settings.MEDIA_ROOT)


def _clear_media():
    root = _media_root()
    if os.path.isdir(root):
        shutil.rmtree(root, ignore_errors=True)
    os.makedirs(os.path.join(root, "documents"), exist_ok=True)


def _write_media_file(relpath, content):
    full = os.path.join(_media_root(), relpath)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    mode = "wb" if isinstance(content, (bytes, bytearray)) else "w"
    with open(full, mode) as fh:
        fh.write(content)


def seed_all():
    """Peuple l'état de démonstration fictif avec des IDs déterministes."""
    from portal.models import (
        Account,
        Application,
        AuditEvent,
        Comment,
        Document,
        ImportRecord,
        Notification,
        OobHit,
        ROLE_ANALYSTE,
        ROLE_EMPRUNTEUR,
        STATUS_DRAFT,
        STATUS_SUBMITTED,
    )

    # Purge complète de l'état mutable.
    OobHit.objects.all().delete()
    AuditEvent.objects.all().delete()
    Notification.objects.all().delete()
    Comment.objects.all().delete()
    Document.objects.all().delete()
    ImportRecord.objects.all().delete()
    Application.objects.all().delete()
    Account.objects.all().delete()

    _clear_media()
    # Cible de l'entité externe XXE (fichier de laboratoire fictif).
    _write_media_file(XXE_TARGET_NAME, XXE_TARGET_CONTENT)
    # Échantillon de pièce distante fictive.
    _write_media_file("sample.pdf", b"%PDF-1.4 fictif creditflux lab\n")

    emp1 = Account.objects.create(
        username="emprunteur1", password=PWD, role=ROLE_EMPRUNTEUR,
        display_name="Emprunteur Un (fictif)",
        email="emprunteur1@creditflux.lab.exemple", phone="+33 1 00 00 10 01",
        bio="Dossier de prêt auto en cours (fictif).",
    )
    emp2 = Account.objects.create(
        username="emprunteur2", password=PWD, role=ROLE_EMPRUNTEUR,
        display_name="Emprunteur Deux (fictif)",
        email="emprunteur2@creditflux.lab.exemple", phone="+33 1 00 00 10 02",
        bio="Dossier de prêt travaux (fictif).",
    )
    analyste1 = Account.objects.create(
        username="analyste1", password=PWD, role=ROLE_ANALYSTE,
        display_name="Analyste Un (fictif)",
        email="analyste1@creditflux.lab.exemple", phone="+33 1 00 00 20 01",
        bio="Analyste crédit — revue des dossiers (fictif).",
    )

    app1 = Application.objects.create(
        pk=APP_EMPRUNTEUR1, owner=emp1, label="Prêt auto (fictif)",
        amount=10000, status=STATUS_DRAFT,
    )
    Application.objects.create(
        pk=APP_EMPRUNTEUR2, owner=emp2, label="Prêt travaux (fictif)",
        amount=15000, status=STATUS_SUBMITTED,
    )
    # Deuxième dossier emprunteur1 pour illustrer le filtre de suivi.
    Application.objects.create(
        owner=emp1, label="Prêt perso (fictif)", amount=3000, status=STATUS_SUBMITTED,
    )

    # Justificatifs initiaux (contenu fictif).
    _write_media_file("documents/1001/bulletin.pdf", b"%PDF-1.4 bulletin fictif emp1\n")
    Document.objects.create(
        pk=DOC_EMPRUNTEUR1, application=app1, owner=emp1,
        filename="bulletin.pdf", relpath="documents/1001/bulletin.pdf",
        content_type="application/pdf",
    )
    _write_media_file("documents/1002/avis.pdf", b"%PDF-1.4 avis fictif emp2\n")
    Document.objects.create(
        pk=DOC_EMPRUNTEUR2,
        application=Application.objects.get(pk=APP_EMPRUNTEUR2), owner=emp2,
        filename="avis.pdf", relpath="documents/1002/avis.pdf",
        content_type="application/pdf",
    )

    # -------------------------------------------------------------------------
    # VOLUME DE DONNÉES FICTIVES (complexité avancée) — 100 % fictif.
    # N'altère AUCUN identifiant déterministe ci-dessus (1001/1002/2001/2002,
    # emprunteur1/2, analyste1). Ajoute comptes, dossiers, pièces, commentaires,
    # notifications et historique d'audit pour densifier la surface de crawl.
    # -------------------------------------------------------------------------
    _seed_volume(
        Account, Application, Comment, Document, Notification, AuditEvent,
        ROLE_ANALYSTE, ROLE_EMPRUNTEUR, STATUS_DRAFT, STATUS_SUBMITTED,
        emp1, emp2, analyste1,
    )


def _seed_volume(Account, Application, Comment, Document, Notification, AuditEvent,
                 ROLE_ANALYSTE, ROLE_EMPRUNTEUR, STATUS_DRAFT, STATUS_SUBMITTED,
                 emp1, emp2, analyste1):
    """Jeu de données fictif étendu (déterministe, reproductible)."""
    STATUS_APPROVED = "APPROVED"

    # --- Comptes supplémentaires (fictifs) -----------------------------------
    extra_borrowers = []
    prenoms = ["Camille", "Sacha", "Noa", "Lou", "Maël", "Jules", "Inès", "Ryan"]
    for i in range(3, 9):  # emprunteur3..emprunteur8
        acc = Account.objects.create(
            username=f"emprunteur{i}", password=PWD, role=ROLE_EMPRUNTEUR,
            display_name=f"{prenoms[(i - 3) % len(prenoms)]} Emprunteur{i} (fictif)",
            email=f"emprunteur{i}@creditflux.lab.exemple",
            phone=f"+33 1 00 00 10 {i:02d}",
            bio="Compte emprunteur de démonstration (fictif).",
        )
        extra_borrowers.append(acc)
    analyste2 = Account.objects.create(
        username="analyste2", password=PWD, role=ROLE_ANALYSTE,
        display_name="Analyste Deux (fictif)",
        email="analyste2@creditflux.lab.exemple", phone="+33 1 00 00 20 02",
        bio="Analyste crédit senior (fictif).",
    )

    all_borrowers = [emp1, emp2] + extra_borrowers

    # --- Dossiers de prêt variés (statuts, montants, libellés) ---------------
    labels = [
        "Prêt auto", "Prêt travaux", "Prêt perso", "Rachat de crédit",
        "Prêt étudiant", "Crédit renouvelable", "Prêt immobilier",
        "LOA véhicule", "Prêt vert (rénovation)", "Prêt trésorerie",
    ]
    statuses = [STATUS_DRAFT, STATUS_SUBMITTED, STATUS_APPROVED]
    new_apps = []
    seq = 0
    for b in all_borrowers:
        for k in range(3):  # 3 dossiers par emprunteur -> ~24 dossiers
            seq += 1
            app = Application.objects.create(
                owner=b,
                label=f"{labels[seq % len(labels)]} (fictif)",
                amount=1000 * ((seq * 7) % 90 + 3),  # 3000..92000, déterministe
                status=statuses[seq % 3],
            )
            new_apps.append(app)

    # --- Pièces justificatives (contenu fictif) ------------------------------
    doc_names = ["bulletin.pdf", "avis-imposition.pdf", "rib.pdf", "piece-identite.pdf"]
    for idx, app in enumerate(new_apps):
        for j in range((idx % 2) + 1):  # 1 ou 2 pièces
            name = doc_names[(idx + j) % len(doc_names)]
            rel = f"documents/{app.pk}/{name}"
            _write_media_file(rel, f"%PDF-1.4 {name} fictif dossier {app.pk}\n".encode())
            Document.objects.create(
                application=app, owner=app.owner, filename=name,
                relpath=rel, content_type="application/pdf",
            )

    # --- Commentaires d'analyse (bénins) -------------------------------------
    analyst_notes = [
        "Pièces reçues, vérification des revenus en cours.",
        "Taux d'endettement conforme, avis favorable sous réserve.",
        "Demande de justificatif complémentaire (RIB).",
        "Dossier complet, transmis au comité (fictif).",
    ]
    for idx, app in enumerate(new_apps):
        if idx % 2 == 0:
            Comment.objects.create(
                application=app, author=(analyste1 if idx % 4 == 0 else analyste2),
                text=analyst_notes[idx % len(analyst_notes)],
            )

    # --- Notifications (use case bénin /notifications) -----------------------
    notif_tpl = [
        ("Dossier reçu", "Votre demande a bien été enregistrée (fictif)."),
        ("Pièce manquante", "Un justificatif complémentaire est attendu (fictif)."),
        ("Décision", "Votre dossier a changé de statut (fictif)."),
        ("Message analyste", "Un analyste a commenté votre dossier (fictif)."),
    ]
    for b in all_borrowers:
        for t in range(2):
            title, body = notif_tpl[(b.pk + t) % len(notif_tpl)]
            Notification.objects.create(
                account=b, title=title, body=body, is_read=(t == 0),
            )
    Notification.objects.create(
        account=emp1, title="Bienvenue", body="Portail CréditFlux — données fictives.",
    )

    # --- Historique d'audit (use case bénin /applications/<id>/history) ------
    for idx, app in enumerate(new_apps):
        AuditEvent.objects.create(
            application=app, actor=app.owner, action="CREATED",
            detail=f"Dossier créé par {app.owner.username} (fictif).",
        )
        if app.status in (STATUS_SUBMITTED, STATUS_APPROVED):
            AuditEvent.objects.create(
                application=app, actor=app.owner, action="SUBMITTED",
                detail="Dossier soumis pour instruction (fictif).",
            )
        if app.status == STATUS_APPROVED:
            AuditEvent.objects.create(
                application=app, actor=(analyste1 if idx % 2 == 0 else analyste2),
                action="APPROVED", detail="Dossier approuvé en comité (fictif).",
            )
    # Historique pour les dossiers déterministes d'origine.
    for pk in (APP_EMPRUNTEUR1, APP_EMPRUNTEUR2):
        app = Application.objects.get(pk=pk)
        AuditEvent.objects.create(
            application=app, actor=app.owner, action="CREATED",
            detail="Dossier initial (fictif).",
        )


def reset_state():
    """Remet l'état de démonstration à zéro (POST /admin/reset)."""
    seed_all()
