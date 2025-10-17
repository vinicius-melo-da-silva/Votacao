using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Dapper;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using SistemaVotacao.Models;
using System;
using Biblioteca.Filters;

namespace SistemaVotacao.Controllers
{
    [SessionAuthorize] // 🔒 Garante que o usuário esteja logado
    public class EleitoresController : Controller
    {
        private readonly string _connectionString;

        public EleitoresController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Eleitores
        // Todos os usuários logados podem visualizar (Comum, Gerente, Adm)
        [SessionAuthorize(RoleAnyOf = "Adm,Gerente,Comum")]
        public IActionResult Index()
        {
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                var eleitores = db.Query<Eleitores>("ListarEleitores",
                    new { p_id_eleitor = (int?)null },
                    commandType: CommandType.StoredProcedure).ToList();
                return View(eleitores);
            }
        }

        // GET: Eleitores/Create
        // Apenas Adm pode criar novos eleitores
        [SessionAuthorize(RoleAnyOf = "Adm")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Eleitores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize(RoleAnyOf = "Adm")]
        public IActionResult Create(Eleitores eleitor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (IDbConnection db = new MySqlConnection(_connectionString))
                    {
                        var parameters = new
                        {
                            p_nome = eleitor.nome,
                            p_titulo_eleitoral = eleitor.titulo_eleitoral
                        };
                        db.Execute("CriarEleitor", parameters, commandType: CommandType.StoredProcedure);
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Erro ao criar eleitor: " + ex.Message);
                }
            }
            return View(eleitor);
        }

        // GET: Eleitores/Edit/5
        // Adm e Gerente podem editar
        [SessionAuthorize(RoleAnyOf = "Adm,Gerente")]
        public IActionResult Edit(int id)
        {
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                var eleitor = db.Query<Eleitores>("ListarEleitores",
                    new { p_id_eleitor = id },
                    commandType: CommandType.StoredProcedure).FirstOrDefault();

                if (eleitor == null)
                {
                    return NotFound();
                }
                return View(eleitor);
            }
        }

        // POST: Eleitores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize(RoleAnyOf = "Adm,Gerente")]
        public IActionResult Edit(int id, Eleitores eleitor)
        {
            if (id != eleitor.id_eleitor)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    using (IDbConnection db = new MySqlConnection(_connectionString))
                    {
                        var parameters = new
                        {
                            p_id_eleitor = id,
                            p_nome = eleitor.nome,
                            p_titulo_eleitoral = eleitor.titulo_eleitoral
                        };
                        db.Execute("EditarEleitor", parameters, commandType: CommandType.StoredProcedure);
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Erro ao editar eleitor: " + ex.Message);
                }
            }
            return View(eleitor);
        }

        // POST: Eleitores/Delete/5
        // Apenas Adm pode excluir
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize(RoleAnyOf = "Adm")]
        public IActionResult Delete(int id)
        {
            try
            {
                using (IDbConnection db = new MySqlConnection(_connectionString))
                {
                    db.Execute("ExcluirEleitor",
                        new { p_id_eleitor = id },
                        commandType: CommandType.StoredProcedure);
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Erro ao excluir eleitor: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
