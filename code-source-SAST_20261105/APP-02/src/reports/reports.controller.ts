import { Controller, Get, Query, Req, Res, UseGuards } from '@nestjs/common';
import { Response } from 'express';
import { ApiBearerAuth, ApiOperation, ApiTags } from '@nestjs/swagger';
import { ReportsService } from './reports.service';
import { LabAuthGuard, AuthContext } from '../common/guards/lab-auth.guard';

/**
 * Rapports / exports fichier — surface /v1 (écarts V019 / V020).
 */
@ApiTags('reports')
@ApiBearerAuth()
@Controller()
export class ReportsController {
  constructor(private readonly reports: ReportsService) {}

  // sanitation pour lire un fichier sur le disque.
  @Get('v1/reports/download')
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: 'Téléchargement d\'un fichier de rapport (paramètre name).' })
  download(@Query('name') name: string, @Res() res: Response): void {
    const content = this.reports.readUnsafe(name ?? ''); 
    res.type('text/plain').send(content);
  }

  @Get('v1/reports/csv')
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: 'Export CSV des consentements du TPP.' })
  csv(@Req() req: any, @Res() res: Response): void {
    const auth: AuthContext = req.auth;
    const csv = this.reports.buildCsvUnsafe(auth.clientId); 
    res.type('text/csv').send(csv);
  }
}
