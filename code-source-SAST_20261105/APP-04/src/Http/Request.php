<?php
namespace App\Http;

/**
 * Enveloppe minimale de la requete HTTP (equivalent Symfony\Component\HttpFoundation\Request
 * pour le profil standalone sans vendor). Expose methode, chemin, query, body, cookies, headers.
 */
final class Request
{
    public string $method;
    public string $path;
    public array $query;
    public array $request;   // body POST (form-urlencoded)
    public array $cookies;
    public array $headers;   // cle en minuscules
    public string $rawBody;

    public static function fromGlobals(): self
    {
        $r = new self();
        $r->method = $_SERVER['REQUEST_METHOD'] ?? 'GET';
        $uri = $_SERVER['REQUEST_URI'] ?? '/';
        $r->path = rawurldecode(parse_url($uri, PHP_URL_PATH) ?? '/');
        $r->query = $_GET;
        $r->rawBody = file_get_contents('php://input') ?: '';
        $r->request = $_POST;
        // Supporte aussi un corps JSON pour l'API legere.
        if (empty($r->request) && $r->rawBody !== '' && str_contains(($_SERVER['CONTENT_TYPE'] ?? ''), 'json')) {
            $decoded = json_decode($r->rawBody, true);
            if (is_array($decoded)) {
                $r->request = $decoded;
            }
        }
        $r->cookies = $_COOKIE;
        $headers = [];
        foreach ($_SERVER as $k => $v) {
            if (str_starts_with($k, 'HTTP_')) {
                $name = strtolower(str_replace('_', '-', substr($k, 5)));
                $headers[$name] = $v;
            }
        }
        if (isset($_SERVER['CONTENT_TYPE'])) {
            $headers['content-type'] = $_SERVER['CONTENT_TYPE'];
        }
        $r->headers = $headers;
        return $r;
    }

    public function get(string $key, $default = null)
    {
        return $this->query[$key] ?? $this->request[$key] ?? $default;
    }

    public function header(string $name, $default = null)
    {
        return $this->headers[strtolower($name)] ?? $default;
    }

    public function wantsJson(): bool
    {
        $accept = $this->header('accept', '');
        return str_contains((string) $accept, 'application/json')
            || str_starts_with($this->path, '/api/');
    }

    /** IP de connexion reelle (REMOTE_ADDR), hors en-tetes falsifiables. */
    public function remoteAddr(): string
    {
        return $_SERVER['REMOTE_ADDR'] ?? '127.0.0.1';
    }
}
