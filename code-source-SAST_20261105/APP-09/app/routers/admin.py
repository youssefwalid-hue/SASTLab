# -*- coding: utf-8 -*-
"""
Administration de laboratoire : reinitialisation de l'etat de demonstration.
POST /admin/reset reseed l'integralite des donnees fictives (appele entre chaque cas
de validation). Non expose comme vulnerabilite ; commodite de laboratoire.
"""
from fastapi import APIRouter

from .. import store

router = APIRouter(tags=["admin"])


@router.post("/admin/reset")
def reset():
    store.seed()
    return {"status": "ok", "reset": True}
