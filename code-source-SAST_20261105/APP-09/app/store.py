# -*- coding: utf-8 -*-
"""
Depot de laboratoire : etat en memoire + table SQLite des instruments.

- Donnees 100% FICTIVES, reinitialisables via POST /admin/reset.
- La table SQLite `instruments` sert de surface realiste pour l'injection SQL
  (APP09-V009) et pour la recherche parametree securisee (NC APP09-N006).
- Aucun secret reel ; aucune base externe (profil standalone).
"""
import sqlite3
import threading
import time

# --- Etat en memoire (reconstruit par seed()) --------------------------------
USERS = {}          # username -> {sub, role, password, accounts:[...], portfolios:[...]}
PORTFOLIOS = {}     # pf_id -> {id, ownerId, cash, positions:[...], name}
ACCOUNTS = {}       # acc_id -> {id, ownerId}
ORDERS = {}         # ord_id -> {id, accountId, instrument, side, quantity, price, status, createdBy, createdAt}
AUTH_CODES = {}     # code -> {sub, client_id, redirect_uri, code_challenge, method, nonce, exp_offset, used}
SESSIONS = {}       # sid -> {sub, state_bound}

_ORDER_SEQ = {"n": 3002}
_LOCK = threading.Lock()

# SQLite partagee (meme thread desactive : FastAPI sert en pool de threads).
_DB = sqlite3.connect(":memory:", check_same_thread=False)
_DB.row_factory = sqlite3.Row


# --- Instruments fictifs -----------------------------------------------------
# NB : aucun nouveau code/nom ne doit se terminer par "NOVA" (l'oracle SQLi
# APP09-V009 attend exactement 1 ligne pour q=NOVA et davantage pour la charge).
_INSTRUMENTS_SEED = [
    ("FR-NOVA", "Novabourse Industries", 42.50),
    ("FR-ALFA", "Alfa Capital", 15.20),
    ("FR-BETA", "Beta Energie", 88.10),
    ("DE-GAMM", "Gamma Werke", 120.00),
    ("US-DELT", "Delta Systems", 250.75),
    # --- volume supplementaire (donnees 100% fictives) ---
    ("FR-EPSI", "Epsilon Telecom", 9.80),
    ("FR-ZETA", "Zeta Assurances", 61.40),
    ("FR-ETA0", "Eta Logistique", 27.15),
    ("DE-THET", "Theta Automobil", 143.90),
    ("DE-IOTA", "Iota Chemie", 54.60),
    ("US-KAPP", "Kappa Cloud", 318.20),
    ("US-LAMB", "Lambda Robotics", 205.05),
    ("GB-MUMU", "Mu Holdings", 33.70),
    ("GB-NUNU", "Nu Pharma", 77.25),
    ("CH-XICO", "Xi Private Bank", 410.00),
]


def _seed_sqlite():
    cur = _DB.cursor()
    cur.execute("DROP TABLE IF EXISTS instruments")
    cur.execute(
        "CREATE TABLE instruments (code TEXT PRIMARY KEY, name TEXT NOT NULL, lastPrice REAL NOT NULL)"
    )
    cur.executemany(
        "INSERT INTO instruments (code, name, lastPrice) VALUES (?, ?, ?)",
        _INSTRUMENTS_SEED,
    )
    _DB.commit()


def next_order_id():
    with _LOCK:
        _ORDER_SEQ["n"] += 1
        return f"ORD-{_ORDER_SEQ['n']}"


