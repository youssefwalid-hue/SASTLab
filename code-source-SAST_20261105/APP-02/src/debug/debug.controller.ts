import { Controller, Get } from '@nestjs/common';
import { ApiExcludeEndpoint, ApiTags } from '@nestjs/swagger';
import { RequestLogService } from '../common/request-log.service';

@ApiTags('debug')
@Controller()
export class DebugController {
  constructor(private readonly requestLog: RequestLogService) {}

  // en clair (secrets rejouables et traçables).
  @Get('debug/requestlog')
  @ApiExcludeEndpoint()
  requestlog() {
    return { entries: this.requestLog.all() };
  }
}
