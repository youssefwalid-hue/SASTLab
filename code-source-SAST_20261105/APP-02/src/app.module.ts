import { MiddlewareConsumer, Module, NestModule } from '@nestjs/common';
import { StoreModule } from './common/store/store.module';
import { CommonModule } from './common/common.module';
import { IdpModule } from './idp/idp.module';
import { AccountsModule } from './accounts/accounts.module';
import { PaymentsModule } from './payments/payments.module';
import { ConsentsModule } from './consents/consents.module';
import { ExportsModule } from './exports/exports.module';
import { NotificationsModule } from './notifications/notifications.module';
import { ReportsModule } from './reports/reports.module';
import { CatalogModule } from './catalog/catalog.module';
import { SecureModule } from './secure/secure.module';
import { AdminModule } from './admin/admin.module';
import { HealthModule } from './health/health.module';
import { DebugModule } from './debug/debug.module';
import { LabHeaderMiddleware } from './common/middleware/lab-header.middleware';

@Module({
  imports: [
    StoreModule,
    CommonModule,
    IdpModule,
    AccountsModule,
    PaymentsModule,
    ConsentsModule,
    ExportsModule,
    NotificationsModule,
    ReportsModule,
    CatalogModule,
    SecureModule,
    AdminModule,
    HealthModule,
    DebugModule,
  ],
})
export class AppModule implements NestModule {
  configure(consumer: MiddlewareConsumer): void {
    // En-tête X-Lab-Environment sur toutes les réponses.
    consumer.apply(LabHeaderMiddleware).forRoutes('*');
  }
}
