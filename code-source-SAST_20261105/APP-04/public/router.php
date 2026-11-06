<?php
// public/router.php - routeur pour le serveur web integre de PHP (execution LOCALE).
//   php -S 0.0.0.0:8080 -t public public/router.php
// Sur Elastic Beanstalk (Apache + php-fpm), c'est public/.htaccess qui reecrit vers index.php
// et app_dev.php est servi directement (ecart volontaire APP04-V013).
$path = parse_url($_SERVER['REQUEST_URI'] ?? '/', PHP_URL_PATH) ?: '/';

// Sert directement les ressources statiques existantes (CSS, JS) sous public/.
if ($path !== '/' && !str_ends_with($path, '.php')) {
    $file = __DIR__ . $path;
    if (is_file($file) && (str_starts_with($path, '/assets/'))) {
        return false; // le serveur integre sert le fichier tel quel
    }
}

// Front controller de developpement (ecart volontaire APP04-V013).
if (preg_match('#^/app_dev\.php#', $path)) {
    require __DIR__ . '/app_dev.php';
    return true;
}

// Tout le reste passe par le front controller de production.
require __DIR__ . '/index.php';
return true;
