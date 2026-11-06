<?php
// public/app_dev.php - front controller de DEVELOPPEMENT laisse accessible.
// (traces d'exception detaillees) et expose la configuration de developpement.
use App\Http\Request;
use App\Kernel;

require dirname(__DIR__) . '/config/bootstrap.php';

// Retire le prefixe /app_dev.php pour que le routage interne fonctionne.
$uri = $_SERVER['REQUEST_URI'] ?? '/';
$stripped = preg_replace('#^/app_dev\.php#', '', $uri);
$_SERVER['REQUEST_URI'] = ($stripped === '' || $stripped === false) ? '/' : $stripped;

$kernel = new Kernel(true); 
$response = $kernel->handle(Request::fromGlobals());
$response->send();
