import { Controller, Post, Req, Res, HttpStatus } from '@nestjs/common';
import type { Request, Response } from 'express';
import { DocStore } from '../common/doc-store.service';

/**
 * Reinitialisation interne du depot documents. Appelee par le service onboarding lors de
 * POST /admin/reset. Protegee par le secret de passerelle (jamais expose au client, et
 * cette route n'est pas publiee par le reverse proxy).
 */
@Controller()
export class InternalController {
  private readonly gatewaySecret = process.env.GATEWAY_SECRET || 'lab-gw-6f2c1e9a-NON-DEVINABLE';

  constructor(private readonly store: DocStore) {}

  @Post('/internal/reset')
  reset(@Req() req: Request, @Res() res: Response) {
    const secret = req.headers['x-gateway-secret'];
    if (secret !== this.gatewaySecret) {
      return res.status(HttpStatus.FORBIDDEN).json({ error: 'acces interne refuse' });
    }
    this.store.seed();
    return res.status(HttpStatus.OK).json({ status: 'reset' });
  }
}
