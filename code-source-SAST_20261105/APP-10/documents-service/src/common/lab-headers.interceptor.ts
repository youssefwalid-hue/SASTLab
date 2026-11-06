import { CallHandler, ExecutionContext, Injectable, NestInterceptor } from '@nestjs/common';
import { Observable } from 'rxjs';

/**
 * Ajoute le marqueur de laboratoire et les en-tetes de diagnostic amont a toutes les reponses.
 * Les en-tetes X-Service* sont des en-tetes amont "normaux" ; c'est le reverse proxy qui doit
 * les masquer en sortie (ecart APP10-V011 porte cote proxy).
 */
@Injectable()
export class LabHeadersInterceptor implements NestInterceptor {
  intercept(context: ExecutionContext, next: CallHandler): Observable<any> {
    const res = context.switchToHttp().getResponse();
    res.setHeader('X-Lab-Environment', 'test-data-fictives');
    res.setHeader('X-Service', 'doc-service');
    res.setHeader('X-Service-Version', '1.0.0');
    res.setHeader('X-Upstream', 'documents:3000');
    return next.handle();
  }
}
