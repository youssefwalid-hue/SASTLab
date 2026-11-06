class ApplicationController < ActionController::Base
  # Securise par defaut : protection CSRF active sur toutes les actions web.
  # (Les ecarts volontaires sont localises : cf. CardsController#toggle pour V010.)
  protect_from_forgery with: :exception

  helper_method :current_client, :logged_in?

  private

  def current_client
    @current_client ||= Client.find_by(id: session[:client_id])
  end

  def logged_in?
    current_client.present?
  end

  def require_login
    return if logged_in?

    redirect_to login_path(redirect: request.fullpath)
  end

  def login_path(redirect: nil)
    redirect ? "/login?redirect=#{CGI.escape(redirect)}" : "/login"
  end
end
