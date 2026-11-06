import { Injectable } from '@nestjs/common';
import {
  Account,
  Consent,
  ExportRecord,
  LabStore,
  Notification,
  Payment,
  RequestLogEntry,
  TppClient,
} from './store.interface';

/**
 * Stockage EN MÉMOIRE (profil local/standalone).
 * Seed de données 100 % FICTIVES au démarrage et sur reset().
 *
 * Acteurs : tpp-a / tpp-b / tpp-c / tpp-d (clients OAuth), alice / bruno / chloe / david /
 * elodie / farid (utilisateurs sous-jacents), admin (administration des consentements).
 * Secret de labo : Password1!.
 *
 * Les identifiants déterministes attendus par le validateur sont PRÉSERVÉS :
 *   clients tpp-a/tpp-b/admin ; comptes ACC-FR-1001 (alice/tpp-a) & ACC-FR-2001 (bruno/tpp-b) ;
 *   paiements PAY-5001/5002/6001 ; consentements CNS-9001/9002 ; exports EXP-1007 (tpp-a) &
 *   EXP-1008 (tpp-b). Le volume supplémentaire vient APRÈS ces entrées.
 */
@Injectable()
export class InMemoryStore implements LabStore {
  clients = new Map<string, TppClient>();
  accounts = new Map<string, Account>();
  payments: Payment[] = [];
  consents = new Map<string, Consent>();
  exports = new Map<string, ExportRecord>();
  notifications: Notification[] = [];
  requestLog: RequestLogEntry[] = [];
  consentSeq = 0;
  exportSeq = 0;
  authCodeSeq = 0;

  constructor() {
    this.reset();
  }

