<?php
// config/bootstrap.php
// Chargeur PSR-4 autonome (profil standalone, sans vendor).
// Dans un deploiement Symfony complet, c'est vendor/autoload.php (Composer) qui remplit ce role.
spl_autoload_register(function (string $class): void {
    $prefix = 'App\\';
    if (!str_starts_with($class, $prefix)) {
        return;
    }
    $relative = substr($class, strlen($prefix));
    $file = dirname(__DIR__) . '/src/' . str_replace('\\', '/', $relative) . '.php';
    if (is_file($file)) {
        require $file;
    }
});

// Repertoires runtime.
foreach ([__DIR__ . '/../var', __DIR__ . '/../var/sessions', __DIR__ . '/../var/uploads'] as $d) {
    if (!is_dir($d)) {
        @mkdir($d, 0770, true);
    }
}
