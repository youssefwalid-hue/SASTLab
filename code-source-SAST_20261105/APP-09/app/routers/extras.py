# -*- coding: utf-8 -*-
"""
API v1 — ecarts localises supplementaires (APP09-V017..V022) et surface
fonctionnelle benigne (routes de crawl sures, SANS ecart ni NC).

Ecarts localises de ce module (chacun jumele d'un NC sur /api/v2/*) :
- APP09-V017 : GET /api/whoami                 — jeton `alg:none` accepte (CWE-347).
- APP09-V018 : GET /api/accounts/directory     — exposition excessive de donnees (CWE-213).
- APP09-V019 : GET /api/instruments/logo       — SSRF, URL fournie requetee cote serveur (CWE-918).
- APP09-V020 : GET /api/export/orders.csv      — fonction critique SANS authentification (CWE-306).
- APP09-V021 : GET /api/stats/orders           — divulgation de trace d'execution (CWE-209).
- APP09-V022 : GET /api/screener               — injection d'expression via eval() (CWE-94/917).

Routes benignes (auth exigee, requetes parametrees, sorties maitrisees) :
- GET /api/instruments/list       — liste paginee d'instruments.
- GET /api/me/profile             — profil du sujet du jeton (champs non sensibles).
- GET /api/me/orders/export.json  — export JSON des ordres DU sujet (perimetre restreint).
- GET /api/notifications          — notifications fictives du sujet.
- GET /api/history                — historique pagine des ordres du sujet.
- GET /api/about                  — page d'aide / a-propos (informations publiques).

DONNEES 100% FICTIVES — usage defensif (banc de test DAST).
"""
import csv
import io
import re
import traceback
import urllib.request

from fastapi import APIRouter, Depends, Query
from fastapi.responses import JSONResponse, PlainTextResponse

from .. import security, store

router = APIRouter(prefix="/api", tags=["extras"])


# =============================================================================
# Ecarts localises (APP09-V017..V022)
# =============================================================================
@router.get("/whoami")
def whoami(payload: dict = Depends(security.jwt_alg_confused)):
    """
    La garde `jwt_alg_confused` accepte un jeton `alg:none` sans verifier la
    signature ; les claims (sub, role) presentes sont repris tels quels. Un jeton
    forge avec role=BACKOFFICE obtient donc une identite privilegiee.
    """
    return {
        "sub": payload.get("sub"),
        "role": payload.get("role"),          # <-- claim non verifie repris tel quel
        "accounts": payload.get("accounts", []),
    }


@router.get("/accounts/directory")
def accounts_directory(payload: dict = Depends(security.jwt_sig_only)):
    """
    Renvoie l'integralite des enregistrements utilisateurs — y compris des champs
    internes/sensibles (mot de passe, telephone) — au lieu d'une projection minimale.
    """
    security.user_of(payload)  # authentifie (INVESTOR suffit)
    # <-- aucun filtrage de champs : tout l'objet utilisateur est serialise.
    return list(store.USERS.values())


@router.get("/instruments/logo")
def instrument_logo(url: str = Query(default=""), payload: dict = Depends(security.jwt_sig_only)):
    """
    Aucune validation de schema/hote : loopback, adresses internes et services de
    metadonnees sont joignables depuis le serveur. Le proxy sortant est volontairement
    contourne (connexion directe) pour refleter un fetcher SSRF classique.
    """
    security.user_of(payload)
    try:
        opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
        with opener.open(url, timeout=3) as resp:  # <-- requete non filtree
            body = resp.read(2048)
            ctype = resp.headers.get("Content-Type", "")
            status = getattr(resp, "status", 200)
        return {"url": url, "status": status, "contentType": ctype,
                "body": body.decode("utf-8", "replace")}
    except Exception as e:
        return {"url": url, "error": e.__class__.__name__, "detail": str(e)}


@router.get("/export/orders.csv")
def export_orders_csv():
    """
    L'export global des ordres (tous comptes) est servi sans aucune dependance
    d'authentification : n'importe quel appelant anonyme obtient les donnees.
    """
    buf = io.StringIO()
    w = csv.writer(buf)
    w.writerow(["id", "accountId", "instrument", "side", "quantity", "price", "status", "createdBy"])
    for o in store.ORDERS.values():
        w.writerow([o["id"], o["accountId"], o["instrument"], o["side"],
                    o["quantity"], o["price"], o["status"], o["createdBy"]])
    return PlainTextResponse(buf.getvalue(), media_type="text/csv")


@router.get("/stats/orders")
def orders_stats(window: str = Query(default="10"), payload: dict = Depends(security.jwt_sig_only)):
    """
    En cas d'erreur de traitement (conversion du parametre `window`), la reponse
    renvoie le message ET la pile d'execution complete (chemins internes, modules).
    """
    security.user_of(payload)
    try:
        n = int(window)
        prices = [o["price"] for o in list(store.ORDERS.values())[:n]]
        avg = sum(prices) / len(prices)
        return {"window": n, "count": len(prices), "avgPrice": round(avg, 4)}
    except Exception as e:
        # <-- fuite d'informations : trace brute exposee au client.
        return JSONResponse(
            {"error": str(e), "type": e.__class__.__name__, "traceback": traceback.format_exc()},
            status_code=500,
        )


@router.get("/screener")
def screener(expr: str = Query(default="lastPrice>50"), payload: dict = Depends(security.jwt_sig_only)):
    """
    La « formule de screening » fournie est passee a eval() avec les builtins Python
    disponibles : une charge telle que `__import__('os').getpid()>0` s'execute cote
    serveur (RCE), tandis qu'un filtre nominal (`lastPrice>100000`) ne renvoie rien.
    """
    security.user_of(payload)
    con = store.db()
    rows = con.cursor().execute("SELECT code, name, lastPrice FROM instruments").fetchall()
    out = []
    for r in rows:
        env = {"lastPrice": r["lastPrice"], "code": r["code"], "name": r["name"]}
        try:
            if eval(expr, {}, env):  # <-- eval non borne : injection d'expression
                out.append({"code": r["code"], "lastPrice": r["lastPrice"]})
        except Exception as e:
            return JSONResponse({"error": "expr_error", "detail": str(e), "expr": expr},
                                status_code=400)
    return out


# =============================================================================
# Surface fonctionnelle benigne (SANS ecart) — routes de crawl sures
# =============================================================================
@router.get("/instruments/list")
def instruments_list(limit: int = Query(default=20, ge=1, le=100),
                     offset: int = Query(default=0, ge=0),
                     payload: dict = Depends(security.jwt_sig_only)):
    """Liste paginee des instruments (requete parametree, bornes validees)."""
    security.user_of(payload)
    con = store.db()
    rows = con.cursor().execute(
        "SELECT code, name, lastPrice FROM instruments ORDER BY code LIMIT ? OFFSET ?",
        (limit, offset),
    ).fetchall()
    return {"limit": limit, "offset": offset, "items": [dict(r) for r in rows]}


@router.get("/me/profile")
def me_profile(payload: dict = Depends(security.jwt_sig_only)):
    """Profil du sujet du jeton — projection NON sensible (pas de mot de passe)."""
    user = security.user_of(payload)
    return {
        "sub": user["sub"], "role": user["role"],
        "email": user.get("email", ""), "phone": user.get("phone", ""),
        "accounts": list(user.get("accounts", [])),
        "portfolios": list(user.get("portfolios", [])),
    }


@router.get("/me/orders/export.json")
def me_orders_export(payload: dict = Depends(security.jwt_sig_only)):
    """Export JSON des ordres DU sujet uniquement (perimetre restreint au jeton)."""
    user = security.user_of(payload)
    mine = [o for o in store.ORDERS.values() if o["accountId"] in user.get("accounts", [])]
    return {"subject": user["sub"], "count": len(mine), "orders": mine}


@router.get("/notifications")
def notifications(payload: dict = Depends(security.jwt_sig_only)):
    """Notifications fictives du sujet (donnees derivees de son propre etat)."""
    user = security.user_of(payload)
    pending = [o for o in store.ORDERS.values()
               if o["accountId"] in user.get("accounts", []) and o["status"] == "PENDING"]
    items = [{"type": "ORDER_PENDING", "orderId": o["id"], "instrument": o["instrument"]}
             for o in pending]
    items.append({"type": "INFO", "message": "Environnement de laboratoire — donnees fictives."})
    return {"subject": user["sub"], "count": len(items), "items": items}


@router.get("/history")
def history(limit: int = Query(default=10, ge=1, le=100),
            offset: int = Query(default=0, ge=0),
            payload: dict = Depends(security.jwt_sig_only)):
    """Historique pagine des ordres du sujet (perimetre restreint, bornes validees)."""
    user = security.user_of(payload)
    mine = sorted(
        [o for o in store.ORDERS.values() if o["accountId"] in user.get("accounts", [])],
        key=lambda o: o.get("createdAt", 0), reverse=True,
    )
    page = mine[offset:offset + limit]
    return {"limit": limit, "offset": offset, "total": len(mine), "items": page}


@router.get("/about")
def about():
    """Page d'aide / a-propos — informations publiques, aucune donnee sensible."""
    return {
        "application": "Novabourse — Portail titres",
        "environment": "laboratoire (donnees 100% fictives)",
        "version": "1.0.0",
        "support": "support@novabourse.lab.exemple",
    }
