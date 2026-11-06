import { Injectable } from '@nestjs/common';
import * as crypto from 'crypto';
import type { Request } from 'express';

export interface SessionUser {
  username: string;
  role: 'PROSPECT' | 'CONFORMITE';
}

const ACCOUNTS: Record<string, 'PROSPECT' | 'CONFORMITE'> = {
  prospect1: 'PROSPECT',
  prospect2: 'PROSPECT',
  prospect3: 'PROSPECT',
  prospect4: 'PROSPECT',
  conformite1: 'CONFORMITE',
};

/**
 * Validation du cookie de session OBRSESSION (HMAC-SHA256), format
 * "base64url(username|ROLE).base64url(hmac)". Meme secret que le service onboarding (Java).
 * Mecanisme correct : signature verifiee, comparaison a temps constant. Pas un ecart.
 */
@Injectable()
export class SessionService {
  private readonly secret = Buffer.from(
    process.env.SESSION_SECRET || 'lab-onboardr-session-secret-fictif',
    'utf8',
  );

  private readCookie(req: Request): string | null {
    const raw = req.headers['cookie'];
    if (!raw) return null;
    for (const part of raw.split(';')) {
      const [k, ...v] = part.trim().split('=');
      if (k === 'OBRSESSION') return v.join('=');
    }
    return null;
  }

  current(req: Request): SessionUser | null {
    const token = this.readCookie(req);
    if (!token) return null;
    const dot = token.indexOf('.');
    if (dot <= 0) return null;
    try {
      const payload = Buffer.from(token.slice(0, dot), 'base64url').toString('utf8');
      const provided = Buffer.from(token.slice(dot + 1), 'base64url');
      const expected = crypto.createHmac('sha256', this.secret).update(payload, 'utf8').digest();
      if (expected.length !== provided.length || !crypto.timingSafeEqual(expected, provided)) {
        return null;
      }
      const [username, role] = payload.split('|');
      if (!ACCOUNTS[username] || (role !== 'PROSPECT' && role !== 'CONFORMITE')) return null;
      return { username, role };
    } catch {
      return null;
    }
  }
}
