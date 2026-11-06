package com.lab.helios.config;

import com.lab.helios.service.HeliosUserDetailsService;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.authentication.dao.DaoAuthenticationProvider;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.rememberme.TokenBasedRememberMeServices;

/**
 * Configuration Spring Security.
 *
 * Securisee par defaut. Les ecarts volontaires lies a la configuration de securite sont :
 *  - APP01-V009 : cookie de session non durci (configure dans application.yml).
 *  - APP01-V010 : redirection ouverte via le success handler ({@link OpenRedirectAuthSuccessHandler}).
 *  - APP01-V011 : HSTS non emis (desactive ci-dessous).
 *  - APP01-V013 : endpoints Actuator (dont /actuator/env) accessibles sans authentification.
 *  - APP01-V019 : protection anti-clickjacking desactivee (frameOptions disable ci-dessous).
 * Le controle sur comparable APP01-N006 (cookie remember-me durci) est fourni par
 * {@link HardenedRememberMeServices}.
 */
@Configuration
public class SecurityConfig {

    private static final String REMEMBER_ME_KEY = "helios-lab-remember-me-key";

    @Bean
    public PasswordEncoder passwordEncoder() {
        return new BCryptPasswordEncoder();
    }

    @Bean
    public DaoAuthenticationProvider authenticationProvider(HeliosUserDetailsService uds,
                                                            PasswordEncoder encoder) {
        DaoAuthenticationProvider provider = new DaoAuthenticationProvider();
        provider.setUserDetailsService(uds);
        provider.setPasswordEncoder(encoder);
        return provider;
    }

    @Bean
    public TokenBasedRememberMeServices rememberMeServices(HeliosUserDetailsService uds) {
        // NC: APP01-N006 — cookie remember-me durci (Secure/HttpOnly/SameSite)
        HardenedRememberMeServices services = new HardenedRememberMeServices(REMEMBER_ME_KEY, uds);
        services.setParameter("remember-me");
        services.setCookieName("helios-remember-me");
        services.setUseSecureCookie(true);
        return services;
    }

    @Bean
    public SecurityFilterChain filterChain(HttpSecurity http,
                                           TokenBasedRememberMeServices rememberMeServices) throws Exception {

        http
            .authorizeHttpRequests(auth -> auth
                // Ressources publiques
                .requestMatchers("/login", "/css/**", "/js/**", "/images/**", "/favicon.ico").permitAll()
                .requestMatchers("/password/**").permitAll()
                .requestMatchers("/actuator/**").permitAll()
                // Reinitialisation de l'etat du laboratoire
                .requestMatchers("/admin/reset").permitAll()
                // API REST legere (session)
                .requestMatchers("/api/**").hasAnyRole("CLIENT", "ADVISOR", "ADMIN")
                .anyRequest().authenticated()
            )
            .formLogin(form -> form
                .loginPage("/login")
                .loginProcessingUrl("/login")
                .usernameParameter("username")
                .passwordParameter("password")
                .successHandler(new OpenRedirectAuthSuccessHandler())
                .failureUrl("/login?error")
                .permitAll()
            )
            .rememberMe(rm -> rm
                .rememberMeServices(rememberMeServices)
                .key(REMEMBER_ME_KEY)
            )
            .logout(logout -> logout
                .logoutUrl("/logout")
                .logoutSuccessUrl("/login?logout")
                .deleteCookies("helios-remember-me")
                .permitAll()
            )
            // CSRF active par defaut ; ignore uniquement sur les routes exercees par les scanners
            // et les ecarts volontaires qui exigent un POST sans jeton CSRF.
            .csrf(csrf -> csrf.ignoringRequestMatchers(
                    "/login",                    // authentification des scanners
                    "/beneficiaries/**",         
                    "/transfers/**",             
                    "/password/**",              
                    "/profile/**",               
                    "/admin/**",                 // reinitialisation du laboratoire
                    "/api/**"                    // API REST legere
            ))
            .headers(headers -> headers
                .httpStrictTransportSecurity(hsts -> hsts.disable())
                // (X-Frame-Options non emis). Le controle sur comparable est /widgets/balance-safe (APP01-N010).
                .frameOptions(frame -> frame.disable())
            )
            // L'application utilise une session de formulaire classique (JSESSIONID).
            .sessionManagement(sm -> sm.sessionCreationPolicy(SessionCreationPolicy.IF_REQUIRED));

        return http.build();
    }
}
