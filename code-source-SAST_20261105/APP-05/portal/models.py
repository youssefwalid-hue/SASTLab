# -*- coding: utf-8 -*-
# =============================================================================
# portal/models.py — Modèle de données CréditFlux (données 100 % fictives).
# Entités : Compte (emprunteur/analyste), Demande de prêt, Justificatif,
# Commentaire d'analyse, Enregistrement d'import, marqueurs out-of-band.
# =============================================================================
from django.db import models

ROLE_EMPRUNTEUR = "EMPRUNTEUR"
ROLE_ANALYSTE = "ANALYSTE"

STATUS_DRAFT = "DRAFT"
STATUS_SUBMITTED = "SUBMITTED"
STATUS_APPROVED = "APPROVED"
STATUS_CHOICES = (STATUS_DRAFT, STATUS_SUBMITTED, STATUS_APPROVED)


class Account(models.Model):
    """Compte de laboratoire (mot de passe fictif en clair, jamais réel)."""

    username = models.CharField(max_length=64, unique=True)
    password = models.CharField(max_length=128)  # Laboratoire : valeur fictive.
    role = models.CharField(max_length=16, default=ROLE_EMPRUNTEUR)
    display_name = models.CharField(max_length=128, default="")
    email = models.CharField(max_length=128, default="")   # fictif (@creditflux.lab.exemple)
    phone = models.CharField(max_length=32, default="")     # fictif
    bio = models.CharField(max_length=256, default="")      # texte de profil éditable

    def __str__(self):
        return self.username


class Application(models.Model):
    """Demande de prêt."""

    owner = models.ForeignKey(Account, on_delete=models.CASCADE, related_name="applications")
    label = models.CharField(max_length=128, default="Demande de prêt")
    amount = models.IntegerField(default=0)
    status = models.CharField(max_length=16, default=STATUS_DRAFT)

    def __str__(self):
        return f"APP#{self.pk} ({self.status})"


class Document(models.Model):
    """Justificatif rattaché à une demande."""

    application = models.ForeignKey(Application, on_delete=models.CASCADE, related_name="documents")
    owner = models.ForeignKey(Account, on_delete=models.CASCADE, related_name="documents")
    filename = models.CharField(max_length=512)
    relpath = models.CharField(max_length=1024)  # chemin relatif sous MEDIA_ROOT
    content_type = models.CharField(max_length=128, default="application/octet-stream")
    inline_body = models.TextField(blank=True, default="")  # contenu récupéré (SSRF)

    def __str__(self):
        return f"DOC#{self.pk} {self.filename}"


class Comment(models.Model):
    """Commentaire d'analyse affiché sur la fiche dossier."""

    application = models.ForeignKey(Application, on_delete=models.CASCADE, related_name="comments")
    author = models.ForeignKey(Account, on_delete=models.CASCADE)
    text = models.TextField()
    created_at = models.DateTimeField(auto_now_add=True)


class ImportRecord(models.Model):
    """Résultat d'un import de document structuré (relecture via /import/last)."""

    owner = models.ForeignKey(Account, on_delete=models.CASCADE)
    note = models.TextField(blank=True, default="")
    created_at = models.DateTimeField(auto_now_add=True)


class Notification(models.Model):
    """Notification de laboratoire affichée dans /notifications (use case bénin)."""

    account = models.ForeignKey(Account, on_delete=models.CASCADE, related_name="notifications")
    title = models.CharField(max_length=128)
    body = models.CharField(max_length=512, default="")
    is_read = models.BooleanField(default=False)
    created_at = models.DateTimeField(auto_now_add=True)


class AuditEvent(models.Model):
    """Trace d'historique d'un dossier (use case bénin /applications/<id>/history)."""

    application = models.ForeignKey(Application, on_delete=models.CASCADE, related_name="events")
    actor = models.ForeignKey(Account, on_delete=models.CASCADE)
    action = models.CharField(max_length=64)
    detail = models.CharField(max_length=256, default="")
    created_at = models.DateTimeField(auto_now_add=True)


class OobHit(models.Model):
    """Marqueur out-of-band (preuve XXE/SSRF/désérialisation en laboratoire)."""

    marker = models.CharField(max_length=128, unique=True)
    created_at = models.DateTimeField(auto_now_add=True)
