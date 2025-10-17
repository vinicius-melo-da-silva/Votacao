using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using SistemaVotacao.Models;
using BCrypt.Net;
using SistemaVotacao.Autenticação;

namespace SistemaVotacao.Controllers
{
    [Route("login")]
    public class LoginController : Controller
    {
        private readonly string _connectionString;

        public LoginController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: login
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("entrar")]
        public IActionResult Entrar(string usuario, string senha)
        {
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                // Busca o usuário pelo login (sem verificar a senha ainda)
                var user = db.QueryFirstOrDefault<Usuario>(
                    "SELECT * FROM usuarios WHERE usuario = @Usuario",
                    new { Usuario = usuario }
                );

                if (user == null)
                {
                    ViewBag.Erro = "Usuário não encontrado!";
                    return View("Login");
                }

                // Verifica a senha usando BCrypt
                bool senhaValida = false;
                try
                {
                    senhaValida = BCrypt.Net.BCrypt.Verify(senha, user.senha_hash);
                }
                catch
                {
                    senhaValida = false;
                }

                if (!senhaValida)
                {
                    ViewBag.Erro = "Senha inválida!";
                    return View("Login");
                }

                // Login válido, podemos salvar dados na sessão
                HttpContext.Session.SetInt32(SessionKeys.UserId, user.id);
                HttpContext.Session.SetString(SessionKeys.UserName, user.nome);
                HttpContext.Session.SetString(SessionKeys.UserRole, user.role);

                TempData["Mensagem"] = "Login realizado com sucesso!";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: login/cadastrar
        [HttpGet("cadastrar")]
        public IActionResult Cadastrar()
        {
            return RedirectToAction("Creat", "Usuario");
        }

        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
