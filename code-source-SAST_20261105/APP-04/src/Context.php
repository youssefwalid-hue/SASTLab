<?php
namespace App;

use App\Http\Request;
use App\Security\Security;
use App\Session\SessionManager;
use App\Store\Database;
use App\Twig\Template;

/** Conteneur de services passe aux controleurs (equivalent du conteneur Symfony, allege). */
final class Context
{
    public Request $request;
    public SessionManager $session;
    public Database $db;
    public Template $twig;
    public Security $security;
    public array $params;
    public array $routeParams = [];

    public function __construct(
        Request $request,
        SessionManager $session,
        Database $db,
        Template $twig,
        Security $security,
        array $params
    ) {
        $this->request = $request;
        $this->session = $session;
        $this->db = $db;
        $this->twig = $twig;
        $this->security = $security;
        $this->params = $params;
    }

    public function render(string $template, array $ctx = []): string
    {
        $ctx['lab'] = [
            'environment' => $this->params['lab_environment'],
            'app_name' => $this->params['app_name'],
        ];
        $ctx['app'] = [
            'user' => $this->security->user(),
            'authenticated' => $this->security->isAuthenticated(),
        ];
        return $this->twig->render($template, $ctx);
    }
}
