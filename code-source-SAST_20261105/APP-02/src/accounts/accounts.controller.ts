import {
  Controller,
  Get,
  Param,
  Req,
  UseGuards,
} from '@nestjs/common';
import { ApiBearerAuth, ApiOperation, ApiTags } from '@nestjs/swagger';
import { AccountsService } from './accounts.service';
import { LabAuthGuard, AuthContext } from '../common/guards/lab-auth.guard';

/**
 * Comptes — surface /v1 (VULNÉRABLE pour les écarts listés).
 */
@ApiTags('accounts')
@ApiBearerAuth()
@Controller()
export class AccountsController {
  constructor(private readonly accounts: AccountsService) {}

  @Get('v1/accounts')
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: 'Liste des comptes couverts par le consentement du TPP.' })
  list(@Req() req: any) {
    const auth: AuthContext = req.auth;
    return { accounts: this.accounts.listSummaryForClient(auth.clientId) };
  }

  @Get('v1/accounts/:accountId')
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: 'Détail d\'un compte.' })
  detail(@Param('accountId') accountId: string, @Req() _req: any) {
    // (la stack est renvoyée par le filtre global sur les routes /v1).
    if (!AccountsService.ID_PATTERN.test(accountId)) {
      throw new Error(
        `Account identifier failed domain validation: received "${accountId}" ` +
          `which does not satisfy ${AccountsService.ID_PATTERN}`,
      );
    }
    // soit le consentement porté par le token (BOLA).
    const account = this.accounts.getRaw(accountId);
    return this.accounts.fullView(account);
  }

  @Get('v1-internal/accounts')
  @ApiOperation({ summary: '[Déprécié] Inventaire interne des comptes (hérité).' })
  legacyInternal() {
    return { accounts: this.accounts.listSummaryAll() };
  }
}
