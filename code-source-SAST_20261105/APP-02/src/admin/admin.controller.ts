import { Controller, HttpCode, Inject, Post } from '@nestjs/common';
import { ApiExcludeEndpoint, ApiTags } from '@nestjs/swagger';
import { LAB_STORE } from '../common/store/store.module';
import { LabStore } from '../common/store/store.interface';

/**
 * Administration de laboratoire : remise à zéro de l'état (données fictives).
 * Utilisé par le validateur de benchmark entre chaque cas.
 */
@ApiTags('admin')
@Controller()
export class AdminController {
  constructor(@Inject(LAB_STORE) private readonly store: LabStore) {}

  @Post('admin/reset')
  @HttpCode(200)
  @ApiExcludeEndpoint()
  reset() {
    this.store.reset();
    return { status: 'reset', message: 'État de laboratoire réinitialisé (données fictives).' };
  }
}
