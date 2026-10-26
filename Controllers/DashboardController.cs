
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;

namespace NOVIX.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IConfiguration _configuration;

        public DashboardController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            try
            {
                using (SqlConnection conexion = new SqlConnection(
                    _configuration.GetConnectionString("NOVIXConnection")))
                {
                    conexion.Open();

                    string consulta = @"
                        SELECT
                            COALESCE(SUM(CASE
                                WHEN Tipo = 'Ingreso' THEN Monto
                                ELSE 0
                            END), 0) AS TotalIngresos,

                            COALESCE(SUM(CASE
                                WHEN Tipo = 'Gasto' THEN Monto
                                ELSE 0
                            END), 0) AS TotalGastos,

                            COUNT(*) AS TotalMovimientos

                        FROM MovimientosFinancieros";

                    using (SqlCommand comando =
                        new SqlCommand(consulta, conexion))
                    using (SqlDataReader lector = comando.ExecuteReader())
                    {
                        if (lector.Read())
                        {
                            decimal totalIngresos =
                                Convert.ToDecimal(lector["TotalIngresos"]);

                            decimal totalGastos =
                                Convert.ToDecimal(lector["TotalGastos"]);

                            int totalMovimientos =
                                Convert.ToInt32(lector["TotalMovimientos"]);

                            ViewBag.TotalIngresos = totalIngresos;
                            ViewBag.TotalGastos = totalGastos;
                            ViewBag.Balance = totalIngresos - totalGastos;
                            ViewBag.TotalMovimientos = totalMovimientos;
                        }
                    }
                }
            }
            catch (Exception)
            {
                ViewBag.ErrorDashboard =
                    "No fue posible cargar los datos financieros.";
                ViewBag.TotalIngresos = 0m;
                ViewBag.TotalGastos = 0m;
                ViewBag.Balance = 0m;
                ViewBag.TotalMovimientos = 0;
            }

            return View();
        }
    }
}
