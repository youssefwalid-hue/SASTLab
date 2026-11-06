package com.lab.helios.service;

import com.lab.helios.domain.PasswordReset;
import com.lab.helios.domain.User;
import com.lab.helios.repo.PasswordResetRepository;
import com.lab.helios.repo.UserRepository;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.util.Optional;
import java.util.UUID;

/** Reinitialisation de mot de passe (lien envoye dans une "boite de laboratoire" fictive). */
@Service
public class PasswordResetService {

    private final PasswordResetRepository repository;
    private final UserRepository userRepository;

    /** Nom d'hote canonique configure cote serveur, utilise par la route sure. */
    private final String canonicalBaseUrl;

    public PasswordResetService(PasswordResetRepository repository,
                                UserRepository userRepository,
                                @Value("${helios.canonical-base-url}") String canonicalBaseUrl) {
        this.repository = repository;
        this.userRepository = userRepository;
        this.canonicalBaseUrl = canonicalBaseUrl;
    }

    /**
     * Genere un lien de reinitialisation a partir de l'hote de la requete entrante.
     *
     * 
     * construite a partir du schema/host recus (Host / X-Forwarded-Host) sans canonicalisation,
     * si bien qu'un Host force dirige le jeton vers un domaine arbitraire.
     */
    public PasswordReset requestInsecure(String email, String requestScheme, String requestHostHeader) {
        String token = newToken();
        String base = requestScheme + "://" + requestHostHeader;
        String resetUrl = base + "/password/reset/confirm?token=" + token;
        return persist(email, token, resetUrl);
    }

    /**
     * Genere un lien de reinitialisation a partir d'un nom d'hote canonique.
     *
     * // NC: APP01-N007 — URL de base canonique : le Host entrant est ignore, le lien est
     * toujours construit depuis la configuration serveur ; aucun empoisonnement possible.
     */
    public PasswordReset requestSecure(String email) {
        String token = newToken();
        // NC: APP01-N007 — base canonique configuree cote serveur, Host entrant ignore
        String resetUrl = canonicalBaseUrl + "/password/reset/confirm?token=" + token;
        return persist(email, token, resetUrl);
    }

    private PasswordReset persist(String email, String token, String resetUrl) {
        // On enregistre la demande meme si l'email est inconnu (reponse uniforme, pas d'enumeration).
        PasswordReset pr = new PasswordReset(email, token, resetUrl, Instant.now());
        return repository.save(pr);
    }

    private String newToken() {
        return UUID.randomUUID().toString().replace("-", "");
    }

    public Optional<User> findUserByEmail(String email) {
        return userRepository.findByEmailIgnoreCase(email);
    }

    public Optional<PasswordReset> lastFor(String email) {
        return repository.findTopByEmailOrderByCreatedAtDesc(email);
    }

    public void deleteAll() {
        repository.deleteAll();
    }
}
