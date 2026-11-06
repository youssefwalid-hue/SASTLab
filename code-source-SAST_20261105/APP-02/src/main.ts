import 'reflect-metadata';
import { NestFactory } from '@nestjs/core';
import { Logger } from '@nestjs/common';
import { DocumentBuilder, SwaggerModule } from '@nestjs/swagger';
import { AppModule } from './app.module';
import { AllExceptionsFilter } from './common/filters/all-exceptions.filter';

/**
 * Agréga Open — API d'agrégation de comptes (Open Banking) — LABORATOIRE DAST.
 * DONNÉES 100 % FICTIVES — usage défensif (test de scanners). Sécurisée par défaut,
 * sauf les 22 écarts volontaires (APP02-V001..V022).
 */
async function bootstrap() {
  const logger = new Logger('bootstrap');
  const app = await NestFactory.create(AppModule, {
    // Buffer les logs ; les traces d'erreur restent côté serveur (sauf écart V014 côté client).
    bufferLogs: false,
  });

  // (On n'appelle donc PAS app.disable('x-powered-by').)

  app.enableCors({
    origin: true, // reflète n'importe quelle Origin
    credentials: true, // Access-Control-Allow-Credentials: true
  });


  app.useGlobalFilters(new AllExceptionsFilter());

  // OpenAPI / Swagger (surface de laboratoire).
  const config = new DocumentBuilder()
    .setTitle('Agréga Open — API d\'agrégation de comptes (laboratoire DAST)')
    .setDescription(
      'API Open Banking volontairement vulnérable, données 100 % fictives, usage défensif. ' +
        'La surface /v1/secure regroupe les contrôles négatifs (routes sûres).',
    )
    .setVersion('1.0')
    .addBearerAuth()
    .addApiKey({ type: 'apiKey', name: 'X-API-Key', in: 'header' }, 'X-API-Key')
    .build();
  const document = SwaggerModule.createDocument(app, config);
  SwaggerModule.setup('docs', app, document);

  const profile = process.env.APP_PROFILE ?? 'local';
  const port =
    Number(process.env.PORT) || (profile === 'prod' || profile === 'standalone' ? 5000 : 8080);

  await app.listen(port, '0.0.0.0');
  logger.log(`Agréga Open démarré (profil=${profile}) sur le port ${port}. Docs: /docs`);
}

bootstrap();
