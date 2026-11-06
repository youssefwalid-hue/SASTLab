# -*- coding: utf-8 -*-
"""
API Recherche d'instruments (v1). Ecart localise : injection SQL (V009).
"""
from fastapi import APIRouter, Depends, Query

from .. import security, store

router = APIRouter(prefix="/api", tags=["instruments"])


@router.get("/instruments")
def search_instruments(q: str = Query(default=""), payload: dict = Depends(security.jwt_sig_only)):
    """
    Le terme `q` est CONCATENE directement dans la requete SQL (aucune liaison de
    parametre) : `' OR '1'='1` neutralise le filtre, une charge UNION fuit le schema.
    """
    security.user_of(payload)  # authentifie
    con = store.db()
    cur = con.cursor()
    # <-- concatenation de chaine : point d'injection volontaire.
    # Le terme est appose en fin de clause LIKE : une charge `' OR '1'='1`
    # ferme le litteral et introduit une tautologie neutralisant le filtre.
    sql = "SELECT code, name, lastPrice FROM instruments WHERE name LIKE '%" + q + "' OR code LIKE '%" + q + "'"
    try:
        rows = cur.execute(sql).fetchall()
    except Exception as e:
        # Erreur SQL renvoyee (comportement typique exploitable par un DAST).
        return {"error": "sql_error", "detail": str(e), "query": sql}
    return [dict(r) for r in rows]
