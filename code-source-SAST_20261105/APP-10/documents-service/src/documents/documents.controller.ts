import {
  Body, Controller, Get, Param, Post, Req, Res,
  UploadedFile, UseInterceptors, HttpStatus,
} from '@nestjs/common';
import { FileInterceptor } from '@nestjs/platform-express';
import type { Request, Response } from 'express';
import * as crypto from 'crypto';
import { DocStore } from '../common/doc-store.service';
import { SessionService } from '../auth/session.service';

/**
 * Pieces justificatives KYC. Securise par defaut SAUF aux ecarts tagues // GT: APP10-Vxxx.
 */
@Controller()
export class DocumentsController {
  constructor(private readonly store: DocStore, private readonly sessions: SessionService) {}

  /**
   * Recuperation d'une piece depuis une URL fournie par l'utilisateur (enrichissement dossier).
   *
   * ni blocage des plages internes/noms d'hotes Docker ; la reponse interne est stockee et
   * restituee comme un justificatif.
   */
  @Post('/api/documents/fetch')
  async fetchDocument(@Req() req: Request, @Res() res: Response, @Body() body: any) {
    const user = this.sessions.current(req);
    if (!user) return res.status(HttpStatus.UNAUTHORIZED).json({ error: 'authentification requise' });
    const onboardingId = parseInt(String(body?.onboardingId ?? '0'), 10);
    const url = String(body?.url ?? '');
    if (!url) return res.status(HttpStatus.BAD_REQUEST).json({ error: 'url requise' });

    // le serveur emet la requete vers l'URL arbitraire fournie par le client.
    let status = 0;
    let preview = '';
    let contentType = 'text/plain';
    try {
      const r = await fetch(url, { method: 'GET', signal: AbortSignal.timeout(5000) });
      status = r.status;
      contentType = r.headers.get('content-type') || 'text/plain';
      preview = (await r.text()).slice(0, 2000);
    } catch (e: any) {
      preview = `fetch-error: ${e?.name || 'Error'}`;
    }
    const docId = this.store.nextId(onboardingId);
    this.store.put({
      id: docId, onboardingId, owner: user.username,
      filename: 'fetched', contentType, content: preview, source: 'fetch',
    });
    return res.status(HttpStatus.CREATED).json({ docId, fetched: { url, status, contentType, preview } });
  }

  /**
   * Depot multipart d'une piece d'identite.
   *
   * ne sont controles ; le fichier est accepte et stocke tel quel avec son type d'origine.
   */
  @Post('/api/onboarding/:id/documents')
  @UseInterceptors(FileInterceptor('file'))
  uploadDocument(@Req() req: Request, @Res() res: Response,
                 @Param('id') id: string, @UploadedFile() file: Express.Multer.File) {
    const user = this.sessions.current(req);
    if (!user) return res.status(HttpStatus.UNAUTHORIZED).json({ error: 'authentification requise' });
    const onboardingId = parseInt(id, 10);
    if (!this.store.knownOnboarding.has(onboardingId)) {
      return res.status(HttpStatus.NOT_FOUND).json({ error: 'dossier introuvable' });
    }
    if (!file) return res.status(HttpStatus.BAD_REQUEST).json({ error: 'fichier requis' });
    const docId = this.store.nextId(onboardingId);
    this.store.put({
      id: docId, onboardingId, owner: user.username,
      filename: file.originalname, contentType: file.mimetype,
      content: file.buffer ? file.buffer.toString('utf8').slice(0, 4000) : '', source: 'upload',
    });
    return res.status(HttpStatus.CREATED).json({
      docId, filename: file.originalname, contentType: file.mimetype, size: file.size,
    });
  }

  /** Liste des pieces d'un dossier (necessite une session). */
  @Get('/api/onboarding/:id/documents')
  listDocuments(@Req() req: Request, @Res() res: Response, @Param('id') id: string) {
    const user = this.sessions.current(req);
    if (!user) return res.status(HttpStatus.UNAUTHORIZED).json({ error: 'authentification requise' });
    const onboardingId = parseInt(id, 10);
    const docs = this.store.listForOnboarding(onboardingId).map((d) => ({
      id: d.id, filename: d.filename, contentType: d.contentType, source: d.source,
    }));
    return res.status(HttpStatus.OK).json({ onboardingId, documents: docs });
  }

  /** Recupere le contenu d'une piece par son id dans le contexte d'un dossier. */
  @Get('/api/onboarding/:id/documents/:docId')
  getDocumentInOnboarding(@Req() req: Request, @Res() res: Response,
                          @Param('id') id: string, @Param('docId') docId: string) {
    const user = this.sessions.current(req);
    if (!user) return res.status(HttpStatus.UNAUTHORIZED).json({ error: 'authentification requise' });
    const doc = this.store.get(docId);
    if (!doc || doc.onboardingId !== parseInt(id, 10)) {
      return res.status(HttpStatus.NOT_FOUND).json({ error: 'piece introuvable' });
    }
    // Restitution avec le type stocke (demontre l'acceptation de contenus non prevus, V007).
    res.setHeader('Content-Type', doc.contentType);
    res.setHeader('Content-Disposition', `attachment; filename="${doc.filename}"`);
    return res.status(HttpStatus.OK).send(doc.content);
  }

