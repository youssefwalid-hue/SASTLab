# -*- coding: utf-8 -*-
"""
Dependances d'authentification (jetons porteurs JWT) et utilitaires PKCE.

Deux gardes de jeton coexistent volontairement :
- `jwt_sig_only`  : routes v1 — verifie la SIGNATURE mais IGNORE `exp`  (ecart APP09-V008).
- `jwt_strict`    : routes v2 securisees — verifie signature + `exp`     (NC APP09-N005).

Dans les deux cas la signature est verifiee : un jeton forge/altere est rejete
(securise par defaut). Seul le controle temporel differe.
"""
import base64
import hashlib

from fastapi import Header, HTTPException

from . import config, jwtlab, store


def _extract_bearer(authorization: str) -> str:
    if not authorization or not authorization.lower().startswith("bearer "):
        raise HTTPException(status_code=401, detail="Jeton porteur requis")
    return authorization.split(" ", 1)[1].strip()


def jwt_sig_only(authorization: str = Header(default="")) -> dict:
    """Garde v1 : signature verifiee, expiration NON verifiee (APP09-V008)."""
    token = _extract_bearer(authorization)
    try:
        # 
        payload = jwtlab.decode(token, config.LAB_JWT_SECRET, verify_exp=False)
    except jwtlab.JwtError:
        raise HTTPException(status_code=401, detail="Jeton invalide")
    return payload


def jwt_strict(authorization: str = Header(default="")) -> dict:
    """Garde v2 : signature + expiration (securise, NC APP09-N005)."""
    token = _extract_bearer(authorization)
    try:
        payload = jwtlab.decode(token, config.LAB_JWT_SECRET, verify_exp=True)
    except jwtlab.JwtError:
        raise HTTPException(status_code=401, detail="Jeton invalide ou expire")
    return payload


def jwt_alg_confused(authorization: str = Header(default="")) -> dict:
    """Garde v1 permissive : accepte `alg:none` sans signature (APP09-V017)."""
    token = _extract_bearer(authorization)
    try:
        payload = jwtlab.decode_any_alg(token, config.LAB_JWT_SECRET, verify_exp=False)
    except jwtlab.JwtError:
        raise HTTPException(status_code=401, detail="Jeton invalide")
    return payload


def user_of(payload: dict) -> dict:
    sub = payload.get("sub")
    user = store.USERS.get(sub)
    if not user:
        raise HTTPException(status_code=401, detail="Sujet inconnu")
    return user


# --- PKCE (S256) -------------------------------------------------------------
def pkce_s256(code_verifier: str) -> str:
    digest = hashlib.sha256(code_verifier.encode("ascii")).digest()
    return base64.urlsafe_b64encode(digest).rstrip(b"=").decode("ascii")
