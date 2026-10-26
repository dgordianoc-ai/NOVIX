
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace NOVIX.Controllers
{
    public class ReportesController : Controller
    {
        private readonly IConfiguration _configuration;

        public ReportesController(IConfiguration configuration)
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

            List<MovimientoReporte> movimientos =
                new List<MovimientoReporte>();

            try
            {
                string cadenaConexion =
                    _configuration.GetConnectionString("NOVIXConnection");

                if (string.IsNullOrWhiteSpace(cadenaConexion))
                {
                    throw new InvalidOperationException(
                        "No se encontró la conexión NOVIXConnection.");
                }

                using (SqlConnection conexion =
                    new SqlConnection(cadenaConexion))
                {
                    conexion.Open();

                    string consultaTotales = @"
                        SELECT
                            COALESCE(SUM(CASE
                                WHEN LTRIM(RTRIM(Tipo)) = 'Ingreso'
                                THEN Monto ELSE 0
                            END), 0) AS TotalIngresos,

                            COALESCE(SUM(CASE
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

                    DateTime fechaInicio =
                        new DateTime(DateTime.Today.Year, 1, 1);

                    DateTime fechaFin = fechaInicio.AddYears(1);

                    string consultaMensual = @"
                        SELECT
                            MONTH(Fecha) AS Mes,

                            COALESCE(SUM(CASE
                                WHEN LTRIM(RTRIM(Tipo)) = 'Ingreso'
                                THEN Monto ELSE 0
                            END), 0) AS Ingresos,

                            COALESCE(SUM(CASE
                                WHEN LTRIM(RTRIM(Tipo)) = 'Gasto'
                                THEN Monto ELSE 0
                            END), 0) AS Gastos

                        FROM dbo.MovimientosFinancieros
                        WHERE Fecha >= @FechaInicio
                          AND Fecha < @FechaFin
                        GROUP BY MONTH(Fecha)
                        ORDER BY MONTH(Fecha);";

                    using (SqlCommand comando =
                        new SqlCommand(consultaMensual, conexion))
                    {
                        comando.Parameters.AddWithValue(
                            "@FechaInicio", fechaInicio);

                        comando.Parameters.AddWithValue(
                            "@FechaFin", fechaFin);

                        using (SqlDataReader lector =
                            comando.ExecuteReader())
                        {
                            while (lector.Read())
                            {
                                int mes =
                                    Convert.ToInt32(lector["Mes"]);

                                ingresosMensuales[mes - 1] =
                                    Convert.ToDecimal(lector["Ingresos"]);

                                gastosMensuales[mes - 1] =
                                    Convert.ToDecimal(lector["Gastos"]);
                            }
                        }
                    }

                    string consultaMovimientos = @"
                        SELECT Id, Fecha, Descripcion, Tipo, Categoria, Monto
                        FROM dbo.MovimientosFinancieros
                        ORDER BY Fecha DESC, Id DESC;";

                    using (SqlCommand comando =
                        new SqlCommand(consultaMovimientos, conexion))
                    using (SqlDataReader lector =
                        comando.ExecuteReader())
                    {
                        while (lector.Read())
                        {
                            MovimientoReporte movimiento =
                                new MovimientoReporte();

                            movimiento.Id =
                                Convert.ToInt32(lector["Id"]);

                            movimiento.Fecha =
                                Convert.ToDateTime(lector["Fecha"]);

                            movimiento.Descripcion =
                                Convert.ToString(lector["Descripcion"]) ?? "";

                            movimiento.Tipo =
                                Convert.ToString(lector["Tipo"]) ?? "";

                            movimiento.Categoria =
                                Convert.ToString(lector["Categoria"]) ?? "";

                            movimiento.Monto =
                                Convert.ToDecimal(lector["Monto"]);

                            movimientos.Add(movimiento);
                        }
                    }
                }

                ViewBag.Meses = meses;
                ViewBag.IngresosMensuales = ingresosMensuales;
                ViewBag.GastosMensuales = gastosMensuales;
                ViewBag.Movimientos = movimientos;

                ViewBag.Ingresos = totalIngresos;
                ViewBag.Gastos = totalGastos;
                ViewBag.Balance = totalIngresos - totalGastos;
            }
            catch (Exception)
            {
                ViewBag.ErrorReporte =
                    "No fue posible cargar los reportes. Verifica la conexión y la estructura de la base de datos.";

                ViewBag.Meses = meses;
                ViewBag.IngresosMensuales = ingresosMensuales;
                ViewBag.GastosMensuales = gastosMensuales;
                ViewBag.Movimientos = movimientos;

                ViewBag.Ingresos = totalIngresos;
                ViewBag.Gastos = totalGastos;
                ViewBag.Balance = totalIngresos - totalGastos;
            }

            return View("~/Views/Reportes/Index.cshtml");
        }
    }

    public class MovimientoReporte
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Categoria { get; set; } = "";
        public decimal Monto { get; set; }
    }
}