<?php
namespace App\Controller;

use App\Context;
use App\Http\Response;

/**
 * Profiler de developpement expose. ECART volontaire APP04-V012 : le profiler Symfony
 * (WebProfilerBundle) reste joignable et divulgue des informations internes (requetes SQL,
 * variables de session, configuration, routes). En production il doit etre desactive.
 */
final class ProfilerController
{
    public function index(Context $ctx): Response
    {
        $session = $ctx->session->all();
        $routes = ['/clients', '/export', '/admin/users/{id}/role', '/api/clients/{id}', '/files/{id}/close'];
        $config = $ctx->params;
        unset($config['lab_password']); // ne pas afficher le mot de passe de labo en clair

        $html = "<!doctype html><html lang='fr'><head><meta charset='utf-8'>"
            . "<title>Symfony Profiler - ConseilDesk</title></head><body>"
            . "<h1>Symfony Web Profiler (dev)</h1>"
            . "<p><strong>Environnement de test - donnees fictives.</strong></p>"
            . "<h2>Panneau Request</h2><pre>method=" . htmlspecialchars($ctx->request->method)
            . " path=" . htmlspecialchars($ctx->request->path) . "</pre>"
            . "<h2>Panneau Session</h2><pre>" . htmlspecialchars(json_encode($session)) . "</pre>"
            . "<h2>Panneau Config</h2><pre>" . htmlspecialchars(json_encode($config, JSON_PRETTY_PRINT)) . "</pre>"
            . "<h2>Panneau Routing</h2><pre>" . htmlspecialchars(implode("\n", $routes)) . "</pre>"
            . "<h2>Panneau Doctrine (SQL)</h2><pre>SELECT * FROM clients WHERE nom LIKE '%...%'</pre>"
            . "</body></html>";

        return Response::html($html);
    }
}
