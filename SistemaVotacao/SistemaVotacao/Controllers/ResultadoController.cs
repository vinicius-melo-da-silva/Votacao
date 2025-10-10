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
                viewModel.Candidatos = db.Query<Candidatos>("ListarCandidatos",
                    new { p_id_candidato = (int?)null },
                    commandType: CommandType.StoredProcedure).ToList();

                // Resultados da eleição
                viewModel.Resultados = ObterResultadoEleicao(db);

                // Estatísticas gerais
                viewModel.TotalEleitores = db.QuerySingle<int>("SELECT COUNT(*) FROM Eleitores");
                viewModel.TotalVotos = db.QuerySingle<int>("SELECT COUNT(*) FROM Votos");
            }

            return View(viewModel);
        }

        private List<ResultadoEleicao> ObterResultadoEleicao(IDbConnection db)
        {
            var query = @"
                SELECT 
                    c.id_candidato,
                    c.nome,
                    c.foto,
                    COUNT(v.id_voto) as total_votos,
                    (SELECT COUNT(*) FROM Votos) as total_geral,
                    CASE 
                        WHEN (SELECT COUNT(*) FROM Votos) = 0 THEN 0
                        ELSE (COUNT(v.id_voto) * 100.0 / (SELECT COUNT(*) FROM Votos))
                    END as percentual
                FROM Candidatos c
                LEFT JOIN Votos v ON c.id_candidato = v.id_candidato
                GROUP BY c.id_candidato, c.nome, c.foto
                ORDER BY total_votos DESC";

            return db.Query<ResultadoEleicao>(query).ToList();
        }
    }

    public class ResultadosViewModel
    {
        public List<Candidatos> Candidatos { get; set; }
        public List<ResultadoEleicao> Resultados { get; set; }
        public int TotalEleitores { get; set; }
        public int TotalVotos { get; set; }
        public int TotalNaoVotaram
        {
            get { return TotalEleitores - TotalVotos; }
        }
        public double PercentualParticipacao
        {
            get
            {
                return TotalEleitores > 0 ? (TotalVotos * 100.0 / TotalEleitores) : 0;
            }
        }
    }

    public class ResultadoEleicao
    {
        public int id_candidato { get; set; }
        public string nome { get; set; }
        public string foto { get; set; }
        public int total_votos { get; set; }
        public int total_geral { get; set; }
        public double percentual { get; set; }

        // Propriedade para exibir percentual formatado
        public string percentual_formatado
        {
            get { return percentual.ToString("F2") + "%"; }
        }
    }
}