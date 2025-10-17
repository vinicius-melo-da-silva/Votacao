using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
using SistemaVotacao.Autenticação;

namespace Biblioteca.Controllers
{
    public class AuthController : Controller
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Redireciona para o LoginController, Index
            return RedirectToAction("Index", "Login", new { returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string senha, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                TempData["Error"] = "Informe e-mail e senha.";
                return RedirectToAction("Index", "Login");
            }

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand("sp_usuario_obter_por_email", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("p_email", email);

            using var rd = cmd.ExecuteReader();

            if (!rd.Read())
            {
                TempData["Error"] = "Usuário não encontrado.";
                return RedirectToAction("Index", "Login");
            }

            var id = rd.GetInt32("id");
            var nome = rd.GetString("nome");
            var role = rd.GetString("role");
            var ativo = rd.GetBoolean("ativo");
            var senhaHash = rd["senha_hash"] as string ?? "";

            if (!ativo)
            {
                TempData["Error"] = "Usuário inativo.";
                return RedirectToAction("Index", "Login");
            }

            bool ok = false;
            try
            {
                ok = BCrypt.Net.BCrypt.Verify(senha, senhaHash);
            }
            catch
            {
                ok = false;
            }

            if (!ok)
            {
                TempData["Error"] = "Senha inválida.";
                return RedirectToAction("Index", "Login");
            }

            HttpContext.Session.SetInt32(SessionKeys.UserId, id);
            HttpContext.Session.SetString(SessionKeys.UserName, nome);
            HttpContext.Session.SetString(SessionKeys.UserEmail, email);
            HttpContext.Session.SetString(SessionKeys.UserRole, role);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }

        [HttpGet]
        public IActionResult AcessoNegado() => View();
    }
}