  reset(): void {
    this.clients.clear();
    this.accounts.clear();
    this.payments = [];
    this.consents.clear();
    this.exports.clear();
    this.notifications = [];
    this.requestLog = [];
    this.consentSeq = 0;
    this.exportSeq = 0;
    this.authCodeSeq = 1000;

    // --- Clients OAuth (TPP) — secrets et clés FICTIFS ----------------------
    const clients: TppClient[] = [
      { clientId: 'tpp-a', clientSecret: 'Password1!', apiKey: 'AGK-LAB-tppa-0001', role: 'tpp' },
      { clientId: 'tpp-b', clientSecret: 'Password1!', apiKey: 'AGK-LAB-tppb-0001', role: 'tpp' },
      { clientId: 'tpp-c', clientSecret: 'Password1!', apiKey: 'AGK-LAB-tppc-0001', role: 'tpp' },
      { clientId: 'tpp-d', clientSecret: 'Password1!', apiKey: 'AGK-LAB-tppd-0001', role: 'tpp' },
      { clientId: 'admin', clientSecret: 'Password1!', apiKey: 'AGK-LAB-admin-0001', role: 'admin' },
    ];
    clients.forEach((c) => this.clients.set(c.clientId, c));

    // --- Comptes — IBAN / PAN de TEST --------------------------------------
    // ACC-FR-1001 / ACC-FR-2001 PRÉSERVÉS (attendus par le validateur).
    const accounts: Account[] = [
      {
        accountId: 'ACC-FR-1001',
        holder: 'alice',
        email: 'alice@agrega.lab.exemple',
        phone: '+33100000001',
        iban: 'FR7612345678901234567890123',
        linkedCardPan: '4111111111111111',
        balance: 4231.57,
        currency: 'EUR',
        status: 'active',
        ownerClientId: 'tpp-a',
      },
      {
        accountId: 'ACC-FR-2001',
        holder: 'bruno',
        email: 'bruno@agrega.lab.exemple',
        phone: '+33100000002',
        iban: 'FR7698765432109876543210987',
        linkedCardPan: '4111222233334444',
        balance: 8820.0,
        currency: 'EUR',
        status: 'active',
        ownerClientId: 'tpp-b',
      },
      // --- Volume supplémentaire (données fictives) --------------------------
      {
        accountId: 'ACC-FR-1002',
        holder: 'alice',
        email: 'alice@agrega.lab.exemple',
        phone: '+33100000001',
        iban: 'FR7611112222333344445555667',
        linkedCardPan: '4111333344445555',
        balance: 15230.42,
        currency: 'EUR',
        status: 'active',
        ownerClientId: 'tpp-a',
      },
      {
        accountId: 'ACC-FR-1003',
        holder: 'chloe',
        email: 'chloe@agrega.lab.exemple',
        phone: '+33100000003',
        iban: 'FR7622223333444455556666778',
        linkedCardPan: '4111444455556666',
        balance: 320.1,
        currency: 'EUR',
        status: 'dormant',
        ownerClientId: 'tpp-c',
      },
      {
        accountId: 'ACC-FR-2002',
        holder: 'bruno',
        email: 'bruno@agrega.lab.exemple',
        phone: '+33100000002',
        iban: 'FR7633334444555566667777889',
        linkedCardPan: '4111555566667777',
        balance: 0.0,
        currency: 'EUR',
        status: 'closed',
        ownerClientId: 'tpp-b',
      },
      {
        accountId: 'ACC-GB-3001',
        holder: 'david',
        email: 'david@agrega.lab.exemple',
        phone: '+33100000004',
        iban: 'GB29NWBK60161331926819',
        linkedCardPan: '4111666677778888',
        balance: 2750.0,
        currency: 'GBP',
        status: 'active',
        ownerClientId: 'tpp-d',
      },
      {
        accountId: 'ACC-FR-3002',
        holder: 'elodie',
        email: 'elodie@agrega.lab.exemple',
        phone: '+33100000005',
        iban: 'FR7644445555666677778888990',
        linkedCardPan: '4111777788889999',
        balance: 980.75,
        currency: 'EUR',
        status: 'active',
        ownerClientId: 'tpp-c',
      },
      {
        accountId: 'ACC-FR-3003',
        holder: 'farid',
        email: 'farid@agrega.lab.exemple',
        phone: '+33100000006',
        iban: 'FR7655556666777788889999001',
        linkedCardPan: '4111888899990000',
        balance: 61240.0,
        currency: 'EUR',
        status: 'active',
        ownerClientId: 'tpp-d',
      },
    ];
    accounts.forEach((a) => this.accounts.set(a.accountId, a));

    // --- Paiements fictifs --------------------------------------------------
    // PAY-5001/5002/6001 PRÉSERVÉS.
    this.payments = [
      {
        paymentId: 'PAY-5001',
        accountId: 'ACC-FR-1001',
        ownerClientId: 'tpp-a',
        amount: 42.9,
        currency: 'EUR',
        label: 'Abonnement transport',
        status: 'executed',
        executedAt: '2026-09-02T09:14:00Z',
      },
      {
        paymentId: 'PAY-5002',
        accountId: 'ACC-FR-1001',
        ownerClientId: 'tpp-a',
        amount: 120.0,
        currency: 'EUR',
        label: 'Électricité',
        status: 'executed',
        executedAt: '2026-09-05T18:30:00Z',
      },
      {
        paymentId: 'PAY-6001',
        accountId: 'ACC-FR-2001',
        ownerClientId: 'tpp-b',
        amount: 59.99,
        currency: 'EUR',
        label: 'Téléphonie',
        status: 'executed',
        executedAt: '2026-09-04T11:00:00Z',
      },
      // --- Volume supplémentaire --------------------------------------------
      {
        paymentId: 'PAY-5003',
        accountId: 'ACC-FR-1002',
        ownerClientId: 'tpp-a',
        amount: 899.0,
        currency: 'EUR',
        label: 'Loyer',
        status: 'pending',
        executedAt: '2026-09-10T07:00:00Z',
      },
      {
        paymentId: 'PAY-5004',
        accountId: 'ACC-FR-1002',
        ownerClientId: 'tpp-a',
        amount: 24.5,
        currency: 'EUR',
        label: 'Streaming',
        status: 'rejected',
        executedAt: '2026-09-11T20:05:00Z',
      },
      {
        paymentId: 'PAY-7001',
        accountId: 'ACC-FR-1003',
        ownerClientId: 'tpp-c',
        amount: 310.0,
        currency: 'EUR',
        label: 'Assurance',
        status: 'executed',
        executedAt: '2026-09-06T08:45:00Z',
      },
      {
        paymentId: 'PAY-8001',
        accountId: 'ACC-GB-3001',
        ownerClientId: 'tpp-d',
        amount: 75.0,
        currency: 'GBP',
        label: 'Utilities',
        status: 'executed',
        executedAt: '2026-09-07T13:20:00Z',
      },
      {
        paymentId: 'PAY-8002',
        accountId: 'ACC-FR-3003',
        ownerClientId: 'tpp-d',
        amount: 1500.0,
        currency: 'EUR',
        label: 'Virement épargne',
        status: 'pending',
        executedAt: '2026-09-12T10:00:00Z',
      },
    ];

    // --- Consentements de seed ---------------------------------------------
    // CNS-9001/9002 PRÉSERVÉS.
    const seedConsents: Consent[] = [
      {
        consentId: 'CNS-9001',
        tpp: 'tpp-a',
        user: 'alice',
        permissions: ['accounts:read', 'payments:read'],
        grantedScopes: ['accounts:read', 'payments:read'],
        createdAt: '2026-08-01T10:00:00Z',
      },
      {
        consentId: 'CNS-9002',
        tpp: 'tpp-b',
        user: 'bruno',
        permissions: ['accounts:read', 'payments:read'],
        grantedScopes: ['accounts:read', 'payments:read'],
        createdAt: '2026-08-01T10:05:00Z',
      },
      {
        consentId: 'CNS-9003',
        tpp: 'tpp-c',
        user: 'chloe',
        permissions: ['accounts:read'],
        grantedScopes: ['accounts:read'],
        createdAt: '2026-08-02T09:00:00Z',
      },
      {
        consentId: 'CNS-9004',
        tpp: 'tpp-d',
        user: 'david',
        permissions: ['accounts:read', 'payments:read'],
        grantedScopes: ['accounts:read', 'payments:read'],
        createdAt: '2026-08-02T09:10:00Z',
      },
      {
        consentId: 'CNS-9005',
        tpp: 'tpp-d',
        user: 'farid',
        permissions: ['accounts:read'],
        grantedScopes: ['accounts:read'],
        createdAt: '2026-08-03T14:30:00Z',
      },
    ];
    seedConsents.forEach((c) => this.consents.set(c.consentId, c));
    this.consentSeq = 9005;

    // --- Exports agrégés — identifiants SÉQUENTIELS -------------------------
    // EXP-1007 (tpp-a) & EXP-1008 (tpp-b) PRÉSERVÉS.
    const seedExports: ExportRecord[] = [
      {
        exportId: 'EXP-1007',
        ownerClientId: 'tpp-a',
        fileName: 'agrega-export-alice-2026-09.json',
        aggregatedData: { holder: 'alice', accounts: ['ACC-FR-1001'], totalBalance: 4231.57 },
        createdAt: '2026-09-10T08:00:00Z',
      },
      {
        exportId: 'EXP-1008',
        ownerClientId: 'tpp-b',
        fileName: 'agrega-export-bruno-2026-09.json',
        aggregatedData: { holder: 'bruno', accounts: ['ACC-FR-2001'], totalBalance: 8820.0 },
        createdAt: '2026-09-10T08:05:00Z',
      },
      {
        exportId: 'EXP-1009',
        ownerClientId: 'tpp-a',
        fileName: 'agrega-export-alice-2026-10.json',
        aggregatedData: { holder: 'alice', accounts: ['ACC-FR-1001', 'ACC-FR-1002'], totalBalance: 19461.99 },
        createdAt: '2026-10-01T08:00:00Z',
      },
      {
        exportId: 'EXP-1010',
        ownerClientId: 'tpp-c',
        fileName: 'agrega-export-chloe-2026-10.json',
        aggregatedData: { holder: 'chloe', accounts: ['ACC-FR-1003'], totalBalance: 320.1 },
        createdAt: '2026-10-01T08:05:00Z',
      },
      {
        exportId: 'EXP-1011',
        ownerClientId: 'tpp-d',
        fileName: 'agrega-export-david-2026-10.json',
        aggregatedData: { holder: 'david', accounts: ['ACC-GB-3001'], totalBalance: 2750.0 },
        createdAt: '2026-10-01T08:10:00Z',
      },
    ];
    seedExports.forEach((e) => this.exports.set(e.exportId, e));
    this.exportSeq = 1011;

    // --- Notifications fictives (surface bénigne) --------------------------
    this.notifications = [
      {
        notificationId: 'NTF-4001',
        ownerClientId: 'tpp-a',
        title: 'Consentement renouvelé',
        message: 'Le consentement de alice a été renouvelé pour 90 jours.',
        createdAt: '2026-09-15T09:00:00Z',
        read: false,
      },
      {
        notificationId: 'NTF-4002',
        ownerClientId: 'tpp-a',
        title: 'Nouvel export disponible',
        message: 'Export agrégé EXP-1009 prêt au téléchargement.',
        createdAt: '2026-10-01T08:00:00Z',
        read: true,
      },
      {
        notificationId: 'NTF-4003',
        ownerClientId: 'tpp-b',
        title: 'Compte clôturé',
        message: 'Le compte ACC-FR-2002 de bruno est passé au statut clôturé.',
        createdAt: '2026-09-20T12:00:00Z',
        read: false,
      },
      {
        notificationId: 'NTF-4004',
        ownerClientId: 'tpp-c',
        title: 'Compte dormant',
        message: 'Le compte ACC-FR-1003 de chloe est dormant depuis 180 jours.',
        createdAt: '2026-09-22T16:30:00Z',
        read: false,
      },
      {
        notificationId: 'NTF-4005',
        ownerClientId: 'tpp-d',
        title: 'Paiement en attente',
        message: 'Virement épargne PAY-8002 en attente de validation.',
        createdAt: '2026-09-12T10:00:00Z',
        read: false,
      },
    ];
  }
}
