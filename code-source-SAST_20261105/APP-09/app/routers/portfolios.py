# -*- coding: utf-8 -*-
"""
API Portefeuilles (v1). Ecarts localises : IDOR (V001), XSS stocke (V012).
Le listing et la creation restent lies au sujet du jeton (securise par defaut).
"""
import secrets

from fastapi import APIRouter, Depends, HTTPException

from .. import security, store
from ..models import PortfolioCreate

router = APIRouter(prefix="/api/portfolios", tags=["portfolios"])


@router.get("")
def list_portfolios(payload: dict = Depends(security.jwt_sig_only)):
    # Listing lie au sujet du jeton (securise). NB : garde v1 -> exp non verifiee (V008).
    user = security.user_of(payload)
    return [store.PORTFOLIOS[pid] for pid in user["portfolios"] if pid in store.PORTFOLIOS]


@router.get("/{portfolio_id}")
def get_portfolio(portfolio_id: str, payload: dict = Depends(security.jwt_sig_only)):
    """
    L'identifiant est utilise tel quel : aucune verification que
    portfolio.ownerId == token.sub. Un jeton d'invest-a lit le portefeuille d'invest-b.
    """
    pf = store.PORTFOLIOS.get(portfolio_id)
    if not pf:
        raise HTTPException(status_code=404, detail="Portefeuille introuvable")
    return pf  # <-- aucun controle de propriete


@router.post("", status_code=201)
def create_portfolio(body: PortfolioCreate, payload: dict = Depends(security.jwt_sig_only)):
    """
    Le nom est stocke SANS assainissement ; la SPA React le restitue via
    dangerouslySetInnerHTML (cf. static/app.js), d'ou l'execution de la charge.
    """
    user = security.user_of(payload)
    pid = "PF-" + secrets.token_hex(4)
    store.PORTFOLIOS[pid] = {
        "id": pid, "ownerId": user["sub"],
        "name": body.name,  # <-- stocke brut, non echappe
        "cash": 0.0, "positions": [],
    }
    user["portfolios"].append(pid)
    return store.PORTFOLIOS[pid]