def seed():
    """(Re)initialise l'integralite de l'etat de demonstration."""
    with _LOCK:
        USERS.clear(); PORTFOLIOS.clear(); ACCOUNTS.clear(); ORDERS.clear()
        AUTH_CODES.clear(); SESSIONS.clear()
        _ORDER_SEQ["n"] = 3002

        # NB volume : les comptes/portefeuilles/ordres DETERMINISTES attendus par le
        # validateur (invest-a/ACC-2001/PF-1001, invest-b/ACC-2002/PF-1002, backoffice,
        # ORD-3001 EXECUTED, ORD-3002 PENDING, ORD-3003) restent STRICTEMENT inchanges.
        # Les enregistrements supplementaires sont 100% fictifs (emails/telephones en
        # @novabourse.lab.exemple) et n'empietent pas sur la sequence d'IDs d'ordres.
        USERS.update({
            "invest-a": {"sub": "invest-a", "role": "INVESTOR", "password": "Password1!",
                         "email": "invest-a@novabourse.lab.exemple", "phone": "+33-100-000-001",
                         "accounts": ["ACC-2001"], "portfolios": ["PF-1001"]},
            "invest-b": {"sub": "invest-b", "role": "INVESTOR", "password": "Password1!",
                         "email": "invest-b@novabourse.lab.exemple", "phone": "+33-100-000-002",
                         "accounts": ["ACC-2002"], "portfolios": ["PF-1002"]},
            "backoffice": {"sub": "backoffice", "role": "BACKOFFICE", "password": "Password1!",
                           "email": "backoffice@novabourse.lab.exemple", "phone": "+33-100-000-009",
                           "accounts": [], "portfolios": []},
        })

        PORTFOLIOS.update({
            "PF-1001": {"id": "PF-1001", "ownerId": "invest-a", "name": "Portefeuille PEA",
                        "cash": 12500.00,
                        "positions": [{"instrument": "FR-NOVA", "quantity": 120, "avgPrice": 41.80}]},
            "PF-1002": {"id": "PF-1002", "ownerId": "invest-b", "name": "Portefeuille CTO",
                        "cash": 8000.00,
                        "positions": [{"instrument": "FR-BETA", "quantity": 30, "avgPrice": 85.00}]},
        })

        ACCOUNTS.update({
            "ACC-2001": {"id": "ACC-2001", "ownerId": "invest-a"},
            "ACC-2002": {"id": "ACC-2002", "ownerId": "invest-b"},
        })

        now = int(time.time())
        ORDERS.update({
            "ORD-3001": {"id": "ORD-3001", "accountId": "ACC-2001", "instrument": "FR-NOVA",
                         "side": "BUY", "quantity": 100, "price": 41.80, "status": "EXECUTED",
                         "createdBy": "invest-a", "createdAt": now},
            "ORD-3002": {"id": "ORD-3002", "accountId": "ACC-2001", "instrument": "FR-NOVA",
                         "side": "SELL", "quantity": 10, "price": 43.00, "status": "PENDING",
                         "createdBy": "invest-a", "createdAt": now},
            # Ordre d'un autre compte (invest-b) : rend observable le BFLA (APP09-V011),
            # ou un INVESTOR voit via /api/admin/orders des ordres qui ne sont pas les siens.
            "ORD-3003": {"id": "ORD-3003", "accountId": "ACC-2002", "instrument": "FR-BETA",
                         "side": "BUY", "quantity": 20, "price": 85.00, "status": "PENDING",
                         "createdBy": "invest-b", "createdAt": now},
        })

        _seed_volume(now)
        _seed_sqlite()


# --- Volume fictif supplementaire -------------------------------------------
# Comptes/portefeuilles/ordres additionnels pour une surface de crawl realiste.
# Les IDs d'ordres fictifs utilisent la plage ORD-7xxx afin de NE JAMAIS entrer en
# collision avec la sequence next_order_id() (ORD-3003, ORD-3004, ...).
_EXTRA_INVESTORS = [
    # (sub, acc, pf, pf_name, cash, instrument, qty, avg, numero)
    ("invest-c", "ACC-2003", "PF-1003", "Portefeuille PEA-PME", 4200.00, "FR-ALFA", 60, 14.90, "003"),
    ("invest-d", "ACC-2004", "PF-1004", "Portefeuille Assurance-vie", 31500.00, "DE-GAMM", 15, 118.00, "004"),
    ("invest-e", "ACC-2005", "PF-1005", "Portefeuille Croissance", 980.00, "US-DELT", 8, 240.10, "005"),
    ("invest-f", "ACC-2006", "PF-1006", "Portefeuille Dividende", 15750.00, "FR-ZETA", 40, 59.00, "006"),
    ("invest-g", "ACC-2007", "PF-1007", "Portefeuille Tech", 6300.00, "US-KAPP", 5, 300.00, "007"),
    ("invest-h", "ACC-2008", "PF-1008", "Portefeuille Defensif", 22000.00, "GB-NUNU", 70, 75.00, "008"),
    ("invest-i", "ACC-2009", "PF-1009", "Portefeuille Europe", 8450.00, "DE-THET", 12, 140.00, "010"),
    ("invest-j", "ACC-2010", "PF-1010", "Portefeuille Monde", 51200.00, "CH-XICO", 3, 405.00, "011"),
]

_EXTRA_ORDER_STATUSES = ["EXECUTED", "PENDING", "CANCELLED", "REJECTED", "PARTIAL"]


def _seed_volume(now):
    for sub, acc, pf, pf_name, cash, instr, qty, avg, num in _EXTRA_INVESTORS:
        USERS[sub] = {
            "sub": sub, "role": "INVESTOR", "password": "Password1!",
            "email": f"{sub}@novabourse.lab.exemple", "phone": f"+33-100-000-{num}",
            "accounts": [acc], "portfolios": [pf],
        }
        ACCOUNTS[acc] = {"id": acc, "ownerId": sub}
        PORTFOLIOS[pf] = {
            "id": pf, "ownerId": sub, "name": pf_name, "cash": cash,
            "positions": [{"instrument": instr, "quantity": qty, "avgPrice": avg}],
        }

    # Historique d'ordres fictif (plage ORD-7xxx, hors sequence next_order_id()).
    seq = 7001
    for sub, acc, pf, pf_name, cash, instr, qty, avg, num in _EXTRA_INVESTORS:
        for k in range(2):
            oid = f"ORD-{seq}"
            seq += 1
            ORDERS[oid] = {
                "id": oid, "accountId": acc, "instrument": instr,
                "side": "BUY" if k == 0 else "SELL",
                "quantity": qty // (k + 2) or 1,
                "price": round(avg * (1.0 + 0.01 * (k - 1)), 2),
                "status": _EXTRA_ORDER_STATUSES[(seq + k) % len(_EXTRA_ORDER_STATUSES)],
                "createdBy": sub, "createdAt": now - 86400 * (k + 1),
            }


def db():
    return _DB
