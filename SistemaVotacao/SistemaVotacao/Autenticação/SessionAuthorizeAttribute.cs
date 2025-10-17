using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using SistemaVotacao.Autenticação;

namespace Biblioteca.Filters
{
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        // Se true, a página é pública e não exige login
        public bool AllowAnonymous { get; set; } = false;

        // Se preenchido, apenas roles específicas podem acessar
        public string? RoleAnyOf { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Se a action permite acesso anônimo, apenas retorna
            if (AllowAnonymous)
            {
                base.OnActionExecuting(context);
                return;
            }

            var http = context.HttpContext;
            var role = http.Session.GetString(SessionKeys.UserRole);
            var userId = http.Session.GetInt32(SessionKeys.UserId);

            if (userId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            if (!string.IsNullOrWhiteSpace(RoleAnyOf))
            {
                var allowed = RoleAnyOf.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (!allowed.Contains(role))
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
