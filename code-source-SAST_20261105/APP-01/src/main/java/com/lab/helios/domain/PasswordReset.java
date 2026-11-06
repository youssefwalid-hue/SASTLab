package com.lab.helios.domain;

import jakarta.persistence.*;
import java.time.Instant;

/**
 * Demande de reinitialisation de mot de passe.
 * Le lien absolu genere est stocke pour inspection dans la "boite de laboratoire" (canal secondaire fictif).
 */
@Entity
@Table(name = "password_reset")
public class PasswordReset {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false)
    private String email;

    @Column(nullable = false)
    private String token;

    /** URL absolue du lien de reinitialisation telle qu'envoyee dans le message fictif. */
    @Column(nullable = false, length = 1000)
    private String resetUrl;

    @Column(nullable = false)
    private Instant createdAt;

    public PasswordReset() {
    }

    public PasswordReset(String email, String token, String resetUrl, Instant createdAt) {
        this.email = email;
        this.token = token;
        this.resetUrl = resetUrl;
        this.createdAt = createdAt;
    }

    public Long getId() {
        return id;
    }

    public String getEmail() {
        return email;
    }

    public String getToken() {
        return token;
    }

    public String getResetUrl() {
        return resetUrl;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }
}
