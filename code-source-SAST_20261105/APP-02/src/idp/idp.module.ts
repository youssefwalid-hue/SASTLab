import { Global, Module } from '@nestjs/common';
import { IdpService } from './idp.service';
import { OAuthController } from './oauth.controller';

@Global()
@Module({
  controllers: [OAuthController],
  providers: [IdpService],
  exports: [IdpService],
})
export class IdpModule {}
