package com.lab.helios.service;

import com.lab.helios.domain.User;
import com.lab.helios.repo.UserRepository;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UserDetailsService;
import org.springframework.security.core.userdetails.UsernameNotFoundException;
import org.springframework.stereotype.Service;

/** Charge les utilisateurs de laboratoire pour Spring Security. */
@Service
public class HeliosUserDetailsService implements UserDetailsService {

    private final UserRepository userRepository;

    public HeliosUserDetailsService(UserRepository userRepository) {
        this.userRepository = userRepository;
    }

    @Override
    public UserDetails loadUserByUsername(String username) throws UsernameNotFoundException {
        User u = userRepository.findByUsername(username)
                .orElseThrow(() -> new UsernameNotFoundException("Utilisateur inconnu : " + username));
        return org.springframework.security.core.userdetails.User.withUsername(u.getUsername())
                .password(u.getPasswordHash())
                .roles(u.getRole().name())
                .build();
    }
}
