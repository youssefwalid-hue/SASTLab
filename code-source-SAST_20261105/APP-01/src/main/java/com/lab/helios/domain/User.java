package com.lab.helios.domain;

import jakarta.persistence.*;

/** Utilisateur de laboratoire (donnees fictives). */
@Entity
@Table(name = "app_user")
public class User {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false, unique = true)
    private String username;

    /** Hash BCrypt du mot de passe (jamais de secret reel). */
    @Column(nullable = false)
    private String passwordHash;

    @Column(nullable = false)
    private String email;

    /** Numero de telephone fictif de contact (donnees de laboratoire). */
    @Column
    private String phone;

    @Column(nullable = false)
    private String displayName;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private Role role;

    public User() {
    }

    public User(String username, String passwordHash, String email, String displayName, Role role) {
        this.username = username;
        this.passwordHash = passwordHash;
        this.email = email;
        this.displayName = displayName;
        this.role = role;
    }

    public User(String username, String passwordHash, String email, String phone, String displayName, Role role) {
        this(username, passwordHash, email, displayName, role);
        this.phone = phone;
    }

    public Long getId() {
        return id;
    }

    public String getUsername() {
        return username;
    }

    public void setUsername(String username) {
        this.username = username;
    }

    public String getPasswordHash() {
        return passwordHash;
    }

    public void setPasswordHash(String passwordHash) {
        this.passwordHash = passwordHash;
    }

    public String getEmail() {
        return email;
    }

    public void setEmail(String email) {
        this.email = email;
    }

    public String getPhone() {
        return phone;
    }

    public void setPhone(String phone) {
        this.phone = phone;
    }

    public String getDisplayName() {
        return displayName;
    }

    public void setDisplayName(String displayName) {
        this.displayName = displayName;
    }

    public Role getRole() {
        return role;
    }

    public void setRole(Role role) {
        this.role = role;
    }
}
