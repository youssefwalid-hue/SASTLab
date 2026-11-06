import { Global, Module } from '@nestjs/common';
import { RequestLogService } from './request-log.service';

@Global()
@Module({
  providers: [RequestLogService],
  exports: [RequestLogService],
})
export class CommonModule {}
