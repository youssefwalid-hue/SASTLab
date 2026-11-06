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
import java.net.URI;
import java.net.URLDecoder;
import java.nio.charset.StandardCharsets;

/**
 *
 * Le reverse proxy filtre la zone d'administration sur la forme BRUTE du chemin
 * (/api/admin -> 403). Tomcat, lui, normalise le chemin AVANT de decoder le slash encode
 * (%2f) : une requete /api/..%2fadmin n'est donc pas bloquee au proxy, et son ".." n'est
 * pas replie par le conteneur. Ce filtre reproduit la divergence attendue par V003 : il
 * decode (%2f, %2e), replie les segments .. / . vers la forme canonique, et re-dispatche.
 * /api/..%2fadmin atteint alors /admin (zone d'administration). Les ressources freres
 * (NC APP10-N002 : /admin/reports) restent protegees par le controle de role applicatif.
 */
@Component
@Order(Ordered.HIGHEST_PRECEDENCE)
public class PathNormalizationFilter extends OncePerRequestFilter {

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain chain)
            throws ServletException, IOException {
        String raw = request.getRequestURI();
        String target = raw;
        try {
            String decoded = URLDecoder.decode(raw, StandardCharsets.UTF_8);
            String norm = URI.create(decoded).normalize().getPath();
            if (norm != null && norm.startsWith("/") && !norm.contains("..") && !norm.equals(raw)) {
                target = norm;
            }
        } catch (RuntimeException ignored) {
            // Chemin malforme : on laisse passer tel quel (comportement inchange).
        }
        if (!target.equals(raw)) {
            request.getRequestDispatcher(target).forward(request, response);
            return;
        }
        chain.doFilter(request, response);
    }
}
