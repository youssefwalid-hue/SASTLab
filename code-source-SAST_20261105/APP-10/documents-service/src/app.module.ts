import { Module } from '@nestjs/common';
import { DocumentsController } from './documents/documents.controller';
import { EnrichmentController } from './enrichment/enrichment.controller';
import { SupportController } from './support/support.controller';
import { HealthController } from './health/health.controller';
import { InternalController } from './internal/internal.controller';
import { DocStore } from './common/doc-store.service';
import { SessionService } from './auth/session.service';

@Module({
  controllers: [
    DocumentsController,
    EnrichmentController,
    SupportController,
    HealthController,
    InternalController,
  ],
  providers: [DocStore, SessionService],
})
export class AppModule {}
