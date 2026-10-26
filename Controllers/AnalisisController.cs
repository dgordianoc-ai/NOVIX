using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;

namespace NOVIX.Controllers
{
    public class AnalisisController : Controller
    {
        private readonly IConfiguration _configuration;

        public AnalisisController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            decimal totalIngresos = 0m;
            decimal totalGastos = 0m;

            string[] meses =
            {
                "Ene", "Feb", "Mar", "Abr", "May", "Jun",
                "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"
            };

            decimal[] ingresosMensuales = new decimal[12];
            decimal[] gastosMensuales = new decimal[12];

            try
            {
                string cadenaConexion =
                    _configuration.GetConnectionString("NOVIXConnection");

                using (SqlConnection conexion =
                    new SqlConnection(cadenaConexion))
                {
                    conexion.Open();

                    // Consultar ingresos y gastos totales.
                    string consultaTotales = @"
                        SELECT
                            ISNULL(SUM(CASE
                                WHEN LTRIM(RTRIM(Tipo)) = 'Ingreso'
                                THEN Monto ELSE 0
                            END), 0) AS TotalIngresos,

                            ISNULL(SUM(CASE
                                WHEN LTRIM(RTRIM(Tipo)) = 'Gasto'
                                THEN Monto ELSE 0
                            END), 0) AS TotalGastos

                        FROM dbo.MovimientosFinancieros;";

                    using (SqlCommand comando =
                        new SqlCommand(consultaTotales, conexion))
                    using (SqlDataReader lector =
                        comando.ExecuteReader())
                    {
                        if (lector.Read())
                        {
                            totalIngresos =
                                Convert.ToDecimal(lector["TotalIngresos"]);

                            totalGastos =
                                Convert.ToDecimal(lector["TotalGastos"]);
                        }
                    }

                    // Consultar los movimientos de cada mes del año actual.
                    DateTime fechaInicio =
                        new DateTime(DateTime.Today.Year, 1, 1);

                    DateTime fechaFin = fechaInicio.AddYears(1);

                    string consultaMensual = @"
                        SELECT
                            MONTH(Fecha) AS Mes,

                            ISNULL(SUM(CASE
                                WHEN LTRIM(RTRIM(Tipo)) = 'Ingreso'
                                THEN Monto ELSE 0
                            END), 0) AS Ingresos,

                            ISNULL(SUM(CASE
                                WHEN LTRIM(RTRIM(Tipo)) = 'Gasto'
                                THEN Monto ELSE 0
                            END), 0) AS Gastos

                        FROM dbo.MovimientosFinancieros

                        WHERE Fecha >= @FechaInicio
                          AND Fecha < @FechaFin

                        GROUP BY MONTH(Fecha)
                        ORDER BY MONTH(Fecha);";

                    using (SqlCommand comandoMensual =
                        new SqlCommand(consultaMensual, conexion))
                    {
                        comandoMensual.Parameters.AddWithValue(
                            "@FechaInicio", fechaInicio);

                        comandoMensual.Parameters.AddWithValue(
                            "@FechaFin", fechaFin);

                        using (SqlDataReader lector =
                            comandoMensual.ExecuteReader())
                        {
                            while (lector.Read())
                            {
                                int mes =
                                    Convert.ToInt32(lector["Mes"]);

                                if (mes >= 1 && mes <= 12)
                                {
                                    ingresosMensuales[mes - 1] =
                                        Convert.ToDecimal(lector["Ingresos"]);

                                    gastosMensuales[mes - 1] =
                                        Convert.ToDecimal(lector["Gastos"]);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorAnalisis =
                    "Error al consultar los movimientos: " + ex.Message;
            }

            ViewBag.Ingresos = totalIngresos;
            ViewBag.Gastos = totalGastos;
            ViewBag.Balance = totalIngresos - totalGastos;

            ViewBag.Meses = meses;
            ViewBag.IngresosMensuales = ingresosMensuales;
            ViewBag.GastosMensuales = gastosMensuales;

            return View("~/Views/Analisis/Index.cshtml");
        }
    }
}