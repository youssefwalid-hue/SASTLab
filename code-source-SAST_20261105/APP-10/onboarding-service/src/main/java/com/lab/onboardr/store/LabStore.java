package com.lab.onboardr.store;

import com.lab.onboardr.domain.Dossier;
import org.springframework.stereotype.Component;

import java.util.Collection;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Depot en memoire des dossiers KYC fictifs. Reinitialisable via /admin/reset.
 * Donnees 100% fictives, aucun secret reel.
 */
@Component
public class LabStore {

    private final Map<Long, Dossier> dossiers = new ConcurrentHashMap<>();
    // Dernier id seede : les creations ulterieures partent au-dessus pour ne jamais
    // collisionner un identifiant deterministe attendu par le validateur.
    private static final long LAST_SEEDED_ID = 1012;
    private final AtomicLong seq = new AtomicLong(LAST_SEEDED_ID);
    // Cache de laboratoire pour /api/config (voir V012, cache aussi assure au proxy).
    public final Map<String, String> configCache = new ConcurrentHashMap<>();

    public LabStore() {
        seed();
    }

    /** Recharge le jeu de donnees fictif de demonstration. */
    public synchronized void seed() {
        dossiers.clear();
        configCache.clear();
        seq.set(LAST_SEEDED_ID);

        // --- Identites deterministes attendues par le validateur (NE PAS MODIFIER) -------
        Dossier d1 = new Dossier(1001, "prospect1", "Camille Fictif",
                "+33100000001", "1 rue de la Demo, 75000 Paris");
        d1.email = "camille.fictif@onboardr.lab.exemple";
        d1.reviewerNotes = "Verification piece d'identite a faire (fictif)";
        d1.riskScore = 12;
        dossiers.put(d1.id, d1);

        Dossier d2 = new Dossier(1002, "prospect2", "Dominique Exemple",
                "+33100000002", "2 avenue du Test, 69000 Lyon");
        d2.email = "dominique.exemple@onboardr.lab.exemple";
        d2.reviewerNotes = "Justificatif de domicile recu (fictif)";
        d2.riskScore = 20;
        dossiers.put(d2.id, d2);

        // --- Volume de donnees fictives supplementaire (identites coherentes inter-services) ---
        // Statuts varies PENDING/APPROVED/REJECTED, proprietaires repartis sur les comptes prospects.
        seedExtra(1003, "prospect1", "Alex Demonstration",  "APPROVED",  "IN_REVIEW",  18, "+33100000003");
        seedExtra(1004, "prospect3", "Noa Specimen",        "PENDING",   "IN_REVIEW",  11, "+33100000004");
        seedExtra(1005, "prospect3", "Sacha Prototype",     "REJECTED",  "REJECTED",   64, "+33100000005");
        seedExtra(1006, "prospect4", "Lou Echantillon",     "PENDING",   "ON_HOLD",    27, "+33100000006");
        seedExtra(1007, "prospect2", "Remy Factice",        "APPROVED",  "CLEARED",    15, "+33100000007");
        seedExtra(1008, "prospect4", "Charlie Simule",      "PENDING",   "IN_REVIEW",  33, "+33100000008");
        seedExtra(1009, "prospect1", "Kim Fictif",          "REJECTED",  "REJECTED",   71, "+33100000009");
        seedExtra(1010, "prospect3", "Morgan Exemple",      "APPROVED",  "CLEARED",    22, "+33100000010");
        seedExtra(1011, "prospect2", "Eli Factice",         "PENDING",   "IN_REVIEW",  19, "+33100000011");
        seedExtra(1012, "prospect4", "Sam Prototype",       "APPROVED",  "CLEARED",    25, "+33100000012");
    }

    private void seedExtra(long id, String owner, String subject, String status,
                           String internalStatus, int riskScore, String phone) {
        Dossier d = new Dossier(id, owner, subject, phone,
                id + " rue des Donnees Fictives, 75000 Paris");
        d.status = status;
        d.internalStatus = internalStatus;
        d.riskScore = riskScore;
        d.email = slug(subject) + "@onboardr.lab.exemple";
        d.reviewerNotes = "Dossier fictif de laboratoire (" + status + ")";
        dossiers.put(id, d);
    }

    private static String slug(String name) {
        return name.toLowerCase().replace(' ', '.');
    }

    public Dossier get(long id) {
        return dossiers.get(id);
    }

    public Collection<Dossier> all() {
        return dossiers.values();
    }

    public Dossier create(String owner, String subject) {
        long id = seq.incrementAndGet();
        Dossier d = new Dossier(id, owner, subject, "", "");
        dossiers.put(id, d);
        return d;
    }
}
