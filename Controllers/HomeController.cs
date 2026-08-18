using Microsoft.AspNetCore.Mvc; // 
using MySql.Data.MySqlClient; // 
using ClosedXML.Excel; // 
using System.Data; // 
using System.Collections.Generic; //
using System.IO; // 
using System; //  

namespace ItoFirmaSorgulama.Controllers // Controller dosyalarýnýn bulunduðu alan.
{
    public class HomeController : Controller // Ana sayfayý yöneten controller sýnýfý.
    {
        // Veritabanýna baðlanmak için gerekli bilgiler.
        private readonly string baglantiCumlesi = "Server=localhost;Database=ItoRehber;Uid=root;Pwd=123456789;Charset=utf8mb4;";

        public IActionResult Index() // Ana sayfa açýldýðýnda çalýþan metot.
        {
            return View(); // Index sayfasýný kullanýcýya gösterir.
        }

        [HttpGet] // Bu metot GET isteði ile çalýþýr.
        public IActionResult FirmaAra(string kelime, string ilce, string meslek, string odaSicil, string ticaretSicil)
        {
            List<object> firmalar = new List<object>(); // Bulunan firmalarý tutacak liste.

            // Hiçbir arama kriteri girilmediyse boþ liste döndürür.
            if (string.IsNullOrEmpty(kelime) && string.IsNullOrEmpty(ilce) && string.IsNullOrEmpty(meslek) && string.IsNullOrEmpty(odaSicil) && string.IsNullOrEmpty(ticaretSicil))
            {
                return Json(firmalar);
            }

            try // Hata oluþursa programýn durmamasý için kullanýlýr.
            {
                using (MySqlConnection baglanti = new MySqlConnection(baglantiCumlesi)) // Veritabaný baðlantýsý oluþturulur.
                {
                    baglanti.Open(); // Veritabaný baðlantýsý açýlýr.

                    // Firmalar tablosundan gerekli bilgiler çekilir.
                    string sql = @"
                    SELECT f.OdaSicilNo, f.TicaretSicilNo,
                    CONCAT(m.GrupKodu, ' - ', m.GrupAdi) AS GrupAdi,
                        i.IlceAdi, f.Unvan, f.Adres, f.WebAdresi      FROM Firmalar f
                        INNER JOIN Ilceler i ON f.IlceId = i.Id
                        INNER JOIN MeslekGruplari m ON f.MeslekGrubuId = m.Id
                        WHERE 1=1";

                    // Kullanýcý hangi alaný doldurduysa sorguya eklenir.
                    if (!string.IsNullOrEmpty(kelime)) sql += " AND f.Unvan LIKE @kelime";
                    if (!string.IsNullOrEmpty(ilce)) sql += " AND i.IlceAdi = @ilce";
                    if (!string.IsNullOrEmpty(meslek)) sql += " AND m.GrupKodu = @meslek";
                    if (!string.IsNullOrEmpty(odaSicil)) sql += " AND f.OdaSicilNo LIKE @odaSicil";
                    if (!string.IsNullOrEmpty(ticaretSicil)) sql += " AND f.TicaretSicilNo LIKE @ticaretSicil";


                    using (MySqlCommand komut = new MySqlCommand(sql, baglanti)) // SQL komutu oluþturulur.
                    {
                        // Girilen deðerler güvenli þekilde sorguya eklenir.
                        if (!string.IsNullOrEmpty(kelime)) komut.Parameters.AddWithValue("@kelime", "%" + kelime + "%");
                        if (!string.IsNullOrEmpty(ilce)) komut.Parameters.AddWithValue("@ilce", ilce);
                        if (!string.IsNullOrEmpty(meslek)) komut.Parameters.AddWithValue("@meslek", meslek);
                        if (!string.IsNullOrEmpty(odaSicil)) komut.Parameters.AddWithValue("@odaSicil", "%" + odaSicil + "%");
                        if (!string.IsNullOrEmpty(ticaretSicil)) komut.Parameters.AddWithValue("@ticaretSicil", "%" + ticaretSicil + "%");

                        using (MySqlDataReader okuyucu = komut.ExecuteReader()) // Sorgu çalýþtýrýlýr.
                        {
                            while (okuyucu.Read()) // Gelen tüm kayýtlar tek tek okunur.
                            {
                                firmalar.Add(new // Okunan bilgiler listeye eklenir.
                                {
                                    odaSicilNo = okuyucu["OdaSicilNo"].ToString(),
                                    ticaretSicilNo = okuyucu["TicaretSicilNo"].ToString(),
                                    meslekGrubu = okuyucu["GrupAdi"].ToString(),
                                    unvan = okuyucu["Unvan"].ToString(),
                                    ilceAdi = okuyucu["IlceAdi"].ToString(),
                                    adres = okuyucu["Adres"].ToString(),
                                    webAdresi = okuyucu["WebAdresi"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) // Hata oluþursa çalýþýr.
            {
                Console.WriteLine("Veritabaný Hatasý: " + ex.Message); // Hata mesajýný konsola yazdýrýr.
            }

            return Json(firmalar); // Bulunan firmalarý JSON olarak gönderir.
        }

        [HttpGet] // Excel indirme isteði GET ile çalýþýr.
        public IActionResult ExcelIndir(string kelime, string ilce, string meslek, string odaSicil, string ticaretSicil)
        {
            DataTable dt = new DataTable("UyeFirmalar"); // Excel için tablo oluþturulur.

            // Excel sütunlarý oluþturulur.
            dt.Columns.Add("Oda Sicil No");
            dt.Columns.Add("Ticaret Sicil No");
            dt.Columns.Add("Meslek Grubu");
            dt.Columns.Add("Ýlçe");
            dt.Columns.Add("Ünvaný");

            try
            {
                using (MySqlConnection baglanti = new MySqlConnection(baglantiCumlesi)) // Veritabanýna baðlanýlýr.
                {
                    baglanti.Open();

                    // Excel'e aktarýlacak veriler seçilir.
                    string sql = @"
                        SELECT f.OdaSicilNo, f.TicaretSicilNo, m.GrupAdi, i.IlceAdi, f.Unvan 
                        FROM Firmalar f
                        INNER JOIN Ilceler i ON f.IlceId = i.Id
                        INNER JOIN MeslekGruplari m ON f.MeslekGrubuId = m.Id
                        WHERE 1=1";

                    // Arama kriterleri varsa sorguya eklenir.
                    if (!string.IsNullOrEmpty(kelime)) sql += " AND f.Unvan LIKE @kelime";
                    if (!string.IsNullOrEmpty(ilce)) sql += " AND i.IlceAdi = @ilce";
                    if (!string.IsNullOrEmpty(meslek)) sql += " AND m.GrupKodu = @meslek";
                    if (!string.IsNullOrEmpty(odaSicil)) sql += " AND f.OdaSicilNo LIKE @odaSicil";
                    if (!string.IsNullOrEmpty(ticaretSicil)) sql += " AND f.TicaretSicilNo LIKE @ticaretSicil";

                    using (MySqlCommand komut = new MySqlCommand(sql, baglanti)) // SQL komutu oluþturulur.
                    {
                        // Parametreler güvenli þekilde eklenir.
                        if (!string.IsNullOrEmpty(kelime)) komut.Parameters.AddWithValue("@kelime", "%" + kelime + "%");
                        if (!string.IsNullOrEmpty(ilce)) komut.Parameters.AddWithValue("@ilce", ilce);
                        if (!string.IsNullOrEmpty(meslek)) komut.Parameters.AddWithValue("@meslek", meslek);
                        if (!string.IsNullOrEmpty(odaSicil)) komut.Parameters.AddWithValue("@odaSicil", "%" + odaSicil + "%");
                        if (!string.IsNullOrEmpty(ticaretSicil)) komut.Parameters.AddWithValue("@ticaretSicil", "%" + ticaretSicil + "%");

                        using (MySqlDataReader okuyucu = komut.ExecuteReader()) // Veriler okunur.
                        {
                            while (okuyucu.Read()) // Her kayýt tabloya eklenir.
                            {
                                dt.Rows.Add(
                                    okuyucu["OdaSicilNo"].ToString(),
                                    okuyucu["TicaretSicilNo"].ToString(),
                                    okuyucu["GrupAdi"].ToString(),
                                    okuyucu["IlceAdi"].ToString(),
                                    okuyucu["Unvan"].ToString()
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Hata olsa bile uygulamanýn kapanmasýný engeller.
            }

            using (XLWorkbook wb = new XLWorkbook()) // Yeni Excel dosyasý oluþturulur.
            {
                wb.Worksheets.Add(dt); // Tablo Excel'e eklenir.
                wb.Worksheet(1).Columns().AdjustToContents(); // Sütun geniþlikleri otomatik ayarlanýr.

                using (MemoryStream stream = new MemoryStream()) // Excel bellekte oluþturulur.
                {
                    wb.SaveAs(stream); // Excel dosyasý belleðe kaydedilir.

                    // Oluþturulan Excel dosyasý kullanýcýya indirilir.
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheet");
                }
            }
        }
    }
}