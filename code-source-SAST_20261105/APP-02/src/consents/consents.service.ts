import { Inject, Injectable } from '@nestjs/common';
import { lookup } from 'dns/promises';
import { isIP } from 'net';
import { LAB_STORE } from '../common/store/store.module';
import { Consent, LabStore } from '../common/store/store.interface';

@Injectable()
export class ConsentsService {
  constructor(@Inject(LAB_STORE) private readonly store: LabStore) {}

  /** Domaines de webhook autorisés (référence sûre, NC: APP02-N008). */
  static readonly WEBHOOK_ALLOWLIST = ['tpp-a.exemple', 'tpp-b.exemple', 'webhook.tpp.exemple'];

  nextConsentId(): string {
    this.store.consentSeq += 1;
    return `CNS-${this.store.consentSeq}`;
  }

  save(consent: Consent): void {
    this.store.consents.set(consent.consentId, consent);
  }

  listAll(): Consent[] {
    return [...this.store.consents.values()];
  }

  /**
   * Déclenche l'appel sortant du webhook de consentement SANS filtrage.
   * ni blocage des adresses internes/lien-local (SSRF).
   */
  async triggerCallbackUnsafe(consent: Consent): Promise<void> {
    if (!consent.callbackUrl) return;
    try {
      const controller = new AbortController();
      const timer = setTimeout(() => controller.abort(), 2000);
      const res = await fetch(consent.callbackUrl, {
        method: 'GET',
        signal: controller.signal,
      });
      clearTimeout(timer);
      const text = await res.text();
      // La réponse de la cible (y compris interne) est stockée dans l'état du consentement.
      consent.lastCallbackResponse = text.slice(0, 500);
    } catch (e) {
      // Même en cas d'échec, la requête sortante a bien été émise (SSRF confirmée).
      consent.lastCallbackResponse = `outbound-attempted:${(e as Error).name}:${(e as Error).message}`;
    }
  }

  /**
   * Valide une callbackUrl selon l'allowlist et bloque les cibles internes.
   * NC: APP02-N008 — référence sûre.
   */
  async validateCallbackSafe(rawUrl: string): Promise<{ ok: boolean; reason?: string }> {
    let url: URL;
    try {
      url = new URL(rawUrl);
    } catch {
      return { ok: false, reason: 'URL invalide' };
    }
    if (url.protocol !== 'https:') {
      return { ok: false, reason: 'Seul https est autorisé' };
    }
    if (!ConsentsService.WEBHOOK_ALLOWLIST.includes(url.hostname)) {
      return { ok: false, reason: 'Domaine non autorisé (hors allowlist)' };
    }
    // Blocage des IP littérales privées / lien-local et résolution DNS contrôlée.
    const host = url.hostname;
    const candidates: string[] = [];
    if (isIP(host)) {
      candidates.push(host);
    } else {
      try {
        const results = await lookup(host, { all: true });
        results.forEach((r) => candidates.push(r.address));
      } catch {
        return { ok: false, reason: 'Résolution DNS impossible' };
      }
    }
    if (candidates.some((ip) => this.isPrivateAddress(ip))) {
      return { ok: false, reason: 'Adresse interne/lien-local refusée' };
    }
    return { ok: true };
  }

  private isPrivateAddress(ip: string): boolean {
    if (ip.startsWith('10.') || ip.startsWith('127.') || ip.startsWith('169.254.')) return true;
    if (ip.startsWith('192.168.')) return true;
    const m = /^172\.(\d+)\./.exec(ip);
    if (m && Number(m[1]) >= 16 && Number(m[1]) <= 31) return true;
    if (ip === '::1' || ip.toLowerCase().startsWith('fe80') || ip.toLowerCase().startsWith('fc') || ip.toLowerCase().startsWith('fd')) {
      return true;
    }
    return false;
  }
}
