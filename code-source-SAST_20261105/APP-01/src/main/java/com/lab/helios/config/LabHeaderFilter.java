package com.lab.helios.config;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;

/**
 * Filtre transverse ajoutant des en-tetes a toutes les reponses :
 *  - X-Lab-Environment : marqueur de laboratoire (donnees fictives) sur toutes les reponses.
 *  - X-Application-Version : ecart volontaire de divulgation de version (APP01-V016).
 */
@Component
@Order(Ordered.HIGHEST_PRECEDENCE)
public class LabHeaderFilter extends OncePerRequestFilter {

    @Value("${helios.app-version}")
    private String appVersion;

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response,
                                    FilterChain filterChain) throws ServletException, IOException {
        // Marqueur de laboratoire (non lie a une vulnerabilite)
        response.setHeader("X-Lab-Environment", "test-data-fictives");

        // La banniere serveur (Server: Apache Tomcat/10.1) est, elle, emise au niveau du connecteur/proxy.
        response.setHeader("X-Application-Version", appVersion);

        filterChain.doFilter(request, response);
    }
}
