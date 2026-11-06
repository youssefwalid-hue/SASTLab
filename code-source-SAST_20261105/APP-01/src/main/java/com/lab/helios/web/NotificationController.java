package com.lab.helios.web;

import com.lab.helios.service.NotificationService;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;

import java.security.Principal;

/** Notifications de l'utilisateur (route benigne, rendu echappe). */
@Controller
public class NotificationController {

    private final NotificationService notificationService;

    public NotificationController(NotificationService notificationService) {
        this.notificationService = notificationService;
    }

    @GetMapping("/notifications")
    public String list(Principal principal, Model model) {
        model.addAttribute("notifications", notificationService.listOf(principal.getName()));
        model.addAttribute("username", principal.getName());
        model.addAttribute("activeNav", "notifications");
        return "notifications";
    }
}
