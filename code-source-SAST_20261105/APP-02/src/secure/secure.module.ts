import { Module } from '@nestjs/common';
import { SecureController } from './secure.controller';
import { AccountsModule } from '../accounts/accounts.module';
import { ConsentsModule } from '../consents/consents.module';
import { NotificationsModule } from '../notifications/notifications.module';
import { ReportsModule } from '../reports/reports.module';

@Module({
  imports: [AccountsModule, ConsentsModule, NotificationsModule, ReportsModule],
  controllers: [SecureController],
})
export class SecureModule {}