  /**
   * NC: APP10-N005 — piece frere adressee par id AVEC controle de propriete : acceder a la
   * piece d'un autre prospect renvoie 403/404 (le service verifie doc.owner == utilisateur).
   */
  @Get('/api/documents/:docId')
  getDocumentById(@Req() req: Request, @Res() res: Response, @Param('docId') docId: string) {
    const user = this.sessions.current(req);
    if (!user) return res.status(HttpStatus.UNAUTHORIZED).json({ error: 'authentification requise' });
    const doc = this.store.get(docId);
    if (!doc) return res.status(HttpStatus.NOT_FOUND).json({ error: 'piece introuvable' });
    // NC: APP10-N005 — controle de propriete : refus si la piece n'appartient pas a l'appelant.
    if (doc.owner !== user.username) {
      return res.status(HttpStatus.FORBIDDEN).json({ error: 'acces refuse' });
    }
    res.setHeader('Content-Type', doc.contentType);
    res.setHeader('Content-Disposition', `attachment; filename="${doc.filename}"`);
    return res.status(HttpStatus.OK).send(doc.content);
  }

  /**
   * NC: APP10-N006 — depot de selfie KYC AVEC validation du type reel (magic bytes) contre
   * une allowlist image ; un fichier .html/.svg/.jsp est rejete (415).
   */
  @Post('/api/onboarding/:id/selfie')
  @UseInterceptors(FileInterceptor('file'))
  uploadSelfie(@Req() req: Request, @Res() res: Response,
               @Param('id') id: string, @UploadedFile() file: Express.Multer.File) {
    const user = this.sessions.current(req);
    if (!user) return res.status(HttpStatus.UNAUTHORIZED).json({ error: 'authentification requise' });
    if (!file) return res.status(HttpStatus.BAD_REQUEST).json({ error: 'fichier requis' });
    // NC: APP10-N006 — validation du type MIME REEL via magic bytes (JPEG/PNG uniquement).
    const buf = file.buffer || Buffer.alloc(0);
    const isJpeg = buf.length >= 3 && buf[0] === 0xff && buf[1] === 0xd8 && buf[2] === 0xff;
    const isPng = buf.length >= 8 && buf[0] === 0x89 && buf[1] === 0x50 && buf[2] === 0x4e && buf[3] === 0x47;
    if (!isJpeg && !isPng) {
      return res.status(HttpStatus.UNSUPPORTED_MEDIA_TYPE)
        .json({ error: 'type non autorise : image JPEG/PNG requise' });
    }
    const onboardingId = parseInt(id, 10);
    const docId = this.store.nextId(onboardingId);
    // Renommage + type force (ne jamais conserver un type/extension fournis par le client).
    this.store.put({
      id: docId, onboardingId, owner: user.username,
      filename: `${docId}.${isPng ? 'png' : 'jpg'}`,
      contentType: isPng ? 'image/png' : 'image/jpeg',
      content: '[selfie binaire fictif]', source: 'upload',
    });
    return res.status(HttpStatus.CREATED).json({ docId, contentType: isPng ? 'image/png' : 'image/jpeg' });
  }

  // ===========================================================================
  // ===========================================================================
  /**
   * Import d'une piece depuis une assertion inter-service.
   *
   * Dans le modele multi-services, le service onboarding (Java) peut agir au nom d'un
   * utilisateur en presentant une assertion JWT portee par l'en-tete X-Service-Token
   * (claims { sub, role }). Le service documents DEVRAIT verifier la signature HS256 de ce
   * jeton avec le secret partage.
   *
   * quelconque) est accepte tel quel. Un client peut forger l'identite/le role inter-service
   * (ex. role CONFORMITE, sub d'un autre prospect) a travers la frontiere de service.
   */
  @Post('/api/documents/import')
  importDocument(@Req() req: Request, @Res() res: Response, @Body() body: any) {
    const token = String(req.headers['x-service-token'] || '');
    const claims = this.decodeJwtUnverified(token);
    if (!claims) return res.status(HttpStatus.BAD_REQUEST).json({ error: 'assertion inter-service requise' });
    const onboardingId = parseInt(String(body?.onboardingId ?? '0'), 10);
    const docId = this.store.nextId(onboardingId);
    this.store.put({
      id: docId, onboardingId, owner: String(claims.sub ?? 'inconnu'),
      filename: String(body?.filename ?? 'import'), contentType: 'application/pdf',
      content: 'Import inter-service (fictif)', source: 'import',
    });
    return res.status(HttpStatus.CREATED).json({
      docId, acceptedAs: { sub: claims.sub, role: claims.role }, verified: false,
    });
  }

