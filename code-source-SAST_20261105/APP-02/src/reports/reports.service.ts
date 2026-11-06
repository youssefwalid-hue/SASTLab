import { Inject, Injectable, OnModuleInit } from '@nestjs/common';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'fs';
import { join, normalize, basename, sep } from 'path';
import { tmpdir } from 'os';
import { LAB_STORE } from '../common/store/store.module';
import { Consent, LabStore } from '../common/store/store.interface';

/**
 * Service de rapports/exports fichier.
 *
 * Prépare au démarrage une arborescence de laboratoire sur disque (données 100 % FICTIVES) :
 *   <tmp>/agrega-lab/reports/     — fichiers d'export légitimes, servis par /v1/reports/download
 *   <tmp>/agrega-lab/internal/    — fichier interne FICTIF (hors périmètre de téléchargement)
 *
 * Le fichier interne (clé de signature fictive) sert à démontrer la traversée de chemin
 * (GT: APP02-V019) : il ne contient AUCUN secret réel.
 */
@Injectable()
export class ReportsService implements OnModuleInit {
  static readonly ROOT = join(tmpdir(), 'agrega-lab');
  static readonly REPORTS_DIR = join(ReportsService.ROOT, 'reports');
  static readonly INTERNAL_DIR = join(ReportsService.ROOT, 'internal');

  /** Fichiers d'export autorisés (référence sûre, NC: APP02-N011). */
  static readonly ALLOWED_FILES = [
    'agrega-export-alice-2026-09.json',
    'agrega-export-bruno-2026-09.json',
    'agrega-export-alice-2026-10.json',
  ];

  constructor(@Inject(LAB_STORE) private readonly store: LabStore) {}

  onModuleInit(): void {
    mkdirSync(ReportsService.REPORTS_DIR, { recursive: true });
    mkdirSync(ReportsService.INTERNAL_DIR, { recursive: true });

    // Fichiers d'export légitimes (fictifs).
    writeFileSync(
      join(ReportsService.REPORTS_DIR, 'agrega-export-alice-2026-09.json'),
      JSON.stringify({ holder: 'alice', accounts: ['ACC-FR-1001'], totalBalance: 4231.57 }, null, 2),
    );
    writeFileSync(
      join(ReportsService.REPORTS_DIR, 'agrega-export-bruno-2026-09.json'),
      JSON.stringify({ holder: 'bruno', accounts: ['ACC-FR-2001'], totalBalance: 8820.0 }, null, 2),
    );
    writeFileSync(
      join(ReportsService.REPORTS_DIR, 'agrega-export-alice-2026-10.json'),
      JSON.stringify({ holder: 'alice', accounts: ['ACC-FR-1001', 'ACC-FR-1002'], totalBalance: 19461.99 }, null, 2),
    );

    // Fichier INTERNE fictif, hors du dossier de téléchargement (cible de traversée).
    writeFileSync(
      join(ReportsService.INTERNAL_DIR, 'idp-signing.key'),
      'LAB-FICTIVE-SIGNING-KEY: ceci est une clé de LABORATOIRE 100% fictive, aucun secret réel.\n',
    );
  }

  /**
   * Lecture de fichier VULNÉRABLE (GT: APP02-V019).
   * Le nom fourni est concaténé au dossier sans aucune sanitation : une séquence
   * `../` permet de sortir du dossier de rapports (traversée de chemin).
   */
  readUnsafe(name: string): string {
    const full = join(ReportsService.REPORTS_DIR, name); 
    return readFileSync(full, 'utf8');
  }

  /**
   * Lecture de fichier SÛRE (référence, NC: APP02-N011).
   * Refuse toute séquence de traversée et restreint à une allowlist de noms de base.
   */
  readSafe(name: string): string | null {
    if (!name || name.includes('..') || name.includes('/') || name.includes('\\')) {
      return null; // NC: APP02-N011 — traversée explicitement refusée
    }
    const base = basename(name);
    if (!ReportsService.ALLOWED_FILES.includes(base)) {
      return null;
    }
    const full = normalize(join(ReportsService.REPORTS_DIR, base));
    // Défense en profondeur : le chemin résolu doit rester dans le dossier de rapports.
    if (!full.startsWith(ReportsService.REPORTS_DIR + sep)) {
      return null;
    }
    if (!existsSync(full)) {
      return null;
    }
    return readFileSync(full, 'utf8');
  }

  // --- Export CSV des consentements du TPP -------------------------------------
  private consentsForClient(clientId: string): Consent[] {
    return [...this.store.consents.values()].filter((c) => c.tpp === clientId);
  }

  /**
   * Export CSV VULNÉRABLE (GT: APP02-V020).
   * Les valeurs issues des données ne sont PAS neutralisées : une cellule débutant par
   * `=`, `+`, `-` ou `@` sera interprétée comme une formule par un tableur (CSV injection).
   */
  buildCsvUnsafe(clientId: string): string {
    const header = 'consentId,tpp,user,grantedScopes';
    const rows = this.consentsForClient(clientId).map((c) => {
      const cells = [c.consentId, c.tpp, c.user, (c.grantedScopes || []).join(' ')];
      return cells.map((v) => this.csvQuote(String(v))).join(',');
    });
    return [header, ...rows].join('\r\n');
  }

  /**
   * Export CSV SÛR (référence, NC: APP02-N012).
   * Les cellules débutant par un caractère de formule sont préfixées d'une apostrophe.
   */
  buildCsvSafe(clientId: string): string {
    const header = 'consentId,tpp,user,grantedScopes';
    const rows = this.consentsForClient(clientId).map((c) => {
      const cells = [c.consentId, c.tpp, c.user, (c.grantedScopes || []).join(' ')];
      return cells.map((v) => this.csvQuote(this.neutralizeFormula(String(v)))).join(',');
    });
    return [header, ...rows].join('\r\n');
  }

  /** Neutralise une cellule susceptible d'être interprétée comme une formule (NC: APP02-N012). */
  private neutralizeFormula(value: string): string {
    if (/^[=+\-@\t\r]/.test(value)) {
      return `'${value}`;
    }
    return value;
  }

  /** Échappement CSV minimal (guillemets si séparateur/guillemet/retour présent). */
  private csvQuote(value: string): string {
    if (/[",\r\n]/.test(value)) {
      return `"${value.replace(/"/g, '""')}"`;
    }
    return value;
  }
}
