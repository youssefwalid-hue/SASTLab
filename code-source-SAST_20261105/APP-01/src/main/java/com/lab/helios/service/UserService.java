package com.lab.helios.service;

import com.lab.helios.domain.Role;
import com.lab.helios.domain.User;
import com.lab.helios.repo.UserRepository;
import org.springframework.security.access.AccessDeniedException;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.LinkedHashMap;
import java.util.Map;

/**
 * Service de profil utilisateur.
 *
 * Deux couples comparables :
 *  - mise a jour du profil  : ecart APP01-V017 (over-binding du role) vs NC APP01-N008 (liaison restreinte).
 *  - lecture d'un profil API : ecart APP01-V022 (exposition excessive) vs NC APP01-N013 (DTO minimal).
 */
@Service
public class UserService {

    private final UserRepository userRepository;

    public UserService(UserRepository userRepository) {
        this.userRepository = userRepository;
    }

    public User getByUsername(String username) {
        return userRepository.findByUsername(username)
                .orElseThrow(() -> new AccessDeniedException("Utilisateur introuvable"));
    }

    /**
     * Mise a jour du profil depuis le formulaire.
     *
     * 
     * etre modifiable par l'utilisateur, est lie depuis la requete et applique tel quel. Un parametre
     * role=ADMIN surajoute au POST eleve immediatement les privileges du compte (elevation verticale).
     */
    @Transactional
    public User updateProfileInsecure(String username, String displayName, String email, String role) {
        User u = getByUsername(username);
        if (displayName != null && !displayName.isBlank()) {
            u.setDisplayName(displayName);
        }
        if (email != null && !email.isBlank()) {
            u.setEmail(email);
        }
        if (role != null && !role.isBlank()) {
            try {
                u.setRole(Role.valueOf(role.trim().toUpperCase()));
            } catch (IllegalArgumentException ignored) {
                // role inconnu : ignore (le champ reste invalide), l'ecart porte sur les valeurs valides
            }
        }
        return userRepository.save(u);
    }

    /**
     * Mise a jour du profil par liaison restreinte (comportement sur de reference).
     *
     * // NC: APP01-N008 — seuls les champs legitimes (displayName, email) sont lies ; le role
     * est gere exclusivement cote serveur et n'est jamais pris depuis la requete, meme si un
     * parametre role=... est surajoute au POST.
     */
    @Transactional
    public User updateProfileSecure(String username, String displayName, String email) {
        User u = getByUsername(username);
        // NC: APP01-N008 — allowlist de champs ; "role" volontairement hors du binding
        if (displayName != null && !displayName.isBlank()) {
            u.setDisplayName(displayName);
        }
        if (email != null && !email.isBlank()) {
            u.setEmail(email);
        }
        return userRepository.save(u);
    }

    /**
     * Representation JSON complete d'un utilisateur.
     *
     * 
     * internes et sensibles (hash de mot de passe, role, identifiant interne) qui n'ont aucune
     * raison d'etre renvoyes a un client. Le serveur expose l'entite entiere au lieu d'un DTO.
     */
    public Map<String, Object> exposeFullInsecure(String username) {
        User u = getByUsername(username);
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("id", u.getId());
        body.put("username", u.getUsername());
        body.put("displayName", u.getDisplayName());
        body.put("email", u.getEmail());
        body.put("phone", u.getPhone());
        body.put("role", u.getRole() == null ? null : u.getRole().name());
        body.put("passwordHash", u.getPasswordHash());
        return body;
    }

    /**
     * Representation JSON minimale d'un utilisateur (comportement sur de reference).
     *
     * // NC: APP01-N013 — DTO d'affichage restreint aux seuls champs necessaires ; aucune donnee
     * interne ni sensible (ni hash, ni role, ni identifiant interne) n'est exposee.
     */
    public Map<String, Object> exposeMinimalSecure(String username) {
        User u = getByUsername(username);
        Map<String, Object> body = new LinkedHashMap<>();
        // NC: APP01-N013 — projection explicite vers un DTO d'affichage
        body.put("username", u.getUsername());
        body.put("displayName", u.getDisplayName());
        return body;
    }
}
