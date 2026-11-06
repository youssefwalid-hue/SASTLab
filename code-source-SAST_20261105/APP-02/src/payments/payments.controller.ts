import { Controller, Get, Inject, Req, UseGuards } from '@nestjs/common';
import { ApiBearerAuth, ApiOperation, ApiTags } from '@nestjs/swagger';
import { LabAuthGuard, AuthContext } from '../common/guards/lab-auth.guard';
import { LAB_STORE } from '../common/store/store.module';
import { LabStore } from '../common/store/store.interface';

@ApiTags('payments')
@ApiBearerAuth()
@Controller()
export class PaymentsController {
  constructor(@Inject(LAB_STORE) private readonly store: LabStore) {}

  @Get('v1/payments')
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: 'Liste des paiements du périmètre du TPP.' })
  list(@Req() req: any) {
    const auth: AuthContext = req.auth;
    // portant que `accounts:read` accède tout de même aux paiements.
    const payments = this.store.payments.filter((p) => p.ownerClientId === auth.clientId);
    return { payments };
  }
}
