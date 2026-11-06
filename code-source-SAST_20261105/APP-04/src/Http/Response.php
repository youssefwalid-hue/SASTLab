<?php
namespace App\Http;

/**
 * Enveloppe minimale de la reponse HTTP. Les en-tetes de securite par defaut
 * (CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, HSTS) sont
 * appliques globalement par le Kernel ; les ecarts volontaires du corpus sont
 * localises dans leurs controleurs/middleware respectifs.
 */
final class Response
{
    public int $status;
    public string $body;
    public array $headers;

    public function __construct(string $body = '', int $status = 200, array $headers = [])
    {
        $this->body = $body;
        $this->status = $status;
        $this->headers = $headers;
    }

    public function setHeader(string $name, string $value): void
    {
        $this->headers[$name] = $value;
    }

    public function addCookie(string $cookieLine): void
    {
        // Permet plusieurs Set-Cookie.
        $this->headers['__set_cookie__'][] = $cookieLine;
    }

    public static function html(string $body, int $status = 200): self
    {
        return new self($body, $status, ['Content-Type' => 'text/html; charset=UTF-8']);
    }

    public static function json($data, int $status = 200): self
    {
        return new self(json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES),
            $status, ['Content-Type' => 'application/json']);
    }

    public static function text(string $body, int $status = 200): self
    {
        return new self($body, $status, ['Content-Type' => 'text/plain; charset=UTF-8']);
    }

    public static function redirect(string $location, int $status = 302): self
    {
        return new self('', $status, ['Location' => $location]);
    }

    public function send(): void
    {
        http_response_code($this->status);
        foreach ($this->headers as $name => $value) {
            if ($name === '__set_cookie__') {
                foreach ($value as $cookieLine) {
                    header('Set-Cookie: ' . $cookieLine, false);
                }
                continue;
            }
            header($name . ': ' . $value);
        }
        echo $this->body;
    }
}
