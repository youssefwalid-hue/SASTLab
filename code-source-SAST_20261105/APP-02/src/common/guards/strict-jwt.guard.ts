import {
  CanActivate,
  ExecutionContext,
  Injectable,
  UnauthorizedException,
} from '@nestjs/common';
import { IdpService } from '../../idp/idp.service';
import { AuthContext } from './lab-auth.guard';

/**
 * Garde d'authentification STRICTE des routes /v1/secure (référence sûre).
 * NC: APP02-N005 — Bearer uniquement, vérification via IdpService.verifyStrict
 * (alg épinglé RS256, aud + iss vérifiés). X-API-Key non accepté ici.
 */
@Injectable()
export class StrictJwtGuard implements CanActivate {
  constructor(private readonly idp: IdpService) {}

  canActivate(context: ExecutionContext): boolean {
    const req = context.switchToHttp().getRequest();
    const authorization: string | undefined = req.header('authorization');
    const match = /^Bearer (.+)$/i.exec(authorization ?? '');
    if (!match) {
      throw new UnauthorizedException('Jeton d\'accès manquant');
    }
    try {
      const payload = this.idp.verifyStrict(match[1]); // NC: APP02-N005
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
      throw new UnauthorizedException('Jeton d\'accès invalide (audience/algorithme/signature)');
    }
  }
}
