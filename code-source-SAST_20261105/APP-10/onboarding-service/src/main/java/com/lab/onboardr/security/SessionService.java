package com.lab.onboardr.security;

import com.lab.onboardr.domain.Role;
import jakarta.servlet.http.Cookie;
import jakarta.servlet.http.HttpServletRequest;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;
import java.nio.charset.StandardCharsets;
import java.util.Base64;
import java.util.Map;

/**
 * Gestion de session de laboratoire par cookie signe (HMAC-SHA256).
 * Le jeton est "base64url(username|ROLE).base64url(hmac)". Le secret est partage
 * avec le service documents (Node) afin que les deux microservices valident la meme
 * session sans depot partage. Comptes et mots de passe 100% fictifs.
 *
 * NOTE SECURITE : ce mecanisme est correct (signature verifiee, comparaison a temps
 * constant). Il ne constitue PAS un des ecarts volontaires.
 */
@Component
public class SessionService {

    public static final String COOKIE = "OBRSESSION";

    private static final Map<String, Role> ACCOUNTS = Map.of(
            "prospect1", Role.PROSPECT,
            "prospect2", Role.PROSPECT,
            "prospect3", Role.PROSPECT,
            "prospect4", Role.PROSPECT,
            "conformite1", Role.CONFORMITE);
    private static final String LAB_PASSWORD = "Password1!";

    private final byte[] secret;

    public SessionService(@Value("${onboardr.session-secret}") String secret) {
        this.secret = secret.getBytes(StandardCharsets.UTF_8);
    }

    public boolean authenticate(String username, String password) {
        return ACCOUNTS.containsKey(username) && LAB_PASSWORD.equals(password);
    }

    public Role roleOf(String username) {
        return ACCOUNTS.get(username);
    }

    /** Un compte de laboratoire porte-t-il ce nom ? (helper benin, non secret). */
    public boolean accountExists(String username) {
        return ACCOUNTS.containsKey(username);
    }

    public String issue(String username, Role role) {
        String payload = username + "|" + role.name();
        String p = b64(payload.getBytes(StandardCharsets.UTF_8));
        String s = b64(hmac(payload));
        return p + "." + s;
    }

    /** Valide le cookie et renvoie l'utilisateur, ou null si absent/invalide. */
    public SessionUser current(HttpServletRequest req) {
        String token = readCookie(req);
        if (token == null) return null;
        int dot = token.indexOf('.');
        if (dot <= 0) return null;
        try {
            String payload = new String(Base64.getUrlDecoder().decode(token.substring(0, dot)),
                    StandardCharsets.UTF_8);
            byte[] expected = hmac(payload);
            byte[] provided = Base64.getUrlDecoder().decode(token.substring(dot + 1));
            if (!constantTimeEquals(expected, provided)) return null;
            String[] parts = payload.split("\\|", 2);
            if (parts.length != 2) return null;
            Role role = Role.valueOf(parts[1]);
            if (!ACCOUNTS.containsKey(parts[0])) return null;
            return new SessionUser(parts[0], role);
        } catch (RuntimeException e) {
            return null;
        }
    }

    private String readCookie(HttpServletRequest req) {
        if (req.getCookies() == null) return null;
        for (Cookie c : req.getCookies()) {
            if (COOKIE.equals(c.getName())) return c.getValue();
        }
        return null;
    }

    private byte[] hmac(String payload) {
        try {
            Mac mac = Mac.getInstance("HmacSHA256");
            mac.init(new SecretKeySpec(secret, "HmacSHA256"));
            return mac.doFinal(payload.getBytes(StandardCharsets.UTF_8));
        } catch (Exception e) {
            throw new IllegalStateException(e);
        }
    }

    private static String b64(byte[] b) {
        return Base64.getUrlEncoder().withoutPadding().encodeToString(b);
    }

    private static boolean constantTimeEquals(byte[] a, byte[] b) {
        if (a.length != b.length) return false;
        int r = 0;
        for (int i = 0; i < a.length; i++) r |= a[i] ^ b[i];
        return r == 0;
    }
}
