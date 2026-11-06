import { Inject, Injectable } from '@nestjs/common';
import { LAB_STORE } from '../common/store/store.module';
import { LabStore, Notification } from '../common/store/store.interface';

@Injectable()
export class NotificationsService {
  constructor(@Inject(LAB_STORE) private readonly store: LabStore) {}

  /** Notifications appartenant au TPP (surface bénigne, scoping par client). */
  listForClient(clientId: string): Notification[] {
    return this.store.notifications.filter((n) => n.ownerClientId === clientId);
  }

  /** Échappement HTML (référence sûre, NC: APP02-N010). */
  escapeHtml(input: string): string {
    return String(input ?? '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#x27;');
  }

  /** Enveloppe HTML d'aperçu (le corps est injecté tel quel par l'appelant). */
  wrapPreview(body: string): string {
    return (
      '<!doctype html><html lang="fr"><head><meta charset="utf-8">' +
      '<title>Aperçu de notification — Agréga Open</title></head><body>' +
      '<h1>Aperçu de notification</h1>' +
      `<div class="notif-preview">${body}</div>` +
      '</body></html>'
    );
  }

  /**
   * Rendu de template VULNÉRABLE (GT: APP02-V021).
   * Les expressions `{{ ... }}` sont ÉVALUÉES côté serveur (injection de template / SSTI).
   */
  renderUnsafe(template: string): string {
    return String(template ?? '').replace(/\{\{(.+?)\}\}/g, (_m, expr) => {
      try {
        // eslint-disable-next-line no-new-func
        const value = Function(`"use strict"; return (${expr});`)();
        return String(value);
      } catch {
        return '';
      }
    });
  }

  /**
   * Rendu de template SÛR (référence, NC: APP02-N013).
   * L'entrée est traitée comme une DONNÉE littérale (aucune évaluation), puis échappée.
   */
  renderSafe(template: string): string {
    return this.escapeHtml(String(template ?? ''));
  }
}
