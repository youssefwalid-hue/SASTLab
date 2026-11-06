import { Injectable } from '@nestjs/common';

export interface StoredDoc {
  id: string;
  onboardingId: number;
  owner: string;      // username proprietaire
  filename: string;
  contentType: string;
  content: string;    // contenu fictif (texte / apercu)
  source: string;     // 'seed' | 'upload' | 'fetch'
  // Champs internes de back-office — ne doivent JAMAIS etre exposes a l'appelant
  // (cf. GT: APP10-V021 excessive data exposure vs NC: APP10-N013 vue filtree).
  ownerEmail?: string;
  reviewerNotes?: string;
  internalRef?: string;
}

/**
 * Depot en memoire des pieces justificatives fictives. Reinitialisable via /internal/reset.
 */
@Injectable()
export class DocStore {
  private docs = new Map<string, StoredDoc>();
  // Dossiers d'onboarding connus (proprietaire) — coherent avec le service onboarding.
  knownOnboarding = new Map<number, string>();
  private counter = 0;

  constructor() {
    this.seed();
  }

  seed() {
    this.docs.clear();
    this.knownOnboarding.clear();
    this.counter = 0;

    // Dossiers connus (proprietaire) — MEMES identites que le service onboarding (Java).
    const owners: Array<[number, string]> = [
      [1001, 'prospect1'], [1002, 'prospect2'], [1003, 'prospect1'], [1004, 'prospect3'],
      [1005, 'prospect3'], [1006, 'prospect4'], [1007, 'prospect2'], [1008, 'prospect4'],
      [1009, 'prospect1'], [1010, 'prospect3'], [1011, 'prospect2'], [1012, 'prospect4'],
    ];
    for (const [oid, owner] of owners) this.knownOnboarding.set(oid, owner);

    // --- Pieces deterministes attendues par le validateur (NE PAS MODIFIER) -----------
    this.put({
      id: 'DOC-1001-A', onboardingId: 1001, owner: 'prospect1',
      filename: 'justificatif-1001.pdf', contentType: 'application/pdf',
      content: 'Justificatif fictif du dossier 1001', source: 'seed',
      ownerEmail: 'camille.fictif@onboardr.lab.exemple',
      reviewerNotes: 'Verification piece d\'identite a faire (fictif)',
      internalRef: 'KYC-REF-1001',
    });
    this.put({
      id: 'DOC-1002-A', onboardingId: 1002, owner: 'prospect2',
      filename: 'justificatif-1002.pdf', contentType: 'application/pdf',
      content: 'Justificatif fictif du dossier 1002', source: 'seed',
      ownerEmail: 'dominique.exemple@onboardr.lab.exemple',
      reviewerNotes: 'Justificatif de domicile recu (fictif)',
      internalRef: 'KYC-REF-1002',
    });

    // --- Volume de pieces fictives supplementaire (coherent avec les dossiers) ---------
    const extra: Array<[number, string, string]> = [
      [1003, 'prospect1', 'alex.demonstration@onboardr.lab.exemple'],
      [1005, 'prospect3', 'sacha.prototype@onboardr.lab.exemple'],
      [1007, 'prospect2', 'remy.factice@onboardr.lab.exemple'],
      [1010, 'prospect3', 'morgan.exemple@onboardr.lab.exemple'],
      [1012, 'prospect4', 'sam.prototype@onboardr.lab.exemple'],
    ];
    for (const [oid, owner, email] of extra) {
      this.put({
        id: `DOC-${oid}-A`, onboardingId: oid, owner,
        filename: `justificatif-${oid}.pdf`, contentType: 'application/pdf',
        content: `Justificatif fictif du dossier ${oid}`, source: 'seed',
        ownerEmail: email,
        reviewerNotes: `Dossier fictif de laboratoire (${oid})`,
        internalRef: `KYC-REF-${oid}`,
      });
    }
  }

  put(doc: StoredDoc) {
    this.docs.set(doc.id, doc);
  }

  get(id: string): StoredDoc | undefined {
    return this.docs.get(id);
  }

  listForOnboarding(onboardingId: number): StoredDoc[] {
    return [...this.docs.values()].filter((d) => d.onboardingId === onboardingId);
  }

  nextId(onboardingId: number): string {
    this.counter += 1;
    return `DOC-${onboardingId}-U${this.counter}`;
  }
}
