import { Body, Controller, Get, Post, Query, Req, Res, HttpStatus } from '@nestjs/common';
import type { Request, Response } from 'express';
import { SessionService } from '../auth/session.service';

/**
 * Support KYC (endpoint frere portant un corps pour le pendant du smuggling) + aide/recherche.
 */
@Controller()
export class SupportController {
  constructor(private readonly sessions: SessionService) {}

  /**
   * NC: APP10-N004 — requete ambigue CL+TE rejetee : toute requete presentant a la fois
   * Content-Length ET Transfer-Encoding est refusee (400). Pas de desynchronisation possible.
   */
  @Post('/api/support/messages')
  createMessage(@Req() req: Request, @Res() res: Response, @Body() body: any) {
    const hasCL = req.headers['content-length'] !== undefined;
    const hasTE = req.headers['transfer-encoding'] !== undefined;
    // NC: APP10-N004 — rejet explicite des en-tetes de longueur conflictuels.
    if (hasCL && hasTE) {
      return res.status(HttpStatus.BAD_REQUEST).json({ error: 'en-tetes de longueur conflictuels (CL+TE)' });
    }
    return res.status(HttpStatus.CREATED).json({ status: 'recu', message: String(body?.message ?? '') });
  }

  // ===========================================================================
  // ===========================================================================
  /**
   * Recherche dans l'aide (rendu HTML).
   *
   * (text/html) SANS echappement : une charge <script>...</script> est executee dans le
   * navigateur de la victime (XSS reflechi). Endpoint porte par le service documents (Node).
   */
  @Get('/api/support/search')
  search(@Res() res: Response, @Query('q') q: string) {
    const term = String(q ?? '');
    const body = `<!doctype html><html lang="fr"><head><meta charset="utf-8">`
      + `<title>Recherche aide</title></head><body>`
      + `<div class="lab-banner">Environnement de test — donnees fictives</div>`
      + `<h1>Resultats pour : ${term}</h1>`
      + `<p>Aucun article d'aide fictif ne correspond.</p>`
      + `</body></html>`;
    res.setHeader('Content-Type', 'text/html; charset=utf-8');
    return res.status(HttpStatus.OK).send(body);
  }

  /**
   * NC: APP10-N010 — recherche frere AVEC echappement HTML : le terme `q` est echappe avant
   * insertion dans la page ; une charge <script> apparait inerte (texte), pas d'XSS.
   */
  @Get('/api/support/search/safe')
  searchSafe(@Res() res: Response, @Query('q') q: string) {
    const term = String(q ?? '');
    // NC: APP10-N010 — echappement des metacaracteres HTML avant rendu.
    const escaped = term
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    const body = `<!doctype html><html lang="fr"><head><meta charset="utf-8">`
      + `<title>Recherche aide</title></head><body>`
      + `<div class="lab-banner">Environnement de test — donnees fictives</div>`
      + `<h1>Resultats pour : ${escaped}</h1>`
      + `<p>Aucun article d'aide fictif ne correspond.</p>`
      + `</body></html>`;
    res.setHeader('Content-Type', 'text/html; charset=utf-8');
    return res.status(HttpStatus.OK).send(body);
  }

  // --- Use cases BENINS (surface de crawl) — auth exigee, sortie statique/echappee ------

  /** Aide / FAQ (benin). */
  @Get('/api/support/faq')
  faq(@Req() req: Request, @Res() res: Response) {
    const user = this.sessions.current(req);
    if (!user) return res.status(HttpStatus.UNAUTHORIZED).json({ error: 'authentification requise' });
    return res.status(HttpStatus.OK).json({
      faq: [
        { q: 'Comment deposer un justificatif ?', a: 'Depuis votre dossier, rubrique Pieces (fictif).' },
        { q: 'Combien de temps pour la validation ?', a: '48h ouvrees (fictif).' },
      ],
    });
  }

  /** A propos / statut du service documents (benin). */
  @Get('/api/support/status')
  status(@Req() req: Request, @Res() res: Response) {
    const user = this.sessions.current(req);
    if (!user) return res.status(HttpStatus.UNAUTHORIZED).json({ error: 'authentification requise' });
    return res.status(HttpStatus.OK).json({ service: 'documents', status: 'ok', environment: 'test-data-fictives' });
  }
}
