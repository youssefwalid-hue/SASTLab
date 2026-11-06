package com.lab.helios.config;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.security.core.userdetails.UserDetailsService;
import org.springframework.security.web.authentication.rememberme.TokenBasedRememberMeServices;

/**
 * Service "remember-me" dont le cookie est durci.
 *
 * // NC: APP01-N006 — le cookie remember-me porte HttpOnly, Secure et SameSite=Strict.
 * C'est le controle sur comparable au cookie de session volontairement non durci (APP01-V009).
 */
public class HardenedRememberMeServices extends TokenBasedRememberMeServices {

    public HardenedRememberMeServices(String key, UserDetailsService userDetailsService) {
        super(key, userDetailsService);
    }

    @Override
    protected void setCookie(String[] tokens, int maxAge, HttpServletRequest request,
                             HttpServletResponse response) {
        String value = encodeCookie(tokens);
        String contextPath = request.getContextPath();
        String path = (contextPath == null || contextPath.isEmpty()) ? "/" : contextPath;

        // NC: APP01-N006 — cookie durci : HttpOnly + Secure + SameSite=Strict
        StringBuilder header = new StringBuilder();
        header.append(getCookieName()).append('=').append(value);
        header.append("; Path=").append(path);
        header.append("; Max-Age=").append(maxAge);
        header.append("; HttpOnly");
        header.append("; Secure");
        header.append("; SameSite=Strict");
        response.addHeader("Set-Cookie", header.toString());
    }
}
