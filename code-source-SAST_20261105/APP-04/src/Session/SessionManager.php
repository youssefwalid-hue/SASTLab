<?php
namespace App\Session;

use App\Http\Request;
use App\Http\Response;

/**
 * Gestionnaire de session base fichier (cookie PHPSESSID).
 *
 * Il remplace le handler natif PHP afin de maitriser FINEMENT, par route :
 *  - la rotation (ou non) de l'identifiant a l'authentification,
 *  - les attributs du cookie (HttpOnly / Secure / SameSite).
 *
 * Comportement SECURISE par defaut (voir login/v2 - NC APP04-N005). Les ecarts
 * volontaires (fixation de session, absence de HttpOnly) sont localises dans
 * SecurityController::loginLegacy et tagges GT APP04-V010 / APP04-V011.
 */
final class SessionManager
{
    private string $dir;
    private ?string $id = null;
    private array $data = [];

    public function __construct(string $dir)
    {
        $this->dir = rtrim($dir, '/');
        if (!is_dir($this->dir)) {
            @mkdir($this->dir, 0770, true);
        }
    }

    public function load(Request $req): void
    {
        $sid = $req->cookies['PHPSESSID'] ?? null;
        if ($sid !== null && $this->isValidId($sid)) {
            $this->id = $sid;
            $this->data = $this->read($sid);
        }
    }

    public function id(): ?string
    {
        return $this->id;
    }

    public function get(string $key, $default = null)
    {
        return $this->data[$key] ?? $default;
    }

    public function set(string $key, $value): void
    {
        $this->data[$key] = $value;
        $this->ensureId();
        $this->write();
    }

    public function all(): array
    {
        return $this->data;
    }

    public function clear(): void
    {
        if ($this->id !== null) {
            @unlink($this->file($this->id));
        }
        $this->data = [];
    }

    /** Genere un nouvel identifiant si aucun n'est actif (sans rotation d'un id existant). */
    public function ensureId(): void
    {
        if ($this->id === null) {
            $this->id = bin2hex(random_bytes(16));
        }
    }

    /** Rotation securisee : nouvel id, ancien fichier invalide (NC APP04-N005). */
    public function regenerate(): void
    {
        $old = $this->id;
        $this->id = bin2hex(random_bytes(16));
        $this->write();
        if ($old !== null && $old !== $this->id) {
            @unlink($this->file($old));
        }
    }

    /**
     * Adopte tel quel l'identifiant fourni par le client, SANS rotation.
     * Utilise uniquement par le login legacy vulnerable (fixation de session).
     */
    public function adoptProvidedId(?string $providedId): void
    {
        if ($providedId !== null && $this->isValidId($providedId)) {
            $this->id = $providedId; 
        } else {
            $this->ensureId();
        }
        $this->write();
    }

    /** En-tete Set-Cookie VULNERABLE : sans HttpOnly. */
    public function cookieHeaderInsecure(): string
    {
        return 'PHPSESSID=' . $this->id . '; Path=/; SameSite=Lax';
    }

    /** En-tete Set-Cookie SECURISE : HttpOnly + Secure + SameSite=Lax (NC APP04-N005). */
    public function cookieHeaderSecure(): string
    {
        return 'PHPSESSID=' . $this->id . '; Path=/; HttpOnly; Secure; SameSite=Lax';
    }

    private function isValidId(string $sid): bool
    {
        return (bool) preg_match('/^[A-Za-z0-9]{6,64}$/', $sid);
    }

    private function file(string $sid): string
    {
        return $this->dir . '/sess_' . preg_replace('/[^A-Za-z0-9]/', '', $sid) . '.json';
    }

    private function read(string $sid): array
    {
        $f = $this->file($sid);
        if (is_file($f)) {
            $raw = file_get_contents($f);
            $d = json_decode($raw ?: '[]', true);
            return is_array($d) ? $d : [];
        }
        return [];
    }

    private function write(): void
    {
        if ($this->id === null) {
            return;
        }
        @file_put_contents($this->file($this->id), json_encode($this->data));
    }
}
