<?php
namespace App;

use App\Controller\AdminController;
use App\Controller\ApiController;
use App\Controller\ClientController;
use App\Controller\DashboardController;
use App\Controller\ExportController;
use App\Controller\FileController;
use App\Controller\HomeController;
use App\Controller\NoteController;
use App\Controller\PortalController;
use App\Controller\ProfileController;
use App\Controller\ProfilerController;
use App\Controller\SecurityController;
use App\Controller\UploadController;
use App\Http\Request;
use App\Http\Response;
use App\Security\Security;
use App\Session\SessionManager;
use App\Store\Database;
use App\Twig\Template;

/**
 * Noyau HTTP de ConseilDesk. Cable les services, route la requete, applique les
 * en-tetes transverses (middleware X-Lab-Environment, en-tetes de securite par defaut,
 * et les ecarts volontaires de banniere serveur / methodes d'ecriture).
 *
 * $debug = true active le mode developpement (front controller app_dev.php) qui expose
 * des traces detaillees -> ECART volontaire APP04-V013. En production ($debug=false),
 * les erreurs sont generiques (securise par defaut).
 */
final class Kernel
{
    private array $params;
    private bool $debug;
    private array $routes = [];

    public function __construct(bool $debug = false)
    {
        $this->params = require dirname(__DIR__) . '/config/parameters.php';
        $this->debug = $debug;
        $this->registerRoutes();
    }

    private function registerRoutes(): void
    {
        $r = function (string $method, string $pattern, string $handler) {
            $this->routes[] = ['method' => $method, 'pattern' => $pattern, 'handler' => $handler];
        };

        $r('GET', '/', HomeController::class . '::index');
        $r('GET', '/login', SecurityController::class . '::loginForm');
        $r('POST', '/login', SecurityController::class . '::loginLegacy');     // V010 / V011
        $r('POST', '/login/v2', SecurityController::class . '::loginSecure');  // NC N005
        $r('POST', '/logout', SecurityController::class . '::logout');
        $r('GET', '/dashboard', DashboardController::class . '::index');

        $r('GET', '/clients', ClientController::class . '::search');           // V002 / V015
        $r('GET', '/clients/v2', ClientController::class . '::searchSecure');  // NC N002
        $r('GET', '/clients/export', ClientController::class . '::exportCsv'); // use case benin (CSV)
        $r('GET', '/clients/report', ClientController::class . '::report');            // V017 (XSS reflechi)
        $r('GET', '/clients/report-secure', ClientController::class . '::reportSecure'); // NC N007
        $r('GET', '/clients/{id}/notes', NoteController::class . '::list');    // V004
        $r('POST', '/clients/{id}/notes', NoteController::class . '::add');    // V005

        $r('GET', '/export', ExportController::class . '::export');            // V003 / V008
        $r('GET', '/export/v2', ExportController::class . '::exportSecure');   // NC N003

        $r('GET', '/files/{id}', FileController::class . '::show');
        $r('POST', '/files/{id}/close', FileController::class . '::close');    // V007
        $r('POST', '/files/{id}/close-v2', FileController::class . '::closeSecure'); // NC N006
        $r('GET', '/files/{id}/print', FileController::class . '::printView');        // V021 (clickjacking)
        $r('GET', '/files/{id}/print-secure', FileController::class . '::printSecure'); // NC N011

        $r('GET', '/admin/users', AdminController::class . '::users');
        $r('POST', '/admin/users/{id}/role', AdminController::class . '::changeRole'); // V001 / V006
        $r('POST', '/admin/roles/{id}/assign', AdminController::class . '::assignRoleSecure'); // NC N001
        $r('POST', '/admin/reset', AdminController::class . '::reset');

        $r('GET', '/profile', ProfileController::class . '::index');             // use case benin (profil)
        $r('POST', '/profile/update', ProfileController::class . '::update');            // V019 (mass assignment)
        $r('POST', '/profile/update-secure', ProfileController::class . '::updateSecure'); // NC N009
        $r('GET', '/notifications', PortalController::class . '::notifications'); // use case benin
        $r('GET', '/history', PortalController::class . '::history');            // use case benin
        $r('GET', '/help', PortalController::class . '::help');                  // use case benin

        $r('POST', '/password/reset', SecurityController::class . '::passwordReset');          // V022 (enum comptes)
        $r('POST', '/password/reset-secure', SecurityController::class . '::passwordResetSecure'); // NC N012

        $r('POST', '/notes/preview', NoteController::class . '::preview');         // V018 (SSTI)
        $r('POST', '/notes/preview-secure', NoteController::class . '::previewSecure'); // NC N008

        $r('GET', '/api/clients/{id}', ApiController::class . '::client');       // V009
        $r('GET', '/api/v2/clients/{id}', ApiController::class . '::clientSecure'); // NC N004
        $r('GET', '/api/profile', ApiController::class . '::profile');           // V020 (CORS reflete + credentials)
        $r('GET', '/api/profile-secure', ApiController::class . '::profileSecure'); // NC N010

        $r('GET', '/_profiler', ProfilerController::class . '::index');          // V012
        $r('GET', '/_profiler/{rest}', ProfilerController::class . '::index');   // V012

        $r('PUT', '/uploads/{path}', UploadController::class . '::put');         // V014
        $r('GET', '/uploads/{path}', UploadController::class . '::get');
        $r('DELETE', '/uploads/{path}', UploadController::class . '::delete');   // V014
    }

