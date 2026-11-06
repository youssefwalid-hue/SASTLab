import {
  CanActivate,
  ExecutionContext,
  Injectable,
  UnauthorizedException,
  Inject,
} from '@nestjs/common';
import { IdpService } from '../../idp/idp.service';
import { RequestLogService } from '../request-log.service';
import { LAB_STORE } from '../store/store.module';
import { LabStore } from '../store/store.interface';

export interface AuthContext {
  clientId: string;
  role: 'tpp' | 'admin';
  scopes: string[];
  aud?: string | string[];
  via: 'jwt' | 'api-key';
}

/**
 * Garde d'authentification des routes /v1 (surface VULNÉRABLE).
 *
 * Accepte deux modes :
 *  - Bearer JWT, vérifié via IdpService.verifyVulnerable (porte les écarts V005/V006).
 *  - X-API-Key statique — GT: APP02-V012 (clé rejouable, journalisée en clair).
 *
 * Elle authentifie uniquement : l'application fine du scope et le contrôle de propriété
 * sont faits (ou volontairement omis : V001/V002/V007/V011) dans les contrôleurs.
 */
@Injectable()
export class LabAuthGuard implements CanActivate {
  constructor(
    private readonly idp: IdpService,
    private readonly requestLog: RequestLogService,
    @Inject(LAB_STORE) private readonly store: LabStore,
  ) {}

  canActivate(context: ExecutionContext): boolean {
    const req = context.switchToHttp().getRequest();
    const apiKey: string | undefined = req.header('x-api-key');
    const authorization: string | undefined = req.header('authorization');

    if (apiKey) {
      this.requestLog.record({
        method: req.method,
        path: req.originalUrl || req.url,
        apiKey,
        authorization,
      });
      const client = [...this.store.clients.values()].find((c) => c.apiKey === apiKey);
      if (!client) {
        throw new UnauthorizedException('Clé d\'API invalide');
      }
      req.auth = {
        clientId: client.clientId,
        role: client.role,
        scopes: ['accounts:read', 'payments:read', 'consents:write'],
        via: 'api-key',
      } as AuthContext;
      return true;
    }

    this.requestLog.record({
      method: req.method,
      path: req.originalUrl || req.url,
      authorization,
    });

    const match = /^Bearer (.+)$/i.exec(authorization ?? '');
    if (!match) {
      throw new UnauthorizedException('Jeton d\'accès manquant');
    }
    try {
      const payload = this.idp.verifyVulnerable(match[1]);
      req.auth = {
        clientId: (payload.client_id as string) || (payload.sub as string),
        role: 'tpp',
        scopes: String(payload.scope ?? '')
          .split(' ')
          .filter(Boolean),
        aud: payload.aud,
        via: 'jwt',
      } as AuthContext;
      return true;
    } catch {
      throw new UnauthorizedException('Jeton d\'accès invalide');
    }
  }
}
