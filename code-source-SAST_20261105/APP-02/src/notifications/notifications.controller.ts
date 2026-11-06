import {
  Body,
  Controller,
  Get,
  HttpCode,
  Post,
  Query,
  Req,
  Res,
  UseGuards,
} from '@nestjs/common';
import { Response } from 'express';
import { ApiBearerAuth, ApiOperation, ApiTags } from '@nestjs/swagger';
import { NotificationsService } from './notifications.service';
import { LabAuthGuard, AuthContext } from '../common/guards/lab-auth.guard';

/**
 * Notifications — surface /v1 (routes bénignes + écarts V018 / V021).
 */
@ApiTags('notifications')
@ApiBearerAuth()
@Controller()
export class NotificationsController {
  constructor(private readonly notifications: NotificationsService) {}

  // Route BÉNIGNE : liste des notifications du TPP (scoping par client, aucune donnée tierce).
  @Get('v1/notifications')
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: 'Liste des notifications du TPP.' })
  list(@Req() req: any) {
    const auth: AuthContext = req.auth;
    return { notifications: this.notifications.listForClient(auth.clientId) };
  }

  // dans une réponse HTML.
  @Get('v1/notifications/preview')
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: 'Aperçu HTML d\'une notification (paramètre message).' })
  preview(@Query('message') message: string, @Res() res: Response): void {
    const html = this.notifications.wrapPreview(message ?? ''); 
    res.type('html').send(html);
  }

  @Post('v1/notifications/render')
  @HttpCode(200)
  @UseGuards(LabAuthGuard)
  @ApiOperation({ summary: 'Rendu d\'un modèle de notification (champ template).' })
  render(@Body() body: any) {
    const rendered = this.notifications.renderUnsafe(String(body?.template ?? ''));
    return { rendered };
  }
}
