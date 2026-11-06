import { NestFactory } from '@nestjs/core';
import { AppModule } from './app.module';
import { LabHeadersInterceptor } from './common/lab-headers.interceptor';

/**
 * APP-10 OnboardR — service documents (NestJS).
 * Laboratoire defensif, donnees 100% fictives. Securise par defaut ; seuls les ecarts
 * listes (// GT: APP10-Vxxx) sont vulnerables.
 */
async function bootstrap() {
  const app = await NestFactory.create(AppModule, { bodyParser: true });
  app.useGlobalInterceptors(new LabHeadersInterceptor());
  const port = parseInt(process.env.PORT || '3000', 10);
  await app.listen(port, '0.0.0.0');
}
bootstrap();
