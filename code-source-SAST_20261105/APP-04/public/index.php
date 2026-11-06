<?php
// public/index.php - front controller de PRODUCTION (document root EB = public/).
// Mode debug DESACTIVE : erreurs generiques, aucune fuite technique (securise par defaut).
use App\Http\Request;
use App\Kernel;

require dirname(__DIR__) . '/config/bootstrap.php';

$kernel = new Kernel(false);
$response = $kernel->handle(Request::fromGlobals());
$response->send();
