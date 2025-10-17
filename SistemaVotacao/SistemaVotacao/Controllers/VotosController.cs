using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Dapper;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using SistemaVotacao.Models;
using System;
using Microsoft.AspNetCore.Http;
using Biblioteca.Filters;
using SistemaVotacao.Autenticação;

namespace SistemaVotacao.Controllers
{
    [SessionAuthorize]
    public class VotosController : Controller
    {
        private readonly string _connectionString;

        public VotosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Votos
        [SessionAuthorize(RoleAnyOf = "Adm,Gerente,Comum")]
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString(SessionKeys.UserRole);
            var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);

            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                string query;

                if (role == "Adm" || role == "Gerente")
                {
                    query = @"
                        SELECT 
                            v.id_voto,
                            v.id_eleitor,
                            v.id_candidato,
                            v.data_voto,
                            v.criado_em,
                            e.nome as nome_eleitor,
                            c.nome as nome_candidato
                        FROM Votos v
                        INNER JOIN Eleitores e ON v.id_eleitor = e.id_eleitor
                        INNER JOIN Candidatos c ON v.id_candidato = c.id_candidato
                        ORDER BY v.data_voto DESC";

                    var votos = db.Query<VotoViewModel>(query).ToList();
                    return View(votos);
                }
                else
                {
                    query = @"
                        SELECT 
                            v.id_voto,
                            v.id_eleitor,
                            v.id_candidato,
                            v.data_voto,
                            v.criado_em,
                            e.nome as nome_eleitor,
                            c.nome as nome_candidato
                        FROM Votos v
                        INNER JOIN Eleitores e ON v.id_eleitor = e.id_eleitor
                        INNER JOIN Candidatos c ON v.id_candidato = c.id_candidato
                        WHERE v.id_eleitor = @id_eleitor
                        ORDER BY v.data_voto DESC";

                    var votos = db.Query<VotoViewModel>(query, new { id_eleitor = userId }).ToList();
                    return View(votos);
                }
            }
        }

        // GET: Votos/Create
        [SessionAuthorize(RoleAnyOf = "Comum")]
        public IActionResult Create()
        {
            var viewModel = new VotoCreateViewModel();
            LoadDropdownData(viewModel);
            return View(viewModel);
        }

        // POST: Votos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize(RoleAnyOf = "Comum")]
        public IActionResult Create(VotoCreateViewModel viewModel)
        {
            var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            viewModel.id_eleitor = userId ?? 0;

            if (ModelState.IsValid)
            {
                using (IDbConnection db = new MySqlConnection(_connectionString))
                {
                    // Verifica se o eleitor já votou
                    var votoExistente = db.QueryFirstOrDefault<int>(
                        "SELECT COUNT(*) FROM Votos WHERE id_eleitor = @id_eleitor",
                        new { id_eleitor = viewModel.id_eleitor });

                    if (votoExistente > 0)
                    {
                        ModelState.AddModelError("", "Você já realizou um voto.");
                        LoadDropdownData(viewModel);
                        return View(viewModel);
                    }

                    // Insere o voto
                    db.Execute(
                        "INSERT INTO Votos (id_eleitor, id_candidato) VALUES (@id_eleitor, @id_candidato)",
                        new { viewModel.id_eleitor, viewModel.id_candidato });
                }

                return RedirectToAction(nameof(Index));
            }

            LoadDropdownData(viewModel);
            return View(viewModel);
        }

        // GET: Votos/Edit/5
        [SessionAuthorize(RoleAnyOf = "Comum,Adm,Gerente")]
        public IActionResult Edit(int id)
        {
            var role = HttpContext.Session.GetString(SessionKeys.UserRole);
            var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);

            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                var voto = db.QueryFirstOrDefault<Votos>("SELECT * FROM Votos WHERE id_voto = @id", new { id });

                if (voto == null) return NotFound();

                // Comum só pode editar seu próprio voto
                if (role == "Comum" && voto.id_eleitor != userId) return Forbid();

                var viewModel = new VotoCreateViewModel
                {
                    id_eleitor = voto.id_eleitor,
                    id_candidato = voto.id_candidato
                };

                LoadDropdownData(viewModel);
                return View(viewModel);
            }
        }

        // POST: Votos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize(RoleAnyOf = "Comum,Adm,Gerente")]
        public IActionResult Edit(int id, VotoCreateViewModel viewModel)
        {
            var role = HttpContext.Session.GetString(SessionKeys.UserRole);
            var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);

            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                var voto = db.QueryFirstOrDefault<Votos>("SELECT * FROM Votos WHERE id_voto = @id", new { id });
                if (voto == null) return NotFound();

                if (role == "Comum" && voto.id_eleitor != userId) return Forbid();

                db.Execute("UPDATE Votos SET id_candidato = @id_candidato WHERE id_voto = @id",
                    new { viewModel.id_candidato, id });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Votos/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize(RoleAnyOf = "Adm")]
        public IActionResult Delete(int id)
        {
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                db.Execute("DELETE FROM Votos WHERE id_voto = @id", new { id });
            }
            return RedirectToAction(nameof(Index));
        }

        private void LoadDropdownData(VotoCreateViewModel viewModel)
        {
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                viewModel.Candidatos = db.Query<Candidatos>("SELECT id_candidato, nome FROM Candidatos ORDER BY nome").ToList();
            }
        }
    }
}
