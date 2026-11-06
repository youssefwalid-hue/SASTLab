import { Inject, Injectable, NotFoundException } from '@nestjs/common';
import { LAB_STORE } from '../common/store/store.module';
import { Account, LabStore } from '../common/store/store.interface';

@Injectable()
export class AccountsService {
  constructor(@Inject(LAB_STORE) private readonly store: LabStore) {}

  /** Format de pattern attendu pour un identifiant de compte. */
  static readonly ID_PATTERN = /^ACC-[A-Z]{2}-\d{3,}$/;

  listSummaryForClient(clientId: string) {
    return [...this.store.accounts.values()]
      .filter((a) => a.ownerClientId === clientId)
      .map((a) => ({
        accountId: a.accountId,
        holder: a.holder,
        currency: a.currency,
        balance: a.balance,
      }));
  }

  listSummaryAll() {
    return [...this.store.accounts.values()].map((a) => ({
      accountId: a.accountId,
      holder: a.holder,
      currency: a.currency,
      balance: a.balance,
    }));
  }

  getRaw(accountId: string): Account {
    const account = this.store.accounts.get(accountId);
    if (!account) {
      throw new NotFoundException('Compte introuvable');
    }
    return account;
  }

  /** Représentation complète (IBAN/PAN en clair) — utilisée par la route /v1 (écart V004). */
  fullView(a: Account) {
    return {
      accountId: a.accountId,
      holder: a.holder,
      iban: a.iban, 
      linkedCardPan: a.linkedCardPan, 
      balance: a.balance,
      currency: a.currency,
      ownerClientId: a.ownerClientId,
    };
  }

  /** Représentation restreinte (IBAN/PAN masqués) — référence sûre (NC: APP02-N004). */
  maskedView(a: Account) {
    return {
      accountId: a.accountId,
      holder: a.holder,
      iban: this.maskIban(a.iban), // NC: APP02-N004
      linkedCardPan: this.maskPan(a.linkedCardPan), // NC: APP02-N004
      balance: a.balance,
      currency: a.currency,
    };
  }

  isOwnedBy(accountId: string, clientId: string): boolean {
    const a = this.store.accounts.get(accountId);
    return !!a && a.ownerClientId === clientId;
  }

  private maskIban(iban: string): string {
    if (iban.length <= 8) return '****';
    return `${iban.slice(0, 4)}${'*'.repeat(iban.length - 8)}${iban.slice(-4)}`;
  }

  private maskPan(pan: string): string {
    if (pan.length <= 10) return '****';
    return `${pan.slice(0, 6)}${'*'.repeat(pan.length - 10)}${pan.slice(-4)}`;
  }
}
