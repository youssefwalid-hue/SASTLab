import { Global, Module } from '@nestjs/common';
import { InMemoryStore } from './memory.store';

export const LAB_STORE = 'LAB_STORE';

/**
 * Câblage de la persistance.
 *
 * - local / standalone : InMemoryStore (défaut).
 * - prod : brancher ici une implémentation PostgreSQL+Redis derrière le même jeton
 *   LAB_STORE (sélection par APP_PROFILE / DB_URL / REDIS_URL). L'interface LabStore
 *   garantit que les contrôleurs restent inchangés.
 */
@Global()
@Module({
  providers: [
    InMemoryStore,
    { provide: LAB_STORE, useExisting: InMemoryStore },
  ],
  exports: [InMemoryStore, LAB_STORE],
})
export class StoreModule {}
