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
    public class ResultadosController : Controller
    {
        private readonly string _connectionString;

        public ResultadosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Resultados
        public IActionResult Index()
        {
            var viewModel = new ResultadosViewModel();

            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                // Lista de candidatos
                viewModel.Candidatos = db.Query<Candidatos>(
                    "ListarCandidatos",
                    new { p_id_candidato = (int?)null },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                // Total de votos (buscar uma vez só)
                viewModel.TotalVotos = db.QuerySingle<int>("SELECT COUNT(*) FROM Votos");

                // Total de eleitores
                viewModel.TotalEleitores = db.QuerySingle<int>("SELECT COUNT(*) FROM Eleitores");

                // Resultados detalhados
                viewModel.Resultados = ObterResultadoEleicao(db, viewModel.TotalVotos);
            }

            return View(viewModel);
        }

        private List<ResultadoEleicao> ObterResultadoEleicao(IDbConnection db, int totalGeral)
        {
            var query = @"
                SELECT 
                    c.id_candidato,
                    c.nome,
                    IFNULL(c.foto, '') AS foto,
                    COUNT(v.id_voto) as total_votos
                FROM Candidatos c
                LEFT JOIN Votos v ON c.id_candidato = v.id_candidato
                GROUP BY c.id_candidato, c.nome, c.foto
                ORDER BY total_votos DESC";

            var resultados = db.Query<ResultadoEleicao>(query).ToList();

            foreach (var r in resultados)
            {
                r.total_geral = totalGeral;
                r.percentual = totalGeral > 0 ? (r.total_votos * 100.0 / totalGeral) : 0;
            }

            return resultados;
        }
    }

    public class ResultadosViewModel
    {
        public List<Candidatos> Candidatos { get; set; } = new List<Candidatos>();
        public List<ResultadoEleicao> Resultados { get; set; } = new List<ResultadoEleicao>();
        public int TotalEleitores { get; set; }
        public int TotalVotos { get; set; }

        // Calculado dinamicamente
        public int TotalNaoVotaram => TotalEleitores - TotalVotos;

        public double PercentualParticipacao =>
            TotalEleitores > 0 ? (TotalVotos * 100.0 / TotalEleitores) : 0;
    }

    public class ResultadoEleicao
    {
        public int id_candidato { get; set; }
        public string nome { get; set; }
        public string foto { get; set; }
        public int total_votos { get; set; }
        public int total_geral { get; set; }
        public double percentual { get; set; }

        public string percentual_formatado => percentual.ToString("F2") + "%";
    }
}
