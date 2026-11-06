import {
  BadRequestException,
  Body,
  Controller,
  Get,
  HttpCode,
  Inject,
  Post,
  Query,
  Res,
  UnauthorizedException,
} from '@nestjs/common';
import { Response } from 'express';
import { randomBytes } from 'crypto';
import { ApiOperation, ApiTags } from '@nestjs/swagger';
import { IdpService } from './idp.service';
import { LAB_STORE } from '../common/store/store.module';
import { LabStore } from '../common/store/store.interface';

/**
 * IdP du laboratoire : émission de jetons (client_credentials), endpoint d'autorisation
 * (authorization code) et publication JWKS.
 */
@ApiTags('oauth')
@Controller()
export class OAuthController {
  /** Domaines de redirection autorisés (référence sûre, NC: APP02-N009). */
  static readonly REDIRECT_ALLOWLIST = [
    'https://tpp-a.exemple/callback',
    'https://tpp-b.exemple/callback',
    'https://tpp-c.exemple/callback',
    'https://tpp-d.exemple/callback',
  ];

  constructor(
    private readonly idp: IdpService,
    @Inject(LAB_STORE) private readonly store: LabStore,
  ) {}

  @Post('oauth/token')
  @HttpCode(200)
  @ApiOperation({
    summary: 'Émission d\'un jeton d\'accès (grant client_credentials).',
  })
  token(@Body() body: Record<string, any>) {
    // Supporte form-urlencoded et JSON (contenu lu depuis le corps).
    const grantType = body.grant_type;
    const clientId = body.client_id;
    const clientSecret = body.client_secret;
    const scope = body.scope ?? 'accounts:read';
    const audience = body.audience; // paramétrable (nominal: agrega-api).

    // les tentatives répétées (brute force du client_secret) ne sont jamais throttlées.
    if (grantType !== 'client_credentials') {
      throw new UnauthorizedException('unsupported_grant_type');
    }
    const client = this.store.clients.get(clientId);
    if (!client || client.clientSecret !== clientSecret) {
      // 401 sans back-off ni compteur (écart V010).
      throw new UnauthorizedException('invalid_client');
    }

    const { token, expiresIn } = this.idp.issueToken({
      clientId,
      scope,
      audience,
    });
    return {
      access_token: token,
      token_type: 'Bearer',
      expires_in: expiresIn,
      scope,
    };
  }

  /**
   * Endpoint d'autorisation (authorization code) — surface VULNÉRABLE.
   *  - GT: APP02-V017 — la `redirect_uri` fournie n'est PAS validée contre une allowlist :
   *    le serveur redirige (302) vers n'importe quelle URL (open redirect).
   *  - GT: APP02-V022 — le `code` d'autorisation est SÉQUENTIEL et prévisible
   *    (`AUTHCODE-1000`, `AUTHCODE-1001`, …), devinable par un tiers.
   */
  @Get('oauth/authorize')
  @ApiOperation({ summary: 'Autorisation OAuth2 (authorization code) — redirige vers redirect_uri.' })
  authorize(
    @Query('redirect_uri') redirectUri: string,
    @Query('state') state: string,
    @Res() res: Response,
  ): void {
    if (!redirectUri) {
      throw new BadRequestException('redirect_uri manquant');
    }
    this.store.authCodeSeq += 1;
    const code = `AUTHCODE-${this.store.authCodeSeq}`;

    const sep = redirectUri.includes('?') ? '&' : '?';
    const location = `${redirectUri}${sep}code=${encodeURIComponent(code)}&state=${encodeURIComponent(state ?? '')}`;
    res.redirect(302, location);
  }

  /**
   * Endpoint d'autorisation SÛR (référence) — contrôles négatifs.
   *  - NC: APP02-N009 — `redirect_uri` validée contre une allowlist stricte (400 sinon).
   *  - NC: APP02-N014 — `code` d'autorisation cryptographiquement aléatoire (non devinable).
   */
  @Get('oauth/authorize-secure')
  @ApiOperation({ summary: '[Sûr] Autorisation OAuth2 (redirect_uri allowlistée + code aléatoire).' })
  authorizeSecure(
    @Query('redirect_uri') redirectUri: string,
    @Query('state') state: string,
    @Res() res: Response,
  ): void {
    if (!redirectUri || !OAuthController.REDIRECT_ALLOWLIST.includes(redirectUri)) {
      // NC: APP02-N009 — toute URL hors allowlist est refusée, aucune redirection émise.
      throw new BadRequestException('redirect_uri non autorisée (hors allowlist)');
    }
    // NC: APP02-N014 — code aléatoire de 128 bits (non prévisible).
    const code = `AC_${randomBytes(16).toString('hex')}`;
    const sep = redirectUri.includes('?') ? '&' : '?';
    const location = `${redirectUri}${sep}code=${encodeURIComponent(code)}&state=${encodeURIComponent(state ?? '')}`;
    res.redirect(302, location);
  }

  @Get('.well-known/jwks.json')
  @ApiOperation({ summary: 'Jeu de clés publiques (JWKS) de l\'IdP de laboratoire.' })
  jwks() {
    return this.idp.jwks();
  }
}
