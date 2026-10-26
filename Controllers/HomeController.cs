using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NOVIX.Models;
using System;
using System.Diagnostics;

namespace NOVIX.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IConfiguration configuration,
            ILogger<HomeController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            if (email == "admin@novix.com" &&
                password == "NovixDemo2026!")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.LoginError = "Correo o contraseña incorrectos.";

            return View();
        }

        [HttpGet]
        public IActionResult Analisis()
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
                string? cadenaConexion =
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
                _logger.LogError(
                    ex,
                    "Error al consultar el análisis financiero.");

                ViewBag.ErrorAnalisis =
                    "No fue posible consultar los movimientos financieros. " +
                    "Revisa la conexión y la tabla de movimientos.";
            }

            ViewBag.Ingresos = totalIngresos;
            ViewBag.Gastos = totalGastos;
            ViewBag.Balance = totalIngresos - totalGastos;

            ViewBag.Meses = meses;
            ViewBag.IngresosMensuales = ingresosMensuales;
            ViewBag.GastosMensuales = gastosMensuales;

            return View("~/Views/Analisis/Index.cshtml");
        }

        // Redirige las direcciones antiguas de Reportes.
        [HttpGet]
        public IActionResult Reportes()
        {
            return RedirectToAction("Index", "Reportes");
        }

        [HttpGet]
        public IActionResult ProbarConexion()
        {
            try
            {
                string? cadenaConexion =
                    _configuration.GetConnectionString("NOVIXConnection");

                if (string.IsNullOrWhiteSpace(cadenaConexion))
                {
                    return Content(
                        "Error: no se encontró NOVIXConnection.");
                }

                using (SqlConnection conexion =
                    new SqlConnection(cadenaConexion))
                {
                    conexion.Open();

                    string consulta = @"
                        SELECT
                            DB_NAME() AS BaseDatos,
                            COUNT(*) AS Cantidad,

                            COALESCE(SUM(CASE
                                WHEN LTRIM(RTRIM(Tipo)) = 'Ingreso'
                                THEN Monto ELSE 0
                            END), 0) AS Ingresos,

                            COALESCE(SUM(CASE
                                WHEN LTRIM(RTRIM(Tipo)) = 'Gasto'
                                THEN Monto ELSE 0
                            END), 0) AS Gastos

                        FROM dbo.MovimientosFinancieros;";

                    using (SqlCommand comando =
                        new SqlCommand(consulta, conexion))
                    using (SqlDataReader lector =
                        comando.ExecuteReader())
                    {
                        if (lector.Read())
                        {
                            return Content(
                                "Base de datos: " + lector["BaseDatos"] +
                                " | Movimientos: " + lector["Cantidad"] +
                                " | Ingresos: Q" + lector["Ingresos"] +
                                " | Gastos: Q" + lector["Gastos"]);
                        }
                    }
                }

                return Content(
                    "La consulta no devolvió resultados.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al comprobar la conexión de NOVIX.");

                return Content(
                    "Error al comprobar la conexión. " +
                    "Consulta la ventana Salida de Visual Studio.");
            }
        }

        // Página de privacidad de NOVIX.
        [HttpGet]
        public IActionResult Privacidad()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}