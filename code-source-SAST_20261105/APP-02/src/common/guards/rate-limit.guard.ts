import { CanActivate, ExecutionContext, Injectable, HttpException, HttpStatus } from '@nestjs/common';

/**
 * Limitation de débit simple en mémoire (référence sûre, profil local).
 * Appliquée aux routes /v1/secure comme mesure de durcissement.
 *
 * NB — GT: APP02-V010 : l'endpoint /oauth/token n'est VOLONTAIREMENT pas protégé
 * par cette garde (absence de rate limit sur l'émission de token).
 *
 * En profil prod, cette logique serait portée par Redis (fenêtre glissante partagée).
 */
@Injectable()
export class RateLimitGuard implements CanActivate {
  private readonly windowMs = 60_000;
  private readonly max = 300; // seuil large : ne gêne pas la validation automatisée.
  private hits = new Map<string, { count: number; reset: number }>();

  canActivate(context: ExecutionContext): boolean {
    const req = context.switchToHttp().getRequest();
    const key = `${req.ip}:${req.baseUrl || ''}${req.path}`;
    const now = Date.now();
    const entry = this.hits.get(key);
    if (!entry || entry.reset < now) {
      this.hits.set(key, { count: 1, reset: now + this.windowMs });
      return true;
    }
    entry.count += 1;
    if (entry.count > this.max) {
      throw new HttpException('Too Many Requests', HttpStatus.TOO_MANY_REQUESTS);
    }
    return true;
  }
}
