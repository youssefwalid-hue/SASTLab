# -*- coding: utf-8 -*-
"""
API Ordres (v1). Ecarts localises : BOLA (V004), mass assignment prix (V005),
annulation d'un ordre execute (V006), absence de rate limit (V010),
BFLA back-office (V011).
"""
import time

from fastapi import APIRouter, Depends, HTTPException, Query

from .. import security, store
from ..models import OrderCreateV1

router = APIRouter(prefix="/api", tags=["orders"])


@router.post("/orders", status_code=201)
def create_order(body: OrderCreateV1, payload: dict = Depends(security.jwt_sig_only)):
    """
    """
    user = security.user_of(payload)
    # <-- V004 : l'accountId fourni par le client est utilise sans verifier la propriete.
    account_id = body.accountId
    if account_id not in store.ACCOUNTS:
        raise HTTPException(status_code=404, detail="Compte introuvable")

    oid = store.next_order_id()
    order = {
        "id": oid,
        "accountId": account_id,        # V004 : compte d'autrui accepte
        "instrument": body.instrument,
        "side": body.side,
        "quantity": body.quantity,
        "price": body.price,            # V005 : prix negatif accepte tel quel
        "status": "PENDING",
        "createdBy": user["sub"],
        "createdAt": int(time.time()),
    }
    store.ORDERS[oid] = order
    # V010 : aucun compteur / 429 — rafales acceptees sans limite.
    return order


@router.get("/orders")
def list_orders(accountId: str = Query(default=""), payload: dict = Depends(security.jwt_sig_only)):
    user = security.user_of(payload)
    if user["role"] == "BACKOFFICE":
        rows = list(store.ORDERS.values())
    else:
        # Investisseur : limite a ses propres comptes (securise par defaut).
        rows = [o for o in store.ORDERS.values() if o["accountId"] in user["accounts"]]
    if accountId:
        rows = [o for o in rows if o["accountId"] == accountId]
    return rows


@router.get("/orders/{order_id}")
def get_order(order_id: str, payload: dict = Depends(security.jwt_sig_only)):
    user = security.user_of(payload)
    order = store.ORDERS.get(order_id)
    if not order:
        raise HTTPException(status_code=404, detail="Ordre introuvable")
    # Lecture limitee au proprietaire du compte ou au back-office (securise par defaut).
    if user["role"] != "BACKOFFICE" and order["accountId"] not in user["accounts"]:
        raise HTTPException(status_code=403, detail="Acces refuse")
    return order


@router.post("/orders/{order_id}/cancel")
def cancel_order(order_id: str, payload: dict = Depends(security.jwt_sig_only)):
    """
    Aucune machine a etats : l'annulation est acceptee quel que soit le statut
    courant (y compris EXECUTED), creant une incoherence de reglement.
    """
    user = security.user_of(payload)
    order = store.ORDERS.get(order_id)
    if not order:
        raise HTTPException(status_code=404, detail="Ordre introuvable")
    if user["role"] != "BACKOFFICE" and order["accountId"] not in user["accounts"]:
        raise HTTPException(status_code=403, detail="Acces refuse")
    # <-- aucune verification de transition d'etat : EXECUTED -> CANCELLED autorise.
    order["status"] = "CANCELLED"
    return order


@router.get("/admin/orders")
def admin_orders(payload: dict = Depends(security.jwt_sig_only)):
    """
    La route d'administration ne verifie pas le role BACKOFFICE ; un INVESTOR
    consulte l'ensemble des ordres de tous les comptes.
    """
    # <-- aucun controle de role fonctionnel
    return list(store.ORDERS.values())
