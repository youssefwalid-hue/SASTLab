package com.lab.helios.config;

import org.springframework.boot.web.embedded.tomcat.TomcatServletWebServerFactory;
import org.springframework.boot.web.server.WebServerFactoryCustomizer;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/** Personnalisation du connecteur Tomcat embarque. */
@Configuration
public class TomcatConnectorConfig {

    /**
     * Active la methode HTTP TRACE sur le connecteur.
     *
     * 
     * desactivee ; le serveur renvoie en echo la requete recue.
     */
    @Bean
    public WebServerFactoryCustomizer<TomcatServletWebServerFactory> traceConnectorCustomizer() {
        return factory -> factory.addConnectorCustomizers(connector -> {
            connector.setAllowTrace(true);
        });
    }
}
