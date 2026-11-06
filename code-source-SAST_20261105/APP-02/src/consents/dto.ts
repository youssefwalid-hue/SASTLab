import { IsArray, IsOptional, IsString, IsUrl } from 'class-validator';
import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';

/**
 * DTO STRICT utilisé par la route sûre /v1/secure/consents (NC: APP02-N003).
 * Les champs liables sont en liste blanche ; `scopes` n'est pas déclaré et sera donc
 * rejeté/ignoré (dérivation serveur). callbackUrl validée (NC: APP02-N008).
 */
export class SecureCreateConsentDto {
  @ApiProperty()
  @IsString()
  user!: string;

  @ApiProperty({ type: [String] })
  @IsArray()
  @IsString({ each: true })
  permissions!: string[];

  @ApiPropertyOptional()
  @IsOptional()
  @IsUrl({ require_protocol: true })
  callbackUrl?: string;
}