  /**
   * NC: APP10-N012 — import frere AVEC verification de signature : l'assertion X-Service-Token
   * est acceptee UNIQUEMENT si elle est signee en HS256 avec le secret partage. Un `alg: none`
   * ou une signature invalide est rejete (401). Pas de forge d'identite inter-service.
   */
  @Post('/api/documents/import/verified')
  importVerified(@Req() req: Request, @Res() res: Response, @Body() body: any) {
    const token = String(req.headers['x-service-token'] || '');
    const claims = this.verifyJwtHs256(token);
    // NC: APP10-N012 — rejet si signature absente/invalide/alg non HS256.
    if (!claims) return res.status(HttpStatus.UNAUTHORIZED).json({ error: 'assertion inter-service invalide' });
    const onboardingId = parseInt(String(body?.onboardingId ?? '0'), 10);
    const docId = this.store.nextId(onboardingId);
    this.store.put({
      id: docId, onboardingId, owner: String(claims.sub ?? 'inconnu'),
      filename: String(body?.filename ?? 'import'), contentType: 'application/pdf',
      content: 'Import inter-service verifie (fictif)', source: 'import',
    });
    return res.status(HttpStatus.CREATED).json({
      docId, acceptedAs: { sub: claims.sub, role: claims.role }, verified: true,
    });
  }

  // ===========================================================================
  // ===========================================================================
  /**
   * Detail "complet" d'une piece.
   *
   * back-office ownerEmail / reviewerNotes / internalRef compris, alors que le prospect ne
   * devrait voir que les champs publics. Le controle de propriete est present (ce n'est pas un
   * IDOR) : c'est bien l'exces de donnees renvoyees au proprietaire legitime qui est en cause.
   */
  @Get('/api/documents/:docId/details')
  documentDetails(@Req() req: Request, @Res() res: Response, @Param('docId') docId: string) {
    const user = this.sessions.current(req);
    if (!user) return res.status(HttpStatus.UNAUTHORIZED).json({ error: 'authentification requise' });
    const doc = this.store.get(docId);
    if (!doc) return res.status(HttpStatus.NOT_FOUND).json({ error: 'piece introuvable' });
    if (doc.owner !== user.username) {
      return res.status(HttpStatus.FORBIDDEN).json({ error: 'acces refuse' });
    }
    return res.status(HttpStatus.OK).json(doc);
  }

  /**
   * NC: APP10-N013 — detail frere AVEC vue filtree : seuls les champs publics d'une piece sont
   * renvoyes (DTO de sortie en allowlist) ; les champs internes (ownerEmail/reviewerNotes/
   * internalRef) ne sont jamais serialises.
   */
  @Get('/api/documents/:docId/summary')
  documentSummary(@Req() req: Request, @Res() res: Response, @Param('docId') docId: string) {
    const user = this.sessions.current(req);
    if (!user) return res.status(HttpStatus.UNAUTHORIZED).json({ error: 'authentification requise' });
    const doc = this.store.get(docId);
    if (!doc) return res.status(HttpStatus.NOT_FOUND).json({ error: 'piece introuvable' });
    if (doc.owner !== user.username) {
      return res.status(HttpStatus.FORBIDDEN).json({ error: 'acces refuse' });
    }
    // NC: APP10-N013 — projection explicite des seuls champs publics.
    return res.status(HttpStatus.OK).json({
      id: doc.id, onboardingId: doc.onboardingId,
      filename: doc.filename, contentType: doc.contentType, source: doc.source,
    });
  }

  // --- Helpers JWT de laboratoire (secret partage avec le service onboarding) ----------
  private readonly serviceSecret = Buffer.from(
    process.env.SESSION_SECRET || 'lab-onboardr-session-secret-fictif', 'utf8',
  );

  /** Decode les claims SANS verifier la signature (comportement vulnerable V020). */
  private decodeJwtUnverified(token: string): any | null {
    const parts = token.split('.');
    if (parts.length < 2) return null;
    try {
      return JSON.parse(Buffer.from(parts[1], 'base64url').toString('utf8'));
    } catch {
      return null;
    }
  }

  /** Verifie un JWT HS256 (alg=none refuse) et renvoie les claims, ou null (comportement sur N012). */
  private verifyJwtHs256(token: string): any | null {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    try {
      const header = JSON.parse(Buffer.from(parts[0], 'base64url').toString('utf8'));
      if (header.alg !== 'HS256') return null;
      const signed = `${parts[0]}.${parts[1]}`;
      const expected = crypto.createHmac('sha256', this.serviceSecret).update(signed, 'utf8').digest();
      const provided = Buffer.from(parts[2], 'base64url');
      if (expected.length !== provided.length || !crypto.timingSafeEqual(expected, provided)) {
        return null;
      }
      return JSON.parse(Buffer.from(parts[1], 'base64url').toString('utf8'));
    } catch {
      return null;
    }
  }
}
