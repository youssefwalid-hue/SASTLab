<?php
namespace App\Controller;

use App\Context;
use App\Http\Response;

final class HomeController
{
    public function index(Context $ctx): Response
    {
        if ($ctx->security->isAuthenticated()) {
            return Response::redirect('/dashboard');
        }
        return Response::html($ctx->render('login.html.twig', [
            'error' => null,
            'csrf' => $ctx->security->csrfToken('login'),
        ]));
    }
}
