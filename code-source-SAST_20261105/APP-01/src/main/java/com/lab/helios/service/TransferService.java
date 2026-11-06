package com.lab.helios.service;

import com.lab.helios.domain.Beneficiary;
import com.lab.helios.domain.Transfer;
import com.lab.helios.domain.TransferStatus;
import com.lab.helios.repo.TransferRepository;
import org.springframework.security.access.AccessDeniedException;
import org.springframework.stereotype.Service;

import java.math.BigDecimal;
import java.time.Instant;
import java.util.List;
import java.util.concurrent.atomic.AtomicInteger;

/** Gestion des virements avec validation OTP (OTP de laboratoire fixe : 123456). */
@Service
public class TransferService {

    public static final String LAB_OTP = "123456";

    private final TransferRepository repository;
    private final BeneficiaryService beneficiaryService;
    private final AtomicInteger sequence = new AtomicInteger(1);

    public TransferService(TransferRepository repository, BeneficiaryService beneficiaryService) {
        this.repository = repository;
        this.beneficiaryService = beneficiaryService;
        resyncSequence();
    }

    public final void resyncSequence() {
        int max = repository.findAll().stream()
                .map(Transfer::getId)
                .filter(id -> id != null && id.startsWith("TRF-"))
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

    public List<Transfer> listOf(String username) {
        return repository.findByOwnerUsername(username);
    }

    public Transfer getOwned(String id, String username) {
        Transfer t = repository.findById(id)
                .orElseThrow(() -> new AccessDeniedException("Virement introuvable"));
        if (!t.getOwnerUsername().equals(username)) {
            throw new AccessDeniedException("Acces refuse au virement " + id);
        }
        return t;
    }

    /** Initialise un virement en attente de validation OTP. Verifie la propriete du beneficiaire. */
    public Transfer create(String username, String sourceAccount, String beneficiaryId, BigDecimal amount) {
        Beneficiary ben = beneficiaryService.getOwned(beneficiaryId, username);
        String id = "TRF-" + String.format("%02d", sequence.getAndIncrement());
        Transfer t = new Transfer(id, username, sourceAccount, ben.getId(), amount,
                TransferStatus.PENDING_OTP, false, Instant.now());
        return repository.save(t);
    }

    /** Etape legitime de validation du challenge OTP (positionne le drapeau otpValidated). */
    public boolean validateOtp(String id, String username, String otp) {
        Transfer t = getOwned(id, username);
        if (LAB_OTP.equals(otp)) {
            t.setOtpValidated(true);
            repository.save(t);
            return true;
        }
        return false;
    }

    /**
     * Confirmation de virement.
     *
     * 
     * NI le drapeau otpValidated NI la valeur de l'OTP soumis. La confirmation fait aveuglement
     * confiance a l'enchainement attendu, si bien qu'un OTP incorrect (ou aucun) execute quand meme.
     */
    public Transfer confirmInsecure(String id, String username, String otp) {
        Transfer t = getOwned(id, username);
        t.setStatus(TransferStatus.EXECUTED);
        return repository.save(t);
    }

    /**
     * Finalisation de virement.
     *
     * // NC: APP01-N005 — comportement sur : l'OTP est revifie et le drapeau otpValidated,
     * lie au virement courant, est exige avant tout passage a EXECUTED. Un OTP invalide ou
     * une etape OTP non franchie laisse le virement en PENDING_OTP.
     */
    public Transfer finalizeSecure(String id, String username, String otp) {
        Transfer t = getOwned(id, username);
        // NC: APP01-N005 — revification de l'OTP et de l'etat du challenge avant execution
        if (!LAB_OTP.equals(otp) || !t.isOtpValidated()) {
            throw new AccessDeniedException("OTP invalide ou non valide pour ce virement");
        }
        t.setStatus(TransferStatus.EXECUTED);
        t.setOtpValidated(false); // invalide l'OTP apres usage
        return repository.save(t);
    }

    public void deleteAll() {
        repository.deleteAll();
        resyncSequence();
    }
}
