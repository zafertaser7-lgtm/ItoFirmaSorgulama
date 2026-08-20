using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ClosedXML.Excel;
using System.Data;

namespace ItoFirmaSorgulama.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // =========================================================
        // ANA SAYFA
        // =========================================================

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        // =========================================================
        // FÝRMA ARAMA
        // =========================================================

        [HttpGet]
        public IActionResult FirmaAra(
            string? kelime,
            string? ilce,
            string? meslek,
            string? odaSicil,
            string? ticaretSicil)
        {
            List<object> firmalar = new List<object>();

            if (string.IsNullOrWhiteSpace(kelime) &&
                string.IsNullOrWhiteSpace(ilce) &&
                string.IsNullOrWhiteSpace(meslek) &&
                string.IsNullOrWhiteSpace(odaSicil) &&
                string.IsNullOrWhiteSpace(ticaretSicil))
            {
                return Json(firmalar);
            }

            try
            {
                using MySqlConnection baglanti =
                    new MySqlConnection(
                        _configuration.GetConnectionString("ItoRehber"));

                baglanti.Open();

                string sql = @"
                    SELECT
                        f.Id,
                        f.OdaSicilNo,
                        f.TicaretSicilNo,
                        CONCAT(m.GrupKodu, ' - ', m.GrupAdi) AS GrupAdi,
                        i.IlceAdi,
                        f.Unvan,
                        f.Adres,
                        f.WebAdresi
                    FROM Firmalar f
                    INNER JOIN Ilceler i
                        ON f.IlceId = i.Id
                    INNER JOIN MeslekGruplari m
                        ON f.MeslekGrubuId = m.Id
                    WHERE 1 = 1";


                if (!string.IsNullOrWhiteSpace(kelime))
                    sql += " AND f.Unvan LIKE @kelime";

                if (!string.IsNullOrWhiteSpace(ilce))
                    sql += " AND i.IlceAdi = @ilce";

                if (!string.IsNullOrWhiteSpace(meslek))
                    sql += " AND m.GrupKodu = @meslek";

                if (!string.IsNullOrWhiteSpace(odaSicil))
                    sql += " AND f.OdaSicilNo LIKE @odaSicil";

                if (!string.IsNullOrWhiteSpace(ticaretSicil))
                    sql += " AND f.TicaretSicilNo LIKE @ticaretSicil";


                sql += " ORDER BY f.Unvan";


                using MySqlCommand komut =
                    new MySqlCommand(sql, baglanti);


                if (!string.IsNullOrWhiteSpace(kelime))
                {
                    komut.Parameters.AddWithValue(
                        "@kelime",
                        "%" + kelime.Trim() + "%");
                }

                if (!string.IsNullOrWhiteSpace(ilce))
                {
                    komut.Parameters.AddWithValue(
                        "@ilce",
                        ilce.Trim());
                }

                if (!string.IsNullOrWhiteSpace(meslek))
                {
                    komut.Parameters.AddWithValue(
                        "@meslek",
                        meslek.Trim());
                }

                if (!string.IsNullOrWhiteSpace(odaSicil))
                {
                    komut.Parameters.AddWithValue(
                        "@odaSicil",
                        "%" + odaSicil.Trim() + "%");
                }

                if (!string.IsNullOrWhiteSpace(ticaretSicil))
                {
                    komut.Parameters.AddWithValue(
                        "@ticaretSicil",
                        "%" + ticaretSicil.Trim() + "%");
                }


                using MySqlDataReader okuyucu =
                    komut.ExecuteReader();


                while (okuyucu.Read())
                {
                    firmalar.Add(new
                    {
                        id = okuyucu["Id"].ToString(),

                        odaSicilNo =
                            okuyucu["OdaSicilNo"].ToString(),

                        ticaretSicilNo =
                            okuyucu["TicaretSicilNo"].ToString(),

                        meslekGrubu =
                            okuyucu["GrupAdi"].ToString(),

                        unvan =
                            okuyucu["Unvan"].ToString(),

                        ilceAdi =
                            okuyucu["IlceAdi"].ToString(),

                        adres =
                            okuyucu["Adres"].ToString(),

                        webAdresi =
                            okuyucu["WebAdresi"].ToString()
                    });
                }


                return Json(firmalar);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = ex.Message
                    });
            }
        }


        // =========================================================
        // FÝRMA DETAY
        // =========================================================

        [HttpGet]
        public IActionResult FirmaDetay(int id)
        {
            try
            {
                using MySqlConnection baglanti =
                    new MySqlConnection(
                        _configuration.GetConnectionString("ItoRehber"));

                baglanti.Open();


                string sql = @"
                    SELECT
                        f.Id,
                        f.OdaSicilNo,
                        f.TicaretSicilNo,
                        f.Unvan,
                        f.Adres,
                        f.WebAdresi,
                        i.IlceAdi,
                        CONCAT(m.GrupKodu, ' - ', m.GrupAdi) AS GrupAdi
                    FROM Firmalar f
                    INNER JOIN Ilceler i
                        ON f.IlceId = i.Id
                    INNER JOIN MeslekGruplari m
                        ON f.MeslekGrubuId = m.Id
                    WHERE f.Id = @id
                    LIMIT 1";


                using MySqlCommand komut =
                    new MySqlCommand(sql, baglanti);

                komut.Parameters.AddWithValue("@id", id);


                using MySqlDataReader okuyucu =
                    komut.ExecuteReader();


                if (okuyucu.Read())
                {
                    var firma = new
                    {
                        Id = okuyucu["Id"].ToString(),

                        OdaSicilNo =
                            okuyucu["OdaSicilNo"].ToString(),

                        TicaretSicilNo =
                            okuyucu["TicaretSicilNo"].ToString(),

                        Unvan =
                            okuyucu["Unvan"].ToString(),

                        Adres =
                            okuyucu["Adres"].ToString(),

                        WebAdresi =
                            okuyucu["WebAdresi"].ToString(),

                        IlceAdi =
                            okuyucu["IlceAdi"].ToString(),

                        GrupAdi =
                            okuyucu["GrupAdi"].ToString()
                    };

                    return View(firma);
                }


                return NotFound(
                    "Bu ID numarasýna ait firma bulunamadý.");
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    "Firma detay hatasý: " + ex.Message);
            }
        }


        // =========================================================
        // EXCEL ÝNDÝR
        // =========================================================

        [HttpGet]
        public IActionResult ExcelIndir(
            string? kelime,
            string? ilce,
            string? meslek,
            string? odaSicil,
            string? ticaretSicil)
        {
            DataTable dt =
                new DataTable("UyeFirmalar");


            dt.Columns.Add("Oda Sicil No");
            dt.Columns.Add("Ticaret Sicil No");
            dt.Columns.Add("Meslek Grubu");
            dt.Columns.Add("Ýlçe");
            dt.Columns.Add("Ünvan");
            dt.Columns.Add("Adres");
            dt.Columns.Add("Web Adresi");


            try
            {
                using MySqlConnection baglanti =
                    new MySqlConnection(
                        _configuration.GetConnectionString("ItoRehber"));

                baglanti.Open();


                string sql = @"
                    SELECT
                        f.OdaSicilNo,
                        f.TicaretSicilNo,
                        CONCAT(m.GrupKodu, ' - ', m.GrupAdi) AS GrupAdi,
                        i.IlceAdi,
                        f.Unvan,
                        f.Adres,
                        f.WebAdresi
                    FROM Firmalar f
                    INNER JOIN Ilceler i
                        ON f.IlceId = i.Id
                    INNER JOIN MeslekGruplari m
                        ON f.MeslekGrubuId = m.Id
                    WHERE 1 = 1";


                if (!string.IsNullOrWhiteSpace(kelime))
                    sql += " AND f.Unvan LIKE @kelime";

                if (!string.IsNullOrWhiteSpace(ilce))
                    sql += " AND i.IlceAdi = @ilce";

                if (!string.IsNullOrWhiteSpace(meslek))
                    sql += " AND m.GrupKodu = @meslek";

                if (!string.IsNullOrWhiteSpace(odaSicil))
                    sql += " AND f.OdaSicilNo LIKE @odaSicil";

                if (!string.IsNullOrWhiteSpace(ticaretSicil))
                    sql += " AND f.TicaretSicilNo LIKE @ticaretSicil";


                sql += " ORDER BY f.Unvan";


                using MySqlCommand komut =
                    new MySqlCommand(sql, baglanti);


                if (!string.IsNullOrWhiteSpace(kelime))
                    komut.Parameters.AddWithValue(
                        "@kelime",
                        "%" + kelime.Trim() + "%");


                if (!string.IsNullOrWhiteSpace(ilce))
                    komut.Parameters.AddWithValue(
                        "@ilce",
                        ilce.Trim());


                if (!string.IsNullOrWhiteSpace(meslek))
                    komut.Parameters.AddWithValue(
                        "@meslek",
                        meslek.Trim());


                if (!string.IsNullOrWhiteSpace(odaSicil))
                    komut.Parameters.AddWithValue(
                        "@odaSicil",
                        "%" + odaSicil.Trim() + "%");


                if (!string.IsNullOrWhiteSpace(ticaretSicil))
                    komut.Parameters.AddWithValue(
                        "@ticaretSicil",
                        "%" + ticaretSicil.Trim() + "%");


                using MySqlDataReader okuyucu =
                    komut.ExecuteReader();


                while (okuyucu.Read())
                {
                    dt.Rows.Add(
                        okuyucu["OdaSicilNo"].ToString(),
                        okuyucu["TicaretSicilNo"].ToString(),
                        okuyucu["GrupAdi"].ToString(),
                        okuyucu["IlceAdi"].ToString(),
                        okuyucu["Unvan"].ToString(),
                        okuyucu["Adres"].ToString(),
                        okuyucu["WebAdresi"].ToString()
                    );
                }
            }
            catch (Exception ex)
            {
                return Content(
                    "Excel aktarým hatasý: " + ex.Message);
            }


            using XLWorkbook wb =
                new XLWorkbook();

            wb.Worksheets.Add(dt);


            var worksheet = wb.Worksheet(1);

            worksheet.Columns().AdjustToContents();


            using MemoryStream stream =
                new MemoryStream();

            wb.SaveAs(stream);


            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ItoFirmaListesi.xlsx"
            );
        }
    }
}