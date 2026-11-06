import {
  BadRequestException,
  Body,
  Controller,
  ForbiddenException,
  Get,
  HttpCode,
  Param,
  Post,
  Query,
  Req,
  Res,
  UnprocessableEntityException,
  UseGuards,
  UsePipes,
  ValidationPipe,
} from '@nestjs/common';
import { Response } from 'express';
import { ApiBearerAuth, ApiOperation, ApiTags } from '@nestjs/swagger';
import { AccountsService } from '../accounts/accounts.service';
import { ConsentsService } from '../consents/consents.service';
import { NotificationsService } from '../notifications/notifications.service';
import { ReportsService } from '../reports/reports.service';
import { StrictJwtGuard } from '../common/guards/strict-jwt.guard';
import { ScopeGuard, Scopes } from '../common/guards/scope.guard';
import { RateLimitGuard } from '../common/guards/rate-limit.guard';
import { AuthContext } from '../common/guards/lab-auth.guard';
import { SecureCreateConsentDto } from '../consents/dto';
import { Consent } from '../common/store/store.interface';
import { Inject } from '@nestjs/common';
import { LAB_STORE } from '../common/store/store.module';
import { LabStore } from '../common/store/store.interface';

/**
 * Surface /v1/secure — CONTRÔLES NÉGATIFS (routes comparables mais SÛRES).
 * Toute alerte d'un scanner sur ces routes est un FAUX POSITIF.
 */
@ApiTags('secure')
@ApiBearerAuth()
@Controller('v1/secure')
@UseGuards(StrictJwtGuard, RateLimitGuard) // NC: APP02-N005 (JWT strict) + durcissement.
export class SecureController {
  constructor(
    private readonly accounts: AccountsService,
    private readonly consents: ConsentsService,
    private readonly notifications: NotificationsService,
    private readonly reports: ReportsService,
    @Inject(LAB_STORE) private readonly store: LabStore,
  ) {}

  // NC: APP02-N005 — aud/iss/alg vérifiés en amont par StrictJwtGuard.
  // NC: APP02-N007 — version courante toujours authentifiée, aucune version dépréciée exposée.
  @Get('accounts')
  @ApiOperation({ summary: '[Sûr] Liste des comptes (JWT strict).' })
  listAccounts(@Req() req: any) {
    const auth: AuthContext = req.auth;
    return { accounts: this.accounts.listSummaryForClient(auth.clientId) };
  }

  // NC: APP02-N001 — contrôle de propriété lié au consentement (403 hors périmètre).
  // NC: APP02-N004 — modèle de sortie restreint (IBAN/PAN masqués).
  @Get('accounts/:accountId')
  @ApiOperation({ summary: '[Sûr] Détail de compte (propriété + masquage).' })
  accountDetail(@Param('accountId') accountId: string, @Req() req: any) {
    const auth: AuthContext = req.auth;
    if (!AccountsService.ID_PATTERN.test(accountId)) {
      // Référence sûre : 422 propre, pas de trace (le filtre global reste générique ici).
      throw new UnprocessableEntityException('Format d\'identifiant de compte invalide');
    }
    if (!this.accounts.isOwnedBy(accountId, auth.clientId)) {
      throw new ForbiddenException('Compte hors du périmètre du consentement'); // NC: APP02-N001
    }
    const account = this.accounts.getRaw(accountId);
    return this.accounts.maskedView(account); // NC: APP02-N004
  }

  // NC: APP02-N006 — garde de scope par ressource (403 si payments:read absent).
  @Get('payments')
  @UseGuards(ScopeGuard)
  @Scopes('payments:read')
  @ApiOperation({ summary: '[Sûr] Paiements (scope payments:read requis).' })
  payments(@Req() req: any) {
    const auth: AuthContext = req.auth;
    return { payments: this.store.payments.filter((p) => p.ownerClientId === auth.clientId) };
  }

