# -*- coding: utf-8 -*-
"""
Routes SECURISEES (v2) — contr^oles negatifs (negative controls).

Chaque route reproduit une surface comparable a une vulnerabilite v1 mais la traite
correctement. Une alerte d'un scanner sur une route /api/v2/* est un FAUX POSITIF.

- GET  /api/v2/portfolios/{id}  -> NC APP09-N001 (controle de propriete, 404)
- GET  /api/v2/portfolios       -> NC APP09-N005 (JWT strict : signature + exp)
- POST /api/v2/orders           -> NC APP09-N003 (accountId derive du jeton, 403)
                                   NC APP09-N004 (prix borne, champs hors contrat 422)
- GET  /api/v2/instruments      -> NC APP09-N006 (requete parametree)
- GET  /api/v2/whoami             -> NC APP09-N007 (JWT strict : alg:none rejete)
- GET  /api/v2/accounts/directory -> NC APP09-N008 (projection minimale, pas de secret)
- GET  /api/v2/instruments/logo   -> NC APP09-N009 (SSRF : liste blanche, loopback bloque)
- GET  /api/v2/export/orders.csv  -> NC APP09-N010 (authentifie + perimetre du sujet)
- GET  /api/v2/stats/orders       -> NC APP09-N011 (erreur generique, pas de trace)
- GET  /api/v2/screener           -> NC APP09-N012 (grammaire sure, pas d'eval)
"""
import csv
import io
import re
import time
import urllib.parse

from fastapi import APIRouter, Depends, HTTPException, Query
from fastapi.responses import PlainTextResponse

from .. import security, store
from ..models import OrderCreateV2

router = APIRouter(prefix="/api/v2", tags=["secure-v2"])

# Hotes autorises pour la recuperation de logo (liste blanche stricte — NC N009).
_LOGO_ALLOWED_HOSTS = {"cdn.novabourse.lab.exemple", "assets.novabourse.lab.exemple"}
# Grammaire sure du screener (NC N012) : lastPrice <op> <nombre>.
_SCREENER_GRAMMAR = re.compile(r"^\s*lastPrice\s*(>=|<=|==|>|<)\s*(\d+(?:\.\d+)?)\s*$")


@router.get("/portfolios")
def list_portfolios_secure(payload: dict = Depends(security.jwt_strict)):
    # // NC: APP09-N005 — jwt_strict verifie signature + exp (jeton expire -> 401).
    user = security.user_of(payload)
    return [store.PORTFOLIOS[pid] for pid in user["portfolios"] if pid in store.PORTFOLIOS]


@router.get("/portfolios/{portfolio_id}")
def get_portfolio_secure(portfolio_id: str, payload: dict = Depends(security.jwt_strict)):
    """
    // NC: APP09-N001  Lecture de portefeuille AVEC controle de propriete (securise).
    Verifie portfolio.ownerId == token.sub ; renvoie 404 (non divulguant) sinon.
    """
    user = security.user_of(payload)
    pf = store.PORTFOLIOS.get(portfolio_id)
    if not pf or pf["ownerId"] != user["sub"]:
        raise HTTPException(status_code=404, detail="Portefeuille introuvable")
    return pf


@router.post("/orders", status_code=201)
def create_order_secure(body: OrderCreateV2, payload: dict = Depends(security.jwt_strict)):
    """
    // NC: APP09-N003  Compte derive du jeton (accountId du corps ignore/valide, 403).
    // NC: APP09-N004  Prix borne (> 0) et champs hors contrat rejetes (422 via extra=forbid).
    """
    user = security.user_of(payload)
    # Compte derive du jeton ; si un accountId est fourni, il doit appartenir au sujet.
    if body.accountId is not None and body.accountId not in user["accounts"]:
        raise HTTPException(status_code=403, detail="Compte non rattache au sujet")
    account_id = body.accountId if body.accountId in user["accounts"] else (
        user["accounts"][0] if user["accounts"] else None)
    if not account_id:
        raise HTTPException(status_code=403, detail="Aucun compte rattache")

    oid = store.next_order_id()
    order = {
        "id": oid, "accountId": account_id, "instrument": body.instrument,
        "side": body.side, "quantity": body.quantity, "price": body.price,
        "status": "PENDING", "createdBy": user["sub"], "createdAt": int(time.time()),
    }
    store.ORDERS[oid] = order
    return order


@router.get("/instruments")
def search_instruments_secure(q: str = Query(default=""), payload: dict = Depends(security.jwt_strict)):
    """
    // NC: APP09-N006  Recherche par requete PARAMETREE (securise).
    Le terme est lie comme valeur litterale ; une charge SQL ne modifie pas le resultat.
    """
    security.user_of(payload)
    con = store.db()
    cur = con.cursor()
    like = f"%{q}%"
    rows = cur.execute(
        "SELECT code, name, lastPrice FROM instruments WHERE name LIKE ? OR code LIKE ?",
        (like, like),
    ).fetchall()
    return [dict(r) for r in rows]


