# -*- coding: utf-8 -*-
"""
Schemas Pydantic (DTO) de l'API.

- Les modeles v1 sont volontairement permissifs la ou un ecart est attendu
  (ex. `OrderCreateV1.price` sans borne — APP09-V005).
- Les modeles v2 sont stricts (bornes, `extra=forbid`) pour les contr^oles negatifs.
"""
from typing import Optional

from pydantic import BaseModel, ConfigDict, Field


class OrderCreateV1(BaseModel):
    # v1 : permissif. `price` accepte tel quel (aucune borne) -> APP09-V005.
    accountId: str
    instrument: str
    side: str = "BUY"
    quantity: int = 1
    price: float


class OrderCreateV2(BaseModel):
    # v2 : strict. Champs hors contrat rejetes (422), prix strictement positif.
    model_config = ConfigDict(extra="forbid")
    accountId: Optional[str] = None  # ignore : le compte est derive du jeton
    instrument: str
    side: str = Field(default="BUY", pattern="^(BUY|SELL)$")
    quantity: int = Field(gt=0, le=1_000_000)
    price: float = Field(gt=0, le=1_000_000)


class PortfolioCreate(BaseModel):
    # v1 : le nom est stocke tel quel (rendu non echappe c^ote React) -> APP09-V012.
    name: str
