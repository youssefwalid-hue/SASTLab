#!/usr/bin/env python
# -*- coding: utf-8 -*-
# CréditFlux — Portail de crédit (Banking DAST Benchmark Lab).
# Environnement de test — DONNÉES 100 % FICTIVES — usage défensif.
import os
import sys


def main():
    os.environ.setdefault("DJANGO_SETTINGS_MODULE", "creditflux.settings")
    try:
        from django.core.management import execute_from_command_line
    except ImportError as exc:
        raise ImportError(
            "Django est introuvable. Installez-le : pip install -r requirements.txt"
        ) from exc
    execute_from_command_line(sys.argv)


if __name__ == "__main__":
    main()
