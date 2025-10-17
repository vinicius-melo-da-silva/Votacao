using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using SistemaVotacao.Models;
using BCrypt.Net;
using Biblioteca.Filters;

namespace SistemaVotacao.Controllers
{
    [SessionAuthorize(RoleAnyOf = "Adm,Gerente")]

    [Route("usuario")]
    public class UsuarioController : Controller
    {
        private readonly string _connectionString;

        public UsuarioController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet("list")]
        public IActionResult List(int? id = null, bool ativosApenas = false)
        {
            var usuarios = new List<Usuario>();

            using (var conexao = new MySqlConnection(_connectionString))
            {
                conexao.Open();
                using (var comando = new MySqlCommand("CALL ListarUsuarios(@p_id, @p_ativos_apenas)", conexao))
                {
                    comando.Parameters.AddWithValue("@p_id", (object)id ?? DBNull.Value);
                    comando.Parameters.AddWithValue("@p_ativos_apenas", ativosApenas);

                    using (var reader = comando.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            usuarios.Add(new Usuario
                            {
                                id = Convert.ToInt32(reader["id"]),
                                nome = reader["nome"].ToString(),
                                titulo_eleitoral = reader["titulo_eleitoral"].ToString(),
                                role = reader["role"].ToString(),
                                ativo = Convert.ToBoolean(reader["ativo"]),
                                criado_em = Convert.ToDateTime(reader["criado_em"])
                            });
                        }
                    }
                }
            }

            return View(usuarios);
        }

        [HttpGet("create")]
        public IActionResult Creat()
        {
            return View();
        }

        [HttpPost("creat")]
        [ValidateAntiForgeryToken]
        public IActionResult Creat(Usuario usuario)
        {
            if (!ModelState.IsValid)
                return View(usuario);

            string senhaHash = BCrypt.Net.BCrypt.HashPassword(usuario.senha_hash);

            using (var conexao = new MySqlConnection(_connectionString))
            {
                conexao.Open();
                using (var comando = new MySqlCommand("CALL CriarUsuario(@p_nome, @p_titulo_eleitoral, @p_senha_hash, @p_role)", conexao))
                {
                    comando.Parameters.AddWithValue("@p_nome", usuario.nome);
                    comando.Parameters.AddWithValue("@p_titulo_eleitoral", usuario.titulo_eleitoral);
                    comando.Parameters.AddWithValue("@p_senha_hash", senhaHash);
                    comando.Parameters.AddWithValue("@p_role", usuario.role);
                    comando.ExecuteNonQuery();
                }
            }

            TempData["Mensagem"] = "Usuário criado com sucesso!";
            return RedirectToAction("List");
        }

        [HttpGet("edit/{id}")]
        public IActionResult Edit(int id)
        {
            Usuario usuario = null;

            using (var conexao = new MySqlConnection(_connectionString))
            {
                conexao.Open();
                using (var comando = new MySqlCommand("CALL ListarUsuarios(@p_id, false)", conexao))
                {
                    comando.Parameters.AddWithValue("@p_id", id);
                    using (var reader = comando.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            usuario = new Usuario
                            {
                                id = Convert.ToInt32(reader["id"]),
                                nome = reader["nome"].ToString(),
                                titulo_eleitoral = reader["titulo_eleitoral"].ToString(),
                                role = reader["role"].ToString(),
                                ativo = Convert.ToBoolean(reader["ativo"]),
                                criado_em = Convert.ToDateTime(reader["criado_em"])
                            };
                        }
                    }
                }
            }

            if (usuario == null)
                return NotFound();

            return View(usuario);
        }

        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Usuario usuario)
        {
            if (!ModelState.IsValid)
                return View(usuario);

            string senhaHash;

            if (!string.IsNullOrWhiteSpace(usuario.senha_hash))
            {
                senhaHash = BCrypt.Net.BCrypt.HashPassword(usuario.senha_hash);
            }
            else
            {
                using (var conexao = new MySqlConnection(_connectionString))
                {
                    conexao.Open();
                    using (var comando = new MySqlCommand("SELECT senha_hash FROM usuarios WHERE id = @p_id", conexao))
                    {
                        comando.Parameters.AddWithValue("@p_id", id);
                        senhaHash = comando.ExecuteScalar()?.ToString();
                    }
                }
            }

            using (var conexao = new MySqlConnection(_connectionString))
            {
                conexao.Open();
                using (var comando = new MySqlCommand("CALL EditarUsuario(@p_id, @p_nome, @p_senha_hash, @p_role, @p_ativo)", conexao))
                {
                    comando.Parameters.AddWithValue("@p_id", id);
                    comando.Parameters.AddWithValue("@p_nome", usuario.nome);
                    comando.Parameters.AddWithValue("@p_senha_hash", senhaHash);
                    comando.Parameters.AddWithValue("@p_role", usuario.role);
                    comando.Parameters.AddWithValue("@p_ativo", usuario.ativo);
                    comando.ExecuteNonQuery();
                }
            }

            TempData["Mensagem"] = "Usuário atualizado com sucesso!";
            return RedirectToAction("List");
        }

        [HttpPost("excluir/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult Excluir(int id)
        {
            using (var conexao = new MySqlConnection(_connectionString))
            {
                conexao.Open();
                using (var comando = new MySqlCommand("CALL ExcluirUsuario(@p_id)", conexao))
                {
                    comando.Parameters.AddWithValue("@p_id", id);
                    comando.ExecuteNonQuery();
                }
            }

            TempData["Mensagem"] = "Usuário excluído com sucesso!";
            return RedirectToAction("List");
        }
    }
}
