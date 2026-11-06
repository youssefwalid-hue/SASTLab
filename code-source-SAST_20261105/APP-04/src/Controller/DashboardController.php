<?php
namespace App\Controller;

use App\Context;
use App\Http\Response;

final class DashboardController
{
    public function index(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        $user = $ctx->security->user();
        return Response::html($ctx->render('dashboard.html.twig', [
            'user' => $user,
        ]));
    }
}
