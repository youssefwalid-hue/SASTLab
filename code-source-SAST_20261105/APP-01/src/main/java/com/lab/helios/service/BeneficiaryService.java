package com.lab.helios.service;

import com.lab.helios.domain.Beneficiary;
import com.lab.helios.repo.BeneficiaryRepository;
import org.springframework.security.access.AccessDeniedException;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.time.temporal.ChronoUnit;
import java.util.List;
import java.util.concurrent.atomic.AtomicInteger;

/** Gestion des beneficiaires. */
@Service
public class BeneficiaryService {

    /** Delai de securite avant qu'un beneficiaire nouvellement cree soit utilisable (J+2). */
    private static final long ACTIVATION_DELAY_DAYS = 2;

    private final BeneficiaryRepository repository;
    private final AtomicInteger sequence = new AtomicInteger(1);

    public BeneficiaryService(BeneficiaryRepository repository) {
        this.repository = repository;
        resyncSequence();
    }

    public final void resyncSequence() {
        int max = repository.findAll().stream()
                .map(Beneficiary::getId)
                .filter(id -> id != null && id.startsWith("BEN-"))
                .mapToInt(id -> {
                    try {
                        return Integer.parseInt(id.substring(4));
                    } catch (NumberFormatException e) {
                        return 0;
                    }
                })
                .max().orElse(0);
        sequence.set(max + 1);
    }

    public List<Beneficiary> listOf(String username) {
        return repository.findByOwnerUsername(username);
    }

    /**
     * Recherche benigne des beneficiaires de l'utilisateur par fragment de nom.
     * Requete derivee parametree (Spring Data) et portee limitee au proprietaire : route sure,
     * sans ecart ni negative control (surface de crawl).
     */
    public List<Beneficiary> searchOwn(String username, String name) {
        if (name == null || name.isBlank()) {
            return repository.findByOwnerUsername(username);
        }
        return repository.findByOwnerUsernameAndNameContainingIgnoreCase(username, name);
    }

    public Beneficiary getOwned(String id, String username) {
        Beneficiary b = repository.findById(id)
                .orElseThrow(() -> new AccessDeniedException("Beneficiaire introuvable"));
        if (!b.getOwnerUsername().equals(username)) {
            throw new AccessDeniedException("Acces refuse au beneficiaire " + id);
        }
        return b;
    }

    /**
     * Creation d'un beneficiaire. Le champ activatedAt est calcule cote serveur (J+2) :
     * c'est le comportement sur attendu, l'utilisateur ne le fournit jamais a la creation.
     */
    public Beneficiary create(String username, String name, String iban, String note) {
        String id = "BEN-" + String.format("%02d", sequence.getAndIncrement());
        // activatedAt gere cote serveur uniquement (valeur de confiance)
        Instant activatedAt = Instant.now().plus(ACTIVATION_DELAY_DAYS, ChronoUnit.DAYS);
        Beneficiary b = new Beneficiary(id, username, name, iban, note, activatedAt);
        return repository.save(b);
    }

    /**
     * Edition d'un beneficiaire via le formulaire.
     *
     * 
     * gere cote serveur, est lie depuis le corps de requete et ecrase la valeur de confiance,
     * ce qui permet d'antidater l'activation et de contourner le delai de securite.
     */
    public Beneficiary editInsecure(String id, String username, String name, String iban,
                                    String note, Instant activatedAtFromRequest) {
        Beneficiary b = getOwned(id, username);
        b.setName(name);
        b.setIban(iban);
        b.setNote(note);
        if (activatedAtFromRequest != null) {
            b.setActivatedAt(activatedAtFromRequest);
        }
        return repository.save(b);
    }

    public void deleteAll() {
        repository.deleteAll();
        resyncSequence();
    }
}
