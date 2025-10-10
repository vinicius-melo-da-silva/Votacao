using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Dapper;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using SistemaVotacao.Models;
using System;

namespace SistemaVotacao.Controllers
{
    public class CandidatosController : Controller
    {
        private readonly string _connectionString;

        public CandidatosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Candidatos
        public IActionResult Index()
        {
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                var candidatos = db.Query<Candidatos>("ListarCandidatos",
                    new { p_id_candidato = (int?)null },
                    commandType: CommandType.StoredProcedure).ToList();
                return View(candidatos);
            }
        }

        // GET: Candidatos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Candidatos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Candidatos candidato)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (IDbConnection db = new MySqlConnection(_connectionString))
                    {
                        var parameters = new
                        {
                            p_nome = candidato.nome,
                            p_cpf = candidato.cpf,
                            p_titulo_eleitoral = candidato.titulo_eleitoral,
                            p_foto = candidato.foto ?? ""
                        };
                        db.Execute("CriarCandidato", parameters, commandType: CommandType.StoredProcedure);
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Erro ao criar candidato: " + ex.Message);
                }
            }
            return View(candidato);
        }

        // GET: Candidatos/Edit/5
        public IActionResult Edit(int id)
        {
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                var candidato = db.Query<Candidatos>("ListarCandidatos",
                    new { p_id_candidato = id },
                    commandType: CommandType.StoredProcedure).FirstOrDefault();

                if (candidato == null)
                {
                    return NotFound();
                }
                return View(candidato);
            }
        }

        // POST: Candidatos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Candidatos candidato)
        {
            if (id != candidato.id_candidato)
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
                            p_id_candidato = id,
                            p_nome = candidato.nome,
                            p_cpf = candidato.cpf,
                            p_titulo_eleitoral = candidato.titulo_eleitoral,
                            p_foto = candidato.foto ?? ""
                        };
                        db.Execute("EditarCandidato", parameters, commandType: CommandType.StoredProcedure);
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Erro ao editar candidato: " + ex.Message);
                }
            }
            return View(candidato);
        }

        // POST: Candidatos/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                using (IDbConnection db = new MySqlConnection(_connectionString))
                {
                    db.Execute("ExcluirCandidato",
                        new { p_id_candidato = id },
                        commandType: CommandType.StoredProcedure);
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Erro ao excluir candidato: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}