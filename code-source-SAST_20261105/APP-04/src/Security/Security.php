<?php
namespace App\Security;

use App\Session\SessionManager;
use App\Store\Database;

/**
 * Service d'authentification / autorisation (equivalent du composant Security de Symfony
 * pour le profil standalone). Fournit l'utilisateur courant, les controles de role et un
 * mecanisme de jeton anti-CSRF. Le comportement par defaut est SECURISE ; les ecarts
 * (absence de controle de role / CSRF) sont localises dans les controleurs concernes.
 */
final class Security
{
    private SessionManager $session;
    private Database $db;

    public function __construct(SessionManager $session, Database $db)
    {
        $this->session = $session;
        $this->db = $db;
    }

    public function isAuthenticated(): bool
    {
        return $this->session->get('user_id') !== null;
    }

    public function user(): ?array
    {
        $id = $this->session->get('user_id');
        if ($id === null) {
            return null;
        }
        // Role relu depuis la base (reflete les changements de role).
        return $this->db->userById((int) $id);
    }

    public function username(): ?string
    {
        $u = $this->user();
        return $u['username'] ?? null;
    }

    public function isGranted(string $role): bool
    {
        $u = $this->user();
        if ($u === null) {
            return false;
        }
        if ($role === 'ROLE_DIRECTEUR') {
            return $u['role'] === 'DIRECTEUR';
        }
        if ($role === 'ROLE_CONSEILLER') {
            return in_array($u['role'], ['CONSEILLER', 'DIRECTEUR'], true);
        }
        return false;
    }

    /** Jeton anti-CSRF lie a la session (double submit / token synchronise). */
    public function csrfToken(string $intention = 'default'): string
    {
        $tokens = $this->session->get('csrf', []);
        if (!isset($tokens[$intention])) {
            $tokens[$intention] = bin2hex(random_bytes(16));
            $this->session->set('csrf', $tokens);
        }
        return $tokens[$intention];
    }

    public function isCsrfTokenValid(string $intention, ?string $token): bool
    {
        $tokens = $this->session->get('csrf', []);
        return isset($tokens[$intention]) && is_string($token)
            && hash_equals($tokens[$intention], $token);
    }
}
