/**
 * Abstraction de persistance.
 *
 * Profil `local`/`standalone` : implémentation EN MÉMOIRE (InMemoryStore) — seed au
 * démarrage, remise à zéro via POST /admin/reset.
 *
 * Profil `prod` : une implémentation PostgreSQL (comptes/consentements/exports) + Redis
 * (jetons, clés d'API, rate limiting) respecterait exactement cette interface. Les
 * variables d'environnement DB_URL / REDIS_URL pilotent le câblage (voir StoreModule).
 *
 * Données 100 % FICTIVES — laboratoire DAST, usage défensif.
 */

export interface TppClient {
  clientId: string;
  clientSecret: string; // secret FICTIF de laboratoire
  apiKey: string; // X-API-Key statique FICTIF
  role: 'tpp' | 'admin';
}

export interface Account {
  accountId: string;
  holder: string;
  email: string; // email FICTIF (@agrega.lab.exemple)
  phone: string; // téléphone FICTIF
  iban: string; // IBAN FR de TEST
  linkedCardPan: string; // PAN de TEST (plage 4111...)
  balance: number;
  currency: string;
  status: 'active' | 'dormant' | 'closed'; // statut métier fictif
  ownerClientId: string; // TPP dont le consentement couvre ce compte
}

export interface Payment {
  paymentId: string;
  accountId: string;
  ownerClientId: string;
  amount: number;
  currency: string;
  label: string;
  status: 'executed' | 'pending' | 'rejected'; // statut fictif
  executedAt: string;
}

export interface Notification {
  notificationId: string;
  ownerClientId: string;
  title: string;
  message: string;
  createdAt: string;
  read: boolean;
}

export interface Consent {
  consentId: string;
  tpp: string;
  user: string;
  permissions: string[];
  grantedScopes: string[];
  callbackUrl?: string;
  lastCallbackResponse?: string;
  createdAt: string;
}

export interface ExportRecord {
  exportId: string; // identifiant SÉQUENTIEL (EXP-1007, EXP-1008, …)
  ownerClientId: string;
  fileName: string;
  aggregatedData: Record<string, unknown>;
  createdAt: string;
}

export interface RequestLogEntry {
  at: string;
  method: string;
  path: string;
  apiKey?: string; // journalisé EN CLAIR — écart V012 (ne jamais faire en prod)
  authorization?: string;
}

export interface LabStore {
  clients: Map<string, TppClient>;
  accounts: Map<string, Account>;
  payments: Payment[];
  consents: Map<string, Consent>;
  exports: Map<string, ExportRecord>;
  notifications: Notification[];
  requestLog: RequestLogEntry[];
  consentSeq: number;
  exportSeq: number;
  authCodeSeq: number; // compteur de codes d'autorisation OAuth2 (écart V022, prévisible)

  reset(): void;
}
