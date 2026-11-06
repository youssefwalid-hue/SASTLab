package com.lab.helios.config;

import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.security.core.Authentication;
import org.springframework.security.web.authentication.AuthenticationSuccessHandler;
import org.springframework.util.StringUtils;

import java.io.IOException;

/**
 * Redirection apres connexion reussie en suivant le parametre returnUrl.
 *
 * 
 * sans verifier qu'il s'agit d'un chemin relatif interne ou d'un domaine autorise.
 */
public class OpenRedirectAuthSuccessHandler implements AuthenticationSuccessHandler {

    @Override
    public void onAuthenticationSuccess(HttpServletRequest request, HttpServletResponse response,
                                        Authentication authentication) throws IOException, ServletException {
        String returnUrl = request.getParameter("returnUrl");
        if (StringUtils.hasText(returnUrl)) {
            response.sendRedirect(returnUrl);
            return;
        }
        response.sendRedirect("/");
    }
}
