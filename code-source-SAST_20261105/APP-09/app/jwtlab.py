# -*- coding: utf-8 -*-
"""
JWT HS256 minimal et autonome pour l'IdP de laboratoire (donnees fictives).

- `encode(payload, secret)` : produit un JWT HS256 compact.
- `decode(token, secret, verify_exp)` : verifie TOUJOURS la signature ;
  ne verifie le claim `exp` que si `verify_exp=True`.

Ce decoupage materialise l'ecart APP09-V008 : les routes v1 valident la signature
mais ignorent `exp` (verify_exp=False), tandis que les routes securisees v2 valident
signature + `exp` (verify_exp=True, NC APP09-N005). Aucune dependance externe.
"""
import base64
import hashlib
import hmac
import json
import time


class JwtError(Exception):
    pass


def _b64u_encode(raw: bytes) -> str:
    return base64.urlsafe_b64encode(raw).rstrip(b"=").decode("ascii")


def _b64u_decode(seg: str) -> bytes:
    pad = "=" * (-len(seg) % 4)
    return base64.urlsafe_b64decode(seg + pad)


def encode(payload: dict, secret: str, kid: str = "lab-1") -> str:
    header = {"alg": "HS256", "typ": "JWT", "kid": kid}
    h = _b64u_encode(json.dumps(header, separators=(",", ":")).encode("utf-8"))
    p = _b64u_encode(json.dumps(payload, separators=(",", ":")).encode("utf-8"))
    signing_input = f"{h}.{p}".encode("ascii")
    sig = hmac.new(secret.encode("utf-8"), signing_input, hashlib.sha256).digest()
    return f"{h}.{p}.{_b64u_encode(sig)}"


def decode(token: str, secret: str, verify_exp: bool = True, leeway: int = 5) -> dict:
    """Verifie la signature (toujours) et, si demande, le claim `exp`."""
    try:
        h_seg, p_seg, s_seg = token.split(".")
    except ValueError:
        raise JwtError("format de jeton invalide")

    signing_input = f"{h_seg}.{p_seg}".encode("ascii")
    expected = hmac.new(secret.encode("utf-8"), signing_input, hashlib.sha256).digest()
    try:
        provided = _b64u_decode(s_seg)
    except Exception:
        raise JwtError("signature illisible")
    # Comparaison a temps constant : rejette toute signature invalide (securise partout).
    if not hmac.compare_digest(expected, provided):
        raise JwtError("signature invalide")

    try:
        payload = json.loads(_b64u_decode(p_seg).decode("utf-8"))
    except Exception:
        raise JwtError("charge utile illisible")

    if verify_exp:
        exp = payload.get("exp")
        if exp is None:
            raise JwtError("claim exp absent")
        if int(time.time()) > int(exp) + leeway:
            raise JwtError("jeton expire")
    return payload


def decode_any_alg(token: str, secret: str, verify_exp: bool = False, leeway: int = 5) -> dict:
    """
    Lit l'en-tete du jeton et, si `alg` vaut `none`, renvoie la charge utile SANS
    verifier la moindre signature : un jeton forge (sub/role arbitraires, signature
    vide) est accepte. Pour tout autre `alg`, la signature HS256 est verifiee comme
    d'habitude (ecart localise a ce decodeur permissif).
    """
    try:
        h_seg, p_seg, s_seg = token.split(".")
    except ValueError:
        raise JwtError("format de jeton invalide")

    try:
        header = json.loads(_b64u_decode(h_seg).decode("utf-8"))
    except Exception:
        raise JwtError("en-tete illisible")
    try:
        payload = json.loads(_b64u_decode(p_seg).decode("utf-8"))
    except Exception:
        raise JwtError("charge utile illisible")

    alg = str(header.get("alg", "")).lower()
    if alg in ("none", ""):
        # <-- ecart : aucune verification de signature pour alg=none.
        return payload

    signing_input = f"{h_seg}.{p_seg}".encode("ascii")
    expected = hmac.new(secret.encode("utf-8"), signing_input, hashlib.sha256).digest()
    try:
        provided = _b64u_decode(s_seg)
    except Exception:
        raise JwtError("signature illisible")
    if not hmac.compare_digest(expected, provided):
        raise JwtError("signature invalide")

    if verify_exp:
        exp = payload.get("exp")
        if exp is None or int(time.time()) > int(exp) + leeway:
            raise JwtError("jeton expire")
    return payload


def peek_payload(token: str) -> dict:
    """Decode la charge utile SANS verifier la signature (usage interne/diagnostic)."""
    try:
        _, p_seg, _ = token.split(".")
        return json.loads(_b64u_decode(p_seg).decode("utf-8"))
    except Exception:
        return {}
