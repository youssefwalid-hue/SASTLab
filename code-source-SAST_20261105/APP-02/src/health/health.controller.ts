import { Controller, Get } from '@nestjs/common';
import { ApiOperation, ApiTags } from '@nestjs/swagger';

@ApiTags('health')
@Controller()
export class HealthController {
  @Get('v1/health')
  @ApiOperation({ summary: 'État de santé de l\'API.' })
  health() {
    // Réponse volontairement dépourvue d'en-têtes de durcissement (GT: APP02-V015)
    // et servie par un Express exposant X-Powered-By (GT: APP02-V016) — voir main.ts.
    return { status: 'ok', service: 'agrega-open', env: 'lab-test-data-fictives' };
  }
}
