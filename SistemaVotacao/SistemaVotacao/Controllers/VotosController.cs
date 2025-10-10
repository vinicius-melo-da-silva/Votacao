using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Dapper;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using SistemaVotacao.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaVotacao.Controllers
{
    public class VotosController : Controller
    {
        private readonly string _connectionString;

        public VotosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Votos
        public IActionResult Index()
        {
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                var query = @"
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
        }

        // GET: Votos/Create
        public IActionResult Create()
        {
            var viewModel = new VotoCreateViewModel();
            LoadDropdownData(viewModel);
            return View(viewModel);
        }

        // POST: Votos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(VotoCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (IDbConnection db = new MySqlConnection(_connectionString))
                    {
                        // Verificar se o eleitor já votou
                        var votoExistente = db.QueryFirstOrDefault<int>(
                            "SELECT COUNT(*) FROM Votos WHERE id_eleitor = @id_eleitor",
                            new { id_eleitor = viewModel.id_eleitor });

                        if (votoExistente > 0)
                        {
                            ModelState.AddModelError("", "Este eleitor já realizou um voto.");
                            LoadDropdownData(viewModel);
                            return View(viewModel);
                        }

                        // Registrar o voto
                        var parameters = new
                        {
                            id_eleitor = viewModel.id_eleitor,
                            id_candidato = viewModel.id_candidato
                        };

                        db.Execute(
                            "INSERT INTO Votos (id_eleitor, id_candidato) VALUES (@id_eleitor, @id_candidato)",
                            parameters);
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Erro ao registrar voto: " + ex.Message);
                }
            }

            LoadDropdownData(viewModel);
            return View(viewModel);
        }

        // POST: Votos/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                using (IDbConnection db = new MySqlConnection(_connectionString))
                {
                    db.Execute("DELETE FROM Votos WHERE id_voto = @id", new { id });
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Erro ao excluir voto: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        private void LoadDropdownData(VotoCreateViewModel viewModel)
        {
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                viewModel.Eleitores = db.Query<Eleitores>("SELECT id_eleitor, nome FROM Eleitores ORDER BY nome").ToList();
                viewModel.Candidatos = db.Query<Candidatos>("SELECT id_candidato, nome FROM Candidatos ORDER BY nome").ToList();
            }
        }
    }

    // ViewModel para exibição dos votos
    public class VotoViewModel
    {
        public int id_voto { get; set; }
        public int id_eleitor { get; set; }
        public int id_candidato { get; set; }
        public DateTime data_voto { get; set; }
        public DateTime criado_em { get; set; }
        public string nome_eleitor { get; set; }
        public string nome_candidato { get; set; }
    }

    // ViewModel para criação de votos
    public class VotoCreateViewModel
    {
        [Required(ErrorMessage = "Selecione um eleitor")]
        [Display(Name = "Eleitor")]
        public int id_eleitor { get; set; }

        [Required(ErrorMessage = "Selecione um candidato")]
        [Display(Name = "Candidato")]
        public int id_candidato { get; set; }

        public List<Eleitores> Eleitores { get; set; }
        public List<Candidatos> Candidatos { get; set; }
    }
}