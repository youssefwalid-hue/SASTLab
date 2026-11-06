# -*- coding: utf-8 -*-
"""
IdP de laboratoire — OIDC Authorization Code + PKCE (donnees fictives).

Routes :
- GET  /authorize    : point d'autorisation. Validation FAIBLE du redirect_uri (APP09-V002).
- GET  /signin-oidc  : callback serveur. state/nonce NON verifies (APP09-V003).
- POST /token        : echange du code. PKCE S256 STRICT + redirect_uri exact (NC APP09-N002).
- GET  /.well-known/openid-configuration : metadonnees de decouverte.

Note laboratoire : pour une execution sans navigateur, /authorize accepte un indice
`login=<compte>` (equivalent a une authentification reussie aupres de l'IdP). Un
parametre de test `lab_exp_offset` permet de produire un jeton deja expire (pour
mettre en evidence APP09-V008 de facon reproductible). Ces commodites sont propres
au laboratoire, au meme titre que l'OTP fixe `123456`.
"""
import secrets
import time
import urllib.parse

from fastapi import APIRouter, Form, Request
from fastapi.responses import HTMLResponse, JSONResponse, RedirectResponse

from .. import config, jwtlab, security, store

router = APIRouter(tags=["oidc"])


def _issue_access_token(sub: str, nonce: str, exp_offset: int) -> dict:
    user = store.USERS.get(sub, {})
    now = int(time.time())
    payload = {
        "sub": sub,
        "role": user.get("role", "INVESTOR"),
        "accounts": user.get("accounts", []),
        "iss": config.OIDC_ISSUER,
        "aud": config.OIDC_AUDIENCE,
        "client_id": config.OIDC_CLIENT_ID,
        "iat": now,
        "exp": now + int(exp_offset),
        "nonce": nonce,
    }
    token = jwtlab.encode(payload, config.LAB_JWT_SECRET)
    return {"token": token, "expires_in": int(exp_offset)}


def _redirect_uri_weakly_valid(redirect_uri: str) -> bool:
    """
    Comparaison par correspondance partielle (startsWith sur l'origine enregistree)
    au lieu d'une egalite stricte : un hote non enregistre dont l'URI commence par
    l'origine legitime est accepte (ex. https://novabourse.lab.exemple.attaquant...).
    """
    for origin in config.REGISTERED_REDIRECT_ORIGINS:
        if redirect_uri.startswith(origin):  # <-- ecart : prefixe, pas egalite
            return True
    return False


@router.get("/authorize")
def authorize(request: Request):
    q = request.query_params
    client_id = q.get("client_id", "")
    redirect_uri = q.get("redirect_uri", "")
    response_type = q.get("response_type", "code")
    code_challenge = q.get("code_challenge", "")
    method = q.get("code_challenge_method", "S256")
    state = q.get("state", "")
    nonce = q.get("nonce", "")
    login = q.get("login", "")
    try:
        exp_offset = int(q.get("lab_exp_offset", config.DEFAULT_TOKEN_TTL))
    except ValueError:
        exp_offset = config.DEFAULT_TOKEN_TTL

    if client_id != config.OIDC_CLIENT_ID:
        return JSONResponse({"error": "unauthorized_client"}, status_code=400)
    if response_type != "code":
        return JSONResponse({"error": "unsupported_response_type"}, status_code=400)

    # 
    if not _redirect_uri_weakly_valid(redirect_uri):
        return JSONResponse({"error": "invalid_request", "error_description": "redirect_uri"},
                            status_code=400)

    # Sans indice de connexion : page de connexion minimale (flux navigateur).
    if not login:
        return HTMLResponse(_login_page(q), status_code=200)

    user = store.USERS.get(login)
    if not user:
        return JSONResponse({"error": "access_denied"}, status_code=400)

    code = "AC-" + secrets.token_urlsafe(18)
    store.AUTH_CODES[code] = {
        "sub": user["sub"], "client_id": client_id, "redirect_uri": redirect_uri,
        "code_challenge": code_challenge, "method": method, "nonce": nonce,
        "exp_offset": exp_offset, "used": False,
    }
    sep = "&" if "?" in redirect_uri else "?"
    location = f"{redirect_uri}{sep}code={urllib.parse.quote(code)}&state={urllib.parse.quote(state)}"
    return RedirectResponse(url=location, status_code=302)


@router.post("/authorize")
def authorize_post(
    request: Request,
    username: str = Form(...),
    password: str = Form(...),
    otp: str = Form(default=""),
    client_id: str = Form(...),
    redirect_uri: str = Form(...),
    code_challenge: str = Form(default=""),
    code_challenge_method: str = Form(default="S256"),
    state: str = Form(default=""),
    nonce: str = Form(default=""),
):
    user = store.USERS.get(username)
    if not user or password != config.LAB_PASSWORD:
        return JSONResponse({"error": "access_denied"}, status_code=400)
    if not _redirect_uri_weakly_valid(redirect_uri):  # 
        return JSONResponse({"error": "invalid_request"}, status_code=400)

    code = "AC-" + secrets.token_urlsafe(18)
    store.AUTH_CODES[code] = {
        "sub": user["sub"], "client_id": client_id, "redirect_uri": redirect_uri,
        "code_challenge": code_challenge, "method": code_challenge_method, "nonce": nonce,
        "exp_offset": config.DEFAULT_TOKEN_TTL, "used": False,
    }
    sep = "&" if "?" in redirect_uri else "?"
    location = f"{redirect_uri}{sep}code={urllib.parse.quote(code)}&state={urllib.parse.quote(state)}"
    return RedirectResponse(url=location, status_code=302)


