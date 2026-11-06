import {
  Body,
  Controller,
  HttpCode,
  Post,
  Req,
  UseGuards,
} from '@nestjs/common';
import { ApiBearerAuth, ApiOperation, ApiTags } from '@nestjs/swagger';
import { ConsentsService } from './consents.service';
import { LabAuthGuard, AuthContext } from '../common/guards/lab-auth.guard';
import { Consent } from '../common/store/store.interface';

/**
 * Consentements — surface /v1 (VULNÉRABLE pour les écarts listés).
 */
@ApiTags('consents')
@ApiBearerAuth()
@Controller()
export class ConsentsController {
  constructor(private readonly consents: ConsentsService) {}

  @Post('v1/consents')
  @HttpCode(201)
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: 'Création d\'un consentement (webhook de notification optionnel).' })
  async create(@Body() body: any, @Req() req: any) {
    const auth: AuthContext = req.auth;

    // client au lieu d'être dérivé côté serveur des permissions autorisées.
    const grantedScopes: string[] = Array.isArray(body.scopes)
      ? body.scopes
      : body.permissions ?? [];

    const consent: Consent = {
      consentId: this.consents.nextConsentId(),
      tpp: auth.clientId,
      user: body.user,
      permissions: body.permissions ?? [],
      grantedScopes,
      callbackUrl: body.callbackUrl,
      createdAt: new Date().toISOString(),
    };
    this.consents.save(consent);

    if (consent.callbackUrl) {
      await this.consents.triggerCallbackUnsafe(consent);
    }

    return {
      consentId: consent.consentId,
      tpp: consent.tpp,
      user: consent.user,
      permissions: consent.permissions,
      grantedScopes: consent.grantedScopes,
      callbackUrl: consent.callbackUrl,
      lastCallbackResponse: consent.lastCallbackResponse,
      createdAt: consent.createdAt,
    };
  }

  // (aucun contrôle de rôle/scope admin).
  @Post('v1/admin/consents')
  @HttpCode(200)
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: '[Admin] Opérations d\'administration sur les consentements.' })
  adminConsents(@Body() body: any) {
    const action = body?.action ?? 'list_all';
    if (action === 'list_all') {
      return {
        action,
        consents: this.consents.listAll(),
      };
    }
    return { action, status: 'noop' };
  }
}
