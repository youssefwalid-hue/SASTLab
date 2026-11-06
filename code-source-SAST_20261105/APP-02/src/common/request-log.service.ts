import { Injectable, Inject } from '@nestjs/common';
import { LAB_STORE } from './store/store.module';
import { LabStore } from './store/store.interface';

/**
 * Journal de requêtes de diagnostic.
 * exposée via GET /debug/requestlog (secret rejouable et traçable).
 * En production, ne jamais journaliser de secret ni exposer un tel endpoint.
 */
@Injectable()
export class RequestLogService {
  constructor(@Inject(LAB_STORE) private readonly store: LabStore) {}

  record(entry: { method: string; path: string; apiKey?: string; authorization?: string }): void {
    this.store.requestLog.push({
      at: new Date().toISOString(),
      method: entry.method,
      path: entry.path,
      apiKey: entry.apiKey, 
      authorization: entry.authorization,
    });
    if (this.store.requestLog.length > 500) {
      this.store.requestLog.splice(0, this.store.requestLog.length - 500);
    }
  }

  all() {
    return this.store.requestLog;
  }
}