@router.get("/whoami")
def whoami_secure(payload: dict = Depends(security.jwt_strict)):
    """
    // NC: APP09-N007  Identite derivee d'un JWT strictement valide (securise).
    `jwt_strict` verifie signature HS256 + exp : un jeton `alg:none` (signature vide)
    ou altere est rejete (401). Les claims ne sont jamais repris sans verification.
    """
    user = security.user_of(payload)
    return {"sub": user["sub"], "role": user["role"], "accounts": list(user.get("accounts", []))}


@router.get("/accounts/directory")
def accounts_directory_secure(payload: dict = Depends(security.jwt_strict)):
    """
    // NC: APP09-N008  Projection minimale non sensible (securise).
    Renvoie uniquement des champs publics (sub, role) ; aucun mot de passe ni
    coordonnee n'est serialise.
    """
    security.user_of(payload)
    return [{"sub": u["sub"], "role": u["role"]} for u in store.USERS.values()]


@router.get("/instruments/logo")
def instrument_logo_secure(url: str = Query(default=""), payload: dict = Depends(security.jwt_strict)):
    """
    // NC: APP09-N009  Recuperation de logo avec liste blanche d'hotes (securise).
    Seuls http(s) et un hote explicitement autorise sont acceptes ; loopback,
    adresses internes et schemas exotiques sont rejetes (400) sans requete sortante.
    """
    security.user_of(payload)
    try:
        parsed = urllib.parse.urlparse(url)
    except Exception:
        raise HTTPException(status_code=400, detail="URL invalide")
    if parsed.scheme not in ("http", "https") or parsed.hostname not in _LOGO_ALLOWED_HOSTS:
        raise HTTPException(status_code=400, detail="Hote non autorise")
    # (En laboratoire, on ne declenche pas l'appel sortant : la validation suffit au NC.)
    return {"url": url, "status": "allowed", "host": parsed.hostname}


@router.get("/export/orders.csv")
def export_orders_csv_secure(payload: dict = Depends(security.jwt_strict)):
    """
    // NC: APP09-N010  Export authentifie et restreint au sujet (securise).
    Authentification requise (jwt_strict) ; seuls les ordres des comptes du sujet
    sont exportes (pas d'acces anonyme ni global).
    """
    user = security.user_of(payload)
    buf = io.StringIO()
    w = csv.writer(buf)
    w.writerow(["id", "accountId", "instrument", "side", "quantity", "price", "status"])
    for o in store.ORDERS.values():
        if o["accountId"] in user.get("accounts", []):
            w.writerow([o["id"], o["accountId"], o["instrument"], o["side"],
                        o["quantity"], o["price"], o["status"]])
    return PlainTextResponse(buf.getvalue(), media_type="text/csv")


@router.get("/stats/orders")
def orders_stats_secure(window: str = Query(default="10"), payload: dict = Depends(security.jwt_strict)):
    """
    // NC: APP09-N011  Gestion d'erreur generique sans divulgation (securise).
    Une entree invalide produit un 400 au message neutre ; aucune trace d'execution
    ni chemin interne n'est renvoye au client.
    """
    security.user_of(payload)
    if not window.isdigit():
        raise HTTPException(status_code=400, detail="Parametre window invalide")
    n = int(window)
    if n <= 0:
        raise HTTPException(status_code=400, detail="Parametre window invalide")
    prices = [o["price"] for o in list(store.ORDERS.values())[:n]]
    avg = (sum(prices) / len(prices)) if prices else 0.0
    return {"window": n, "count": len(prices), "avgPrice": round(avg, 4)}


@router.get("/screener")
def screener_secure(expr: str = Query(default="lastPrice>50"), payload: dict = Depends(security.jwt_strict)):
    """
    // NC: APP09-N012  Screener a grammaire restreinte, sans eval (securise).
    Seule la forme `lastPrice <op> <nombre>` est acceptee ; toute autre expression
    (appel, import, attribut) est rejetee (422). Aucune evaluation dynamique.
    """
    security.user_of(payload)
    m = _SCREENER_GRAMMAR.match(expr or "")
    if not m:
        raise HTTPException(status_code=422, detail="Expression non autorisee")
    op, num = m.group(1), float(m.group(2))
    import operator
    cmp = {">": operator.gt, "<": operator.lt, ">=": operator.ge,
           "<=": operator.le, "==": operator.eq}[op]
    con = store.db()
    rows = con.cursor().execute("SELECT code, lastPrice FROM instruments").fetchall()
    return [{"code": r["code"], "lastPrice": r["lastPrice"]} for r in rows if cmp(r["lastPrice"], num)]
