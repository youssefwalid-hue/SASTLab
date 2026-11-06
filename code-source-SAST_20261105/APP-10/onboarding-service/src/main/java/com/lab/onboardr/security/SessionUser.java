package com.lab.onboardr.security;

import com.lab.onboardr.domain.Role;

/** Identite authentifiee derivee du cookie de session OBRSESSION. */
public record SessionUser(String username, Role role) {
    public boolean isConformite() {
        return role == Role.CONFORMITE;
    }
}
