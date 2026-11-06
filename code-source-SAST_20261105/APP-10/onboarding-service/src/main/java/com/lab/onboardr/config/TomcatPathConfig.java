package com.lab.onboardr.config;

import org.apache.catalina.connector.Connector;
import org.springframework.boot.web.embedded.tomcat.TomcatServletWebServerFactory;
import org.springframework.boot.web.server.WebServerFactoryCustomizer;
import org.springframework.context.annotation.Configuration;

/**
 * Configuration du connecteur Tomcat.
 *
 * Le backend DECODE et NORMALISE les sequences de chemin encodees (%2e, %2f) avant
 * le routage MVC. C'est ce comportement, combine a un filtrage de chemin effectue sur
 * la forme BRUTE au niveau du reverse proxy, qui produit l'ecart de normalisation
 * APP10-V003 (le proxy et le backend ne s'accordent pas sur la forme canonique).
 * L'ecart lui-meme est porte par la config Nginx (filtrage avant decodage).
 */
@Configuration
public class TomcatPathConfig implements WebServerFactoryCustomizer<TomcatServletWebServerFactory> {

    @Override
    public void customize(TomcatServletWebServerFactory factory) {
        factory.addConnectorCustomizers((Connector connector) -> {
            // Decode les slash encodes (%2F) : le backend normalise vers la forme canonique.
            connector.setEncodedSolidusHandling("decode");
            // Autorise . et .. encodes dans le chemin brut recu du proxy.
            connector.setProperty("relaxedPathChars", "\"<>[\\]^`{|}");
            connector.setProperty("relaxedQueryChars", "\"<>[\\]^`{|}");
        });
    }
}
