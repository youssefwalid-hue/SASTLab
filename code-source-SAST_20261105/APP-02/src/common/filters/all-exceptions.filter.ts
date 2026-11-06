import {
  ArgumentsHost,
  Catch,
  ExceptionFilter,
  HttpException,
  HttpStatus,
} from '@nestjs/common';
import { Request, Response } from 'express';

/**
 * Filtre d'exception global.
 *
 * (entrée invalide) renvoie la trace complète (stack NestJS/TypeScript) au client.
 * Sur les routes sûres (/v1/secure, /admin), un message générique est renvoyé et la
 * trace reste côté serveur uniquement (comportement de référence sûr).
 */
@Catch()
export class AllExceptionsFilter implements ExceptionFilter {
  catch(exception: unknown, host: ArgumentsHost): void {
    const ctx = host.switchToHttp();
    const res = ctx.getResponse<Response>();
    const req = ctx.getRequest<Request>();
    const path = req.originalUrl || req.url;

    if (exception instanceof HttpException) {
      const status = exception.getStatus();
      res.status(status).json({
        statusCode: status,
        path,
        ...(typeof exception.getResponse() === 'object'
          ? (exception.getResponse() as object)
          : { message: exception.getResponse() }),
      });
      return;
    }

    const err = exception as Error;
    const isSecurePath = path.startsWith('/v1/secure') || path.startsWith('/admin');

    if (isSecurePath) {
      // Référence sûre : message générique, trace journalisée côté serveur uniquement.
      res.status(HttpStatus.INTERNAL_SERVER_ERROR).json({
        statusCode: 500,
        path,
        message: 'Internal server error',
      });
      return;
    }

    res.status(HttpStatus.INTERNAL_SERVER_ERROR).json({
      statusCode: 500,
      path,
      error: err?.name ?? 'Error',
      message: err?.message ?? 'Unexpected error',
      exception: err?.constructor?.name,
      stack: (err?.stack ?? '').split('\n').map((l) => l.trim()),
    });
  }
}
