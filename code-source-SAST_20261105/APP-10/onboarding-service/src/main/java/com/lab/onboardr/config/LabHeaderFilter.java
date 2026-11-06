package com.lab.onboardr.config;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;

/**
 * Filtre transverse. Ajoute le marqueur de laboratoire et des en-tetes de diagnostic
 * interne. Les en-tetes de diagnostic (X-Service*) sont des en-tetes amont "normaux"
 * cote microservice ; c'est le reverse proxy qui DEVRAIT les masquer en sortie.
 * L'ecart APP10-V011 est porte au niveau du proxy (absence de proxy_hide_header).
 */
@Component
@Order(Ordered.HIGHEST_PRECEDENCE)
public class LabHeaderFilter extends OncePerRequestFilter {

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response,
                                    FilterChain filterChain) throws ServletException, IOException {
        response.setHeader("X-Lab-Environment", "test-data-fictives");

        // En-tetes de diagnostic amont (identification du microservice). En production,
        // le reverse proxy doit les retirer (proxy_hide_header) ; ici il ne le fait pas (V011).
        response.setHeader("X-Service", "kyc-core");
        response.setHeader("X-Service-Version", "2.3.1");
        response.setHeader("X-Upstream", "kyc-internal:8080");

        filterChain.doFilter(request, response);
    }
}
