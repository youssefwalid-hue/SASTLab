import { Controller, ForbiddenException, Get, Inject, Query, Req, UseGuards } from '@nestjs/common';
import { ApiBearerAuth, ApiOperation, ApiTags } from '@nestjs/swagger';
import { LabAuthGuard, AuthContext } from '../common/guards/lab-auth.guard';
import { LAB_STORE } from '../common/store/store.module';
import { LabStore } from '../common/store/store.interface';

/**
 * Surface fonctionnelle BÉNIGNE (densification de crawl, SANS écart ni NC).
 * Toutes les routes exigent une authentification, filtrent par périmètre du TPP et ne
 * renvoient que des données non sensibles (IBAN/PAN jamais exposés ici). Aucune alerte
 * de scanner n'est attendue sur ces routes.
 */
@ApiTags('catalog')
@ApiBearerAuth()
@Controller()
export class CatalogController {
  constructor(@Inject(LAB_STORE) private readonly store: LabStore) {}

  // Recherche paginée des comptes du TPP (filtre en mémoire, paramétré).
  @Get('v1/search/accounts')
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: 'Recherche paginée des comptes du TPP.' })
  searchAccounts(
    @Req() req: any,
    @Query('q') q: string,
    @Query('page') page?: string,
    @Query('size') size?: string,
  ) {
    const auth: AuthContext = req.auth;
    const needle = String(q ?? '').toLowerCase();
    const pageNum = Math.max(1, Number(page) || 1);
    const pageSize = Math.min(50, Math.max(1, Number(size) || 10));

    const all = [...this.store.accounts.values()]
      .filter((a) => a.ownerClientId === auth.clientId)
      .filter(
        (a) =>
          !needle ||
          a.holder.toLowerCase().includes(needle) ||
          a.accountId.toLowerCase().includes(needle) ||
          a.status.toLowerCase().includes(needle),
      );

    const start = (pageNum - 1) * pageSize;
    const items = all.slice(start, start + pageSize).map((a) => ({
      accountId: a.accountId,
      holder: a.holder,
      currency: a.currency,
      balance: a.balance,
      status: a.status,
    }));

    return { page: pageNum, size: pageSize, total: all.length, items };
  }

  // Historique des transactions d'un compte (contrôle de propriété — route SÛRE).
  @Get('v1/transactions')
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: 'Historique des transactions d\'un compte du périmètre.' })
  transactions(@Req() req: any, @Query('accountId') accountId: string) {
    const auth: AuthContext = req.auth;
    const account = this.store.accounts.get(accountId);
    if (!account || account.ownerClientId !== auth.clientId) {
      // Route bénigne : refus net si le compte n'est pas dans le périmètre du TPP.
      throw new ForbiddenException('Compte hors du périmètre du consentement');
    }
    const transactions = this.store.payments
      .filter((p) => p.accountId === accountId && p.ownerClientId === auth.clientId)
      .map((p) => ({
        paymentId: p.paymentId,
        amount: p.amount,
        currency: p.currency,
        label: p.label,
        status: p.status,
        executedAt: p.executedAt,
      }));
    return { accountId, transactions };
  }

  // Profil du TPP courant (clé d'API masquée).
  @Get('v1/profile')
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: 'Profil du TPP authentifié.' })
  profile(@Req() req: any) {
    const auth: AuthContext = req.auth;
    const client = this.store.clients.get(auth.clientId);
    const apiKey = client?.apiKey ?? '';
    const maskedApiKey = apiKey ? `${'*'.repeat(Math.max(0, apiKey.length - 4))}${apiKey.slice(-4)}` : '';
    return {
      clientId: auth.clientId,
      role: auth.role,
      scopes: auth.scopes,
      via: auth.via,
      apiKey: maskedApiKey,
    };
  }

  // Liste des exports appartenant au TPP (résumé, SANS IDOR : scoping strict par propriétaire).
  @Get('v1/exports')
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: 'Liste des exports agrégés du TPP (résumé).' })
  listExports(@Req() req: any) {
    const auth: AuthContext = req.auth;
    const exports = [...this.store.exports.values()]
      .filter((e) => e.ownerClientId === auth.clientId)
      .map((e) => ({ exportId: e.exportId, fileName: e.fileName, createdAt: e.createdAt }));
    return { exports };
  }

  // Page d'aide / à-propos — information statique publique (aucune donnée métier).
  @Get('v1/about')
  @ApiOperation({ summary: 'À propos de l\'API (aide, information statique).' })
  about() {
    return {
      service: 'agrega-open',
      description: "API d'agrégation de comptes (Open Banking) — environnement de laboratoire.",
      environment: 'lab-test-data-fictives',
      documentation: '/docs',
      contact: 'support@agrega.lab.exemple',
    };
  }
}
