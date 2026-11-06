import { Injectable, Logger, OnModuleInit } from '@nestjs/common';
import {
  generateKeyPairSync,
  createPublicKey,
  createHmac,
  timingSafeEqual,
  KeyObject,
} from 'crypto';
import * as jwt from 'jsonwebtoken';

/**
 * IdP de LABORATOIRE intégré.
 *
 * - Génère une paire de clés RSA de TEST au démarrage (aucun secret réel).
 * - Publie la clé publique en JWKS sur /.well-known/jwks.json (kid=lab-1).
 * - Émet des JWT RS256 pour le grant client_credentials.
 *
 * Deux chemins de vérification coexistent :
 *  - verifyVulnerable()  : utilisé par les routes /v1 (écarts V005 aud ignorée + V006 confusion d'algo).
 *  - verifyStrict()      : utilisé par les routes /v1/secure (aud+iss vérifiés, alg épinglé RS256).
 */
@Injectable()
export class IdpService implements OnModuleInit {
  private readonly logger = new Logger('IdpService');

  readonly kid = 'lab-1';
  readonly issuer = 'https://agrega-idp.lab.exemple';
  readonly audience = 'agrega-api';

  private privateKeyPem!: string;
  publicKeyPem!: string;
  private publicKeyObject!: KeyObject;

  onModuleInit(): void {
    // Clé RSA de TEST générée localement au boot — jamais persistée, jamais réelle.
    const { publicKey, privateKey } = generateKeyPairSync('rsa', {
      modulusLength: 2048,
      publicKeyEncoding: { type: 'spki', format: 'pem' },
      privateKeyEncoding: { type: 'pkcs8', format: 'pem' },
    });
    this.privateKeyPem = privateKey;
    this.publicKeyPem = publicKey;
    this.publicKeyObject = createPublicKey(publicKey);
    this.logger.log(`IdP de laboratoire prêt (kid=${this.kid}, alg=RS256).`);
  }

  /** JWKS public (RFC 7517) — exposé sur /.well-known/jwks.json. */
  jwks(): { keys: Array<Record<string, string>> } {
    const jwk = this.publicKeyObject.export({ format: 'jwk' }) as {
      n: string;
      e: string;
      kty: string;
    };
    return {
      keys: [
        {
          kty: jwk.kty,
          n: jwk.n,
          e: jwk.e,
          alg: 'RS256',
          use: 'sig',
          kid: this.kid,
        },
      ],
    };
  }

  /**
   * Émet un jeton d'accès RS256 (grant client_credentials).
   * `audience` est paramétrable : en usage normal `agrega-api`, mais l'appelant peut
   * en demander une autre (utile pour mettre en évidence V005).
   */
  issueToken(params: {
    clientId: string;
    scope: string;
    audience?: string;
    expiresInSec?: number;
  }): { token: string; expiresIn: number } {
    const expiresIn = params.expiresInSec ?? 3600;
    const token = jwt.sign(
      {
        sub: params.clientId,
        client_id: params.clientId,
        scope: params.scope,
      },
      this.privateKeyPem,
      {
        algorithm: 'RS256',
        keyid: this.kid,
        issuer: this.issuer,
        audience: params.audience ?? this.audience,
        expiresIn,
      },
    );
    return { token, expiresIn };
  }

  /**
   * Vérification VULNÉRABLE (routes /v1).
   *  - GT: APP02-V006 — accepte HS256 signé avec le PEM de la clé PUBLIQUE comme secret
   *    HMAC (algorithme non épinglé : la liste autorise RS256 ET HS256).
   *  - GT: APP02-V005 — ne vérifie PAS le claim `aud` (aucune contrainte d'audience).
   */
  verifyVulnerable(token: string): jwt.JwtPayload {
    const decoded = jwt.decode(token, { complete: true });
    const alg = decoded?.header?.alg;

    if (alg === 'HS256') {
      // PUBLIQUE (PEM) est utilisée comme secret HMAC, l'algorithme n'est pas épinglé.
      // (Implémentation manuelle car jsonwebtoken refuserait ce schéma ; elle reproduit
      // fidèlement le bug d'un code applicatif qui réutilise la clé JWKS comme secret.)
      const [h, p, s] = token.split('.');
      const expected = createHmac('sha256', this.publicKeyPem)
        .update(`${h}.${p}`)
        .digest();
      const provided = Buffer.from(s, 'base64url');
      if (
        expected.length !== provided.length ||
        !timingSafeEqual(expected, provided)
      ) {
        throw new Error('invalid HS256 signature');
      }
      const payload = JSON.parse(
        Buffer.from(p, 'base64url').toString('utf8'),
      ) as jwt.JwtPayload;
      if (payload.exp && Date.now() / 1000 > payload.exp) {
        throw new Error('token expired');
      }
      if (payload.iss && payload.iss !== this.issuer) {
        throw new Error('invalid issuer');
      }
      return payload;
    }

    // RS256 (cas nominal) — mais toujours sans vérification d'audience (V005).
    return jwt.verify(token, this.publicKeyObject, {
      algorithms: ['RS256'],
      issuer: this.issuer,
    }) as jwt.JwtPayload;
  }

  /**
   * Vérification STRICTE (routes /v1/secure) — comportement de référence sûr.
   * Algorithme épinglé RS256, `aud` et `iss` vérifiés. (NC: APP02-N005)
   */
  verifyStrict(token: string): jwt.JwtPayload {
    return jwt.verify(token, this.publicKeyObject, {
      algorithms: ['RS256'], // NC: APP02-N005 — alg épinglé, HS256 refusé.
      issuer: this.issuer,
      audience: this.audience, // NC: APP02-N005 — audience vérifiée.
    }) as jwt.JwtPayload;
  }
}
