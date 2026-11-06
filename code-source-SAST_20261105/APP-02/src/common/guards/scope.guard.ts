import {
  CanActivate,
  ExecutionContext,
  ForbiddenException,
  Injectable,
  SetMetadata,
} from '@nestjs/common';
import { Reflector } from '@nestjs/core';
import { AuthContext } from './lab-auth.guard';

export const REQUIRED_SCOPE = 'required_scope';
/** Déclare le scope OAuth2 requis sur un handler (référence sûre). */
export const Scopes = (scope: string) => SetMetadata(REQUIRED_SCOPE, scope);

/**
 * Garde de scope par ressource (référence sûre).
 * NC: APP02-N006 — refuse (403) si le scope requis n'est pas porté par le jeton.
 */
@Injectable()
export class ScopeGuard implements CanActivate {
  constructor(private readonly reflector: Reflector) {}

  canActivate(context: ExecutionContext): boolean {
    const required = this.reflector.get<string>(REQUIRED_SCOPE, context.getHandler());
    if (!required) {
      return true;
    }
    const req = context.switchToHttp().getRequest();
    const auth: AuthContext | undefined = req.auth;
    if (!auth || !auth.scopes.includes(required)) {
      throw new ForbiddenException(`Scope requis manquant : ${required}`);
    }
    return true;
  }
}
