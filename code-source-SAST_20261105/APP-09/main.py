# -*- coding: utf-8 -*-
"""
APP-09 Novabourse — Portail titres (laboratoire DAST).

Application FastAPI + SPA React minimale, SECURISEE PAR DEFAUT, comportant uniquement
les 22 ecarts localises du referentiel (APP09-V001..V022) et des routes securisees
de reference (negative controls APP09-N001..N012, sous /api/v2/*).

DONNEES 100% FICTIVES — usage defensif (test de scanners DAST).
Lancement local :  uvicorn main:app --host 0.0.0.0 --port 8080
Elastic Beanstalk : web: uvicorn main:app --host 0.0.0.0 --port 5000  (Procfile)

Ecarts geres au niveau de l'application (main) :
- APP09-V007 : /openapi.json complet expose (schema FastAPI par defaut).
- APP09-V013 : /docs (Swagger UI) expose.
- APP09-V014 : aucun en-tete de securite HTTP (HSTS/nosniff/frame/CSP).
- APP09-V015 : CORS reflete l'Origin avec credentials sur les routes v1 /api/*.
- APP09-V016 : banniere serveur (Server: uvicorn) + X-Powered-By divulguees.
"""
import os

from fastapi import FastAPI, Request
from fastapi.responses import FileResponse, Response
from fastapi.staticfiles import StaticFiles

from app import config, store
from app.routers import admin, extras, instruments, oidc, orders, portfolios, secure

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
STATIC_DIR = os.path.join(BASE_DIR, "static")

# 
# 
app = FastAPI(
    title="Novabourse — Portail titres (laboratoire DAST)",
    version="1.0.0",
    description="Environnement de test — donnees 100% fictives — usage defensif.",
)

# Seed initial au demarrage.
store.seed()


@app.middleware("http")
async def lab_and_cors_middleware(request: Request, call_next):
    origin = request.headers.get("origin", "")
    path = request.url.path

    # Pre-vol CORS minimal (pour les routes API).
    if request.method == "OPTIONS" and path.startswith("/api/"):
        resp = Response(status_code=204)
    else:
        resp = await call_next(request)

    # En-tete de laboratoire sur toutes les reponses.
    resp.headers[config.LAB_ENV_HEADER] = config.LAB_ENV_VALUE

    # 
    # par le serveur ASGI ; on ajoute de plus un X-Powered-By explicite).
    resp.headers["X-Powered-By"] = "FastAPI"

    # CORS :
    if path.startswith("/api/v2/"):
        # Securise (NC) : liste blanche stricte d'une seule origine, pas de reflexion.
        resp.headers["Access-Control-Allow-Origin"] = config.SPA_ORIGIN
        resp.headers["Access-Control-Allow-Credentials"] = "true"
        resp.headers["Vary"] = "Origin"
    elif path.startswith("/api/"):
        # 
        if origin:
            resp.headers["Access-Control-Allow-Origin"] = origin  # <-- reflexion
            resp.headers["Access-Control-Allow-Credentials"] = "true"
            resp.headers["Vary"] = "Origin"
    if request.method == "OPTIONS" and path.startswith("/api/"):
        resp.headers["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS"
        resp.headers["Access-Control-Allow-Headers"] = "Authorization,Content-Type,Origin"

    # 
    # X-Content-Type-Options, X-Frame-Options, Content-Security-Policy).
    return resp


# --- Routeurs ----------------------------------------------------------------
app.include_router(oidc.router)
app.include_router(portfolios.router)
app.include_router(orders.router)
app.include_router(instruments.router)
app.include_router(extras.router)
app.include_router(secure.router)
app.include_router(admin.router)


# --- SPA React (statique) ----------------------------------------------------
if os.path.isdir(STATIC_DIR):
    app.mount("/static", StaticFiles(directory=STATIC_DIR), name="static")


@app.get("/")
def spa_index():
    # GET / sert la SPA ; reponse sans en-tete de securite (V014) et avec banniere (V016).
    index = os.path.join(STATIC_DIR, "index.html")
    if os.path.isfile(index):
        return FileResponse(index, media_type="text/html")
    return Response("<h1>Novabourse — laboratoire</h1>", media_type="text/html")


@app.get("/callback")
def spa_callback():
    # Route de callback de la SPA (traitee c^ote client en PKCE).
    index = os.path.join(STATIC_DIR, "index.html")
    if os.path.isfile(index):
        return FileResponse(index, media_type="text/html")
    return Response("<h1>Novabourse — callback</h1>", media_type="text/html")