@router.get("/signin-oidc")
def signin_oidc(request: Request):
    """
    Le callback serveur consomme le code et ouvre une session SANS comparer le `state`
    recu a une valeur liee a la session navigateur, et sans revalider le `nonce`.
    Un `code` appartenant a l'attaquant, injecte dans le navigateur de la victime,
    connecte celle-ci au compte de l'attaquant.
    """
    q = request.query_params
    code = q.get("code", "")
    state = q.get("state", "")  # <-- recu mais jamais compare a la session (ecart)

    rec = store.AUTH_CODES.get(code)
    if not rec or rec.get("used"):
        return JSONResponse({"error": "invalid_grant"}, status_code=400)
    rec["used"] = True

    # Aucune verification de `state`/`nonce` contre la session : la connexion aboutit.
    sid = "S-" + secrets.token_urlsafe(16)
    store.SESSIONS[sid] = {"sub": rec["sub"], "state_bound": None}
    resp = RedirectResponse(url="/", status_code=302)
    resp.set_cookie("nb_session", sid, httponly=True, samesite="lax", path="/")
    return resp


@router.post("/token")
def token(
    grant_type: str = Form(...),
    code: str = Form(...),
    redirect_uri: str = Form(...),
    client_id: str = Form(...),
    code_verifier: str = Form(default=""),
):
    """
    // NC: APP09-N002  Echange du code d'autorisation — PKCE S256 STRICT (securise).
    Verifie : grant_type, code a usage unique, client_id, redirect_uri EXACT (liste
    blanche + valeur liee au code), et S256(code_verifier) == code_challenge.
    """
    if grant_type != "authorization_code":
        return JSONResponse({"error": "unsupported_grant_type"}, status_code=400)

    rec = store.AUTH_CODES.get(code)
    if not rec or rec.get("used"):
        return JSONResponse({"error": "invalid_grant"}, status_code=400)

    if client_id != rec["client_id"]:
        return JSONResponse({"error": "invalid_client"}, status_code=400)

    # redirect_uri : egalite exacte contre la liste blanche ET contre la valeur du code.
    if redirect_uri != rec["redirect_uri"] or redirect_uri not in config.REGISTERED_REDIRECT_URIS:
        return JSONResponse({"error": "invalid_grant", "error_description": "redirect_uri"},
                            status_code=400)

    # PKCE S256 strict : recalcul et comparaison a temps constant.
    if rec.get("method", "S256") != "S256":
        return JSONResponse({"error": "invalid_request", "error_description": "pkce"},
                            status_code=400)
    if not code_verifier or security.pkce_s256(code_verifier) != rec.get("code_challenge"):
        return JSONResponse({"error": "invalid_grant", "error_description": "pkce"},
                            status_code=400)

    rec["used"] = True  # usage unique
    issued = _issue_access_token(rec["sub"], rec.get("nonce", ""), rec["exp_offset"])
    return JSONResponse({
        "access_token": issued["token"],
        "token_type": "Bearer",
        "expires_in": issued["expires_in"],
        "id_token": issued["token"],
        "scope": "openid portfolio orders",
    })


@router.get("/.well-known/openid-configuration")
def openid_config():
    base = config.OIDC_ISSUER
    return JSONResponse({
        "issuer": base,
        "authorization_endpoint": f"{base}/authorize",
        "token_endpoint": f"{base}/token",
        "response_types_supported": ["code"],
        "grant_types_supported": ["authorization_code"],
        "code_challenge_methods_supported": ["S256"],
        "id_token_signing_alg_values_supported": ["HS256"],
    })


def _login_page(q) -> str:
    def esc(v):
        import html
        return html.escape(v or "")
    hidden = "".join(
        f'<input type="hidden" name="{k}" value="{esc(q.get(k, ""))}">'
        for k in ("client_id", "redirect_uri", "code_challenge",
                  "code_challenge_method", "state", "nonce")
    )
    return f"""<!doctype html><html lang="fr"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Novabourse — Connexion (laboratoire)</title></head>
<body style="font-family:system-ui,Arial,sans-serif;max-width:420px;margin:40px auto">
<div style="background:#fff4ce;border:1px solid #e3c766;padding:8px;border-radius:6px">
Environnement de test — donnees fictives</div>
<h1>Connexion Novabourse</h1>
<form method="post" action="/authorize">
{hidden}
<p><label>Identifiant<br><input name="username" value="invest-a"></label></p>
<p><label>Mot de passe<br><input name="password" type="password" value="Password1!"></label></p>
<p><label>OTP (si demande)<br><input name="otp" value="123456"></label></p>
<button type="submit">Se connecter</button>
</form></body></html>"""
