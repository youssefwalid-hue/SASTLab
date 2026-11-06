import { Body, Controller, Post, Req, Res, HttpStatus } from '@nestjs/common';
import type { Request, Response } from 'express';
import { SessionService } from '../auth/session.service';

/**
 * Enrichissement de dossier (endpoint frere de la recuperation de piece).
 */
@Controller()
export class EnrichmentController {
  // Allowlist stricte de domaines publics autorises (donnees fictives de labo).
  private static readonly ALLOWED_HOSTS = new Set(['cdn.exemple', 'logos.exemple']);

  constructor(private readonly sessions: SessionService) {}

  /**
   * NC: APP10-N003 — recuperation d'un logo d'entreprise AVEC allowlist et blocage des plages
   * internes : une URL interne (http://kyc-internal:8080/...) est refusee (400), aucun appel
   * sortant vers le reseau interne n'est emis.
   */
  @Post('/api/enrichment/company-logo')
  async companyLogo(@Req() req: Request, @Res() res: Response, @Body() body: any) {
    const user = this.sessions.current(req);
    if (!user) return res.status(HttpStatus.UNAUTHORIZED).json({ error: 'authentification requise' });
    const logoUrl = String(body?.logoUrl ?? '');
    let parsed: URL;
    try {
      parsed = new URL(logoUrl);
    } catch {
      return res.status(HttpStatus.BAD_REQUEST).json({ error: 'URL invalide' });
    }
    // NC: APP10-N003 — protocole https + allowlist de domaines ; tout hote interne/IP privee refuse.
    const host = parsed.hostname.toLowerCase();
    const isIpLike = /^[0-9.]+$/.test(host) || host.includes(':');
    if (parsed.protocol !== 'https:' || isIpLike || !EnrichmentController.ALLOWED_HOSTS.has(host)) {
      return res.status(HttpStatus.BAD_REQUEST)
        .json({ error: 'domaine non autorise', allowed: [...EnrichmentController.ALLOWED_HOSTS] });
    }
    // Hote autorise : en labo on ne telecharge pas reellement, on confirme l'acceptation.
    return res.status(HttpStatus.OK).json({ status: 'accepted', host });
  }
}
