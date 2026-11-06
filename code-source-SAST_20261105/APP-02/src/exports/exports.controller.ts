import {
  Controller,
  Get,
  Inject,
  NotFoundException,
  Param,
  UseGuards,
} from '@nestjs/common';
import { ApiBearerAuth, ApiOperation, ApiTags } from '@nestjs/swagger';
import { LabAuthGuard } from '../common/guards/lab-auth.guard';
import { LAB_STORE } from '../common/store/store.module';
import { LabStore } from '../common/store/store.interface';

@ApiTags('exports')
@ApiBearerAuth()
@Controller()
export class ExportsController {
  constructor(@Inject(LAB_STORE) private readonly store: LabStore) {}

  @Get('v1/exports/:exportId')
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: 'Téléchargement d\'un export agrégé.' })
  download(@Param('exportId') exportId: string) {
    const record = this.store.exports.get(exportId);
    if (!record) {
      throw new NotFoundException('Export introuvable');
    }
    // de propriété : un TPP télécharge l'export d'un autre TPP en incrémentant l'id.
    return {
      exportId: record.exportId,
      owner: record.ownerClientId,
      fileName: record.fileName,
      aggregatedData: record.aggregatedData,
      createdAt: record.createdAt,
    };
  }
}
