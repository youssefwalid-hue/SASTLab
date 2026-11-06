<?php
// config/parameters.php - parametres applicatifs (profil standalone, donnees fictives).
// Dans un deploiement `prod` reel, DB_URL est injecte par variable d'environnement (RDS MySQL).
return [
    'app_name'        => 'ConseilDesk',
    'lab_environment' => 'test-data-fictives',
    'lab_password'    => 'Password1!',
    'lab_otp'         => '123456',
    'db_path'         => dirname(__DIR__) . '/var/data.db',
    'templates_dir'   => dirname(__DIR__) . '/templates',
    'export_dir'      => dirname(__DIR__) . '/templates/export/',
    'uploads_dir'     => dirname(__DIR__) . '/var/uploads/',
    'sessions_dir'    => dirname(__DIR__) . '/var/sessions/',
];
