import { Injectable, NestMiddleware } from '@nestjs/common';
import { Request, Response, NextFunction } from 'express';

/**
 * Ajoute l'en-tête de laboratoire sur toutes les réponses.
 * Marque sans ambiguïté que l'environnement contient des DONNÉES 100 % FICTIVES.
 */
@Injectable()
export class LabHeaderMiddleware implements NestMiddleware {
  use(_req: Request, res: Response, next: NextFunction): void {
    res.setHeader('X-Lab-Environment', 'test-data-fictives');
    next();
  }
}