    public function handle(Request $request): Response
    {
        $session = new SessionManager($this->params['sessions_dir']);
        $session->load($request);
        $db = new Database($this->params['db_path']);
        $twig = new Template($this->params['templates_dir']);
        $security = new Security($session, $db);
        $ctx = new Context($request, $session, $db, $twig, $security, $this->params);

        try {
            // V014 : le serveur annonce et accepte les verbes d'ecriture au niveau global.
            if ($request->method === 'OPTIONS') {
                $resp = new Response('', 204);
                $resp->setHeader('Allow', 'GET, POST, PUT, DELETE, OPTIONS');
                return $this->finalize($resp, $request);
            }

            $resp = $this->dispatch($ctx);
        } catch (\Throwable $e) {
            $resp = $this->handleError($e);
        }

        return $this->finalize($resp, $request);
    }

    private function dispatch(Context $ctx): Response
    {
        $req = $ctx->request;
        foreach ($this->routes as $route) {
            if ($route['method'] !== $req->method) {
                continue;
            }
            $regex = $this->compilePattern($route['pattern']);
            if (preg_match($regex, $req->path, $m)) {
                $params = [];
                foreach ($m as $k => $v) {
                    if (!is_int($k)) {
                        $params[$k] = $v;
                    }
                }
                $ctx->routeParams = $params;
                [$class, $method] = explode('::', $route['handler']);
                $controller = new $class();
                return $controller->$method($ctx);
            }
        }
        return Response::html($ctx->render('error.html.twig', [
            'code' => 404, 'message' => 'Ressource introuvable',
        ]), 404);
    }

    private function compilePattern(string $pattern): string
    {
        $regex = preg_replace_callback('/\{(\w+)\}/', function ($m) {
            // {path} capte les slashs (uploads imbriques) ; sinon segment simple.
            if ($m[1] === 'path' || $m[1] === 'rest') {
                return '(?P<' . $m[1] . '>.+)';
            }
            return '(?P<' . $m[1] . '>[^/]+)';
        }, $pattern);
        return '#^' . $regex . '$#';
    }

    private function handleError(\Throwable $e): Response
    {
        if ($this->debug) {
            $trace = "Exception: " . get_class($e) . "\nMessage: " . $e->getMessage()
                . "\nFichier: " . $e->getFile() . ':' . $e->getLine()
                . "\n\nStack trace:\n" . $e->getTraceAsString();
            return Response::text($trace, 500);
        }
        // Production : message generique, aucune fuite technique (securise par defaut).
        return Response::text("Erreur interne du serveur.", 500);
    }

    private function finalize(Response $resp, Request $request): Response
    {
        // Middleware : marqueur d'environnement de laboratoire sur TOUTES les reponses.
        $resp->setHeader('X-Lab-Environment', $request->header('x-lab-environment') ? 'test-data-fictives' : 'test-data-fictives');

        // protection anti-framing (X-Frame-Options absent, CSP sans frame-ancestors).
        $frameable = isset($resp->headers['__frameable__']);
        unset($resp->headers['__frameable__']);

        // En-tetes de securite par defaut (durcissement) - securise PARTOUT sauf ecarts listes.
        $defaults = [
            'X-Content-Type-Options' => 'nosniff',
            'X-Frame-Options' => 'DENY',
            'Referrer-Policy' => 'no-referrer',
            'Content-Security-Policy' => "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'",
            'Strict-Transport-Security' => 'max-age=31536000; includeSubDomains',
        ];
        if ($frameable) {
            unset($defaults['X-Frame-Options']); 
        }
        foreach ($defaults as $k => $v) {
            if (!isset($resp->headers[$k])) {
                $resp->setHeader($k, $v);
            }
        }

        $resp->setHeader('Server', 'Apache/2.4.58');
        $resp->setHeader('X-Powered-By', 'PHP/8.2');

        return $resp;
    }
}