  // NC: APP02-N002 — fonction d'administration restreinte par rôle/scope admin.
  @Post('admin/consents')
  @HttpCode(200)
  @ApiOperation({ summary: '[Sûr] Administration des consentements (rôle admin requis).' })
  adminConsents(@Req() req: any, @Body() body: any) {
    const auth: AuthContext = req.auth;
    const isAdmin = auth.clientId === 'admin' || auth.scopes.includes('consents:admin');
    if (!isAdmin) {
      throw new ForbiddenException('Privilège d\'administration requis'); // NC: APP02-N002
    }
    const action = body?.action ?? 'list_all';
    return { action, consents: this.consents.listAll() };
  }

  // NC: APP02-N003 — DTO strict (scopes dérivés serveur, champ scopes du corps ignoré).
  // NC: APP02-N008 — webhook validé par allowlist + blocage des adresses internes.
  @Post('consents')
  @HttpCode(201)
  @UsePipes(new ValidationPipe({ whitelist: true, transform: true }))
  @ApiOperation({ summary: '[Sûr] Création de consentement (DTO strict + webhook allowlist).' })
  async createConsent(@Body() dto: SecureCreateConsentDto, @Req() req: any) {
    const auth: AuthContext = req.auth;

    if (dto.callbackUrl) {
      const check = await this.consents.validateCallbackSafe(dto.callbackUrl); // NC: APP02-N008
      if (!check.ok) {
        throw new UnprocessableEntityException(`callbackUrl refusée : ${check.reason}`);
      }
    }

    // NC: APP02-N003 — scopes dérivés côté serveur des permissions autorisées,
    // JAMAIS du corps client (le champ `scopes` a déjà été retiré par le DTO strict).
    const serverAllowed = ['accounts:read', 'payments:read'];
    const grantedScopes = (dto.permissions ?? []).filter((p) => serverAllowed.includes(p));

    const consent: Consent = {
      consentId: this.consents.nextConsentId(),
      tpp: auth.clientId,
      user: dto.user,
      permissions: dto.permissions ?? [],
      grantedScopes,
      callbackUrl: dto.callbackUrl,
      createdAt: new Date().toISOString(),
    };
    this.consents.save(consent);

    return {
      consentId: consent.consentId,
      tpp: consent.tpp,
      user: consent.user,
      permissions: consent.permissions,
      grantedScopes: consent.grantedScopes,
      callbackUrl: consent.callbackUrl,
      createdAt: consent.createdAt,
    };
  }

  // NC: APP02-N010 — aperçu de notification avec échappement HTML (XSS neutralisée).
  @Get('notifications/preview')
  @ApiOperation({ summary: '[Sûr] Aperçu de notification (message échappé).' })
  previewSecure(@Query('message') message: string, @Res() res: Response): void {
    const html = this.notifications.wrapPreview(this.notifications.escapeHtml(message ?? ''));
    res.type('html').send(html);
  }

  // NC: APP02-N013 — rendu de modèle sans évaluation (entrée traitée en donnée littérale).
  @Post('notifications/render')
  @HttpCode(200)
  @ApiOperation({ summary: '[Sûr] Rendu de modèle de notification (sans évaluation).' })
  renderSecure(@Body() body: any) {
    return { rendered: this.notifications.renderSafe(String(body?.template ?? '')) };
  }

  // NC: APP02-N011 — téléchargement de rapport avec anti-traversée + allowlist.
  @Get('reports/download')
  @ApiOperation({ summary: '[Sûr] Téléchargement de rapport (nom validé, pas de traversée).' })
  downloadSecure(@Query('name') name: string, @Res() res: Response): void {
    const content = this.reports.readSafe(name ?? '');
    if (content === null) {
      throw new BadRequestException('Nom de fichier invalide ou non autorisé'); // NC: APP02-N011
    }
    res.type('text/plain').send(content);
  }

  // NC: APP02-N012 — export CSV avec neutralisation des cellules de formule.
  @Get('reports/csv')
  @ApiOperation({ summary: '[Sûr] Export CSV des consentements (cellules neutralisées).' })
  csvSecure(@Req() req: any, @Res() res: Response): void {
    const auth: AuthContext = req.auth;
    res.type('text/csv').send(this.reports.buildCsvSafe(auth.clientId)); // NC: APP02-N012
  }
}
