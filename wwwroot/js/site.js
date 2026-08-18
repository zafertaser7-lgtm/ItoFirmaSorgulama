document.addEventListener("DOMContentLoaded", function () {
    const loading = document.getElementById("loadingOverlay");
    const btnSearch = document.getElementById("btnSearch");
    const btnExcel = document.getElementById("btnExcel");
    const themeToggleBtn = document.getElementById('theme-toggle');

    // --- 1. TABLO VE ARAMA İŞLEMLERİ ---
    function filtreleriCalistir(e) {
        if (e) e.preventDefault(); // Sayfanın anlamsızca yenilenmesini engeller

        const kelime = document.getElementById("unvan").value;
        const ilce = document.getElementById("ilcesi").value;
        const meslek = document.getElementById("meslekGrubu").value;
        const odaSicil = document.getElementById("odaSicil").value;
        const ticaretSicil = document.getElementById("ticaretSicil").value;

        const url = `/Home/FirmaAra?kelime=${encodeURIComponent(kelime)}&ilce=${encodeURIComponent(ilce)}&meslek=${encodeURIComponent(meslek)}&odaSicil=${encodeURIComponent(odaSicil)}&ticaretSicil=${encodeURIComponent(ticaretSicil)}`;

        fetch(url)
            .then(r => r.json())
            .then(data => tabloyuDoldur(data))
            .catch(err => {
                console.error("Hata Detayı:", err);
                alert("Sorgulama sırasında hata oluştu.");
            });
    }

    function tabloyuDoldur(firmalar) {
        const tbody = document.querySelector("#firmaTable tbody");
        const toplamFirma = document.getElementById("toplamFirma");

        tbody.innerHTML = "";

        if (!firmalar || firmalar.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="7" class="empty-state" style="text-align: center; padding: 15px;">
                        Kayıt bulunamadı.
                    </td>
                </tr>`;
            toplamFirma.textContent = "0";
            return;
        }

        let html = "";

        firmalar.forEach(firma => {
            let webLink = (firma.webAdresi && firma.webAdresi.trim() !== '')
                ? `<a href="${firma.webAdresi.startsWith('http') ? firma.webAdresi : 'https://' + firma.webAdresi}" target="_blank" class="text-primary">Siteye Git</a>`
                : '-';

            html += `
            <tr>
                <td>${firma.odaSicilNo}</td>
                <td>${firma.ticaretSicilNo}</td>
                <td>${firma.meslekGrubu}</td>
                <td><strong>${firma.unvan}</strong></td>
                <td>${firma.ilceAdi}</td>
                <td><small>${firma.adres}</small></td>
                <td>${webLink}</td>
            </tr>`;
        });

        tbody.innerHTML = html;
        toplamFirma.textContent = firmalar.length;
    }

    // Arama ve Excel Buton Dinleyicileri
    if (btnSearch) {
        btnSearch.addEventListener("click", filtreleriCalistir);
    }

    if (btnExcel) {
        btnExcel.addEventListener("click", function (e) {
            if (e) e.preventDefault();

            const kelime = document.getElementById("unvan").value;
            const ilce = document.getElementById("ilcesi").value;
            const meslek = document.getElementById("meslekGrubu").value;
            const odaSicil = document.getElementById("odaSicil").value;
            const ticaretSicil = document.getElementById("ticaretSicil").value;

            window.location.href = `/Home/ExcelIndir?kelime=${encodeURIComponent(kelime)}&ilce=${encodeURIComponent(ilce)}&meslek=${encodeURIComponent(meslek)}&odaSicil=${encodeURIComponent(odaSicil)}&ticaretSicil=${encodeURIComponent(ticaretSicil)}`;
        });
    }

    // --- 2. KARANLIK MOD (THEME) İŞLEMLERİ ---

    // Ezan vakitlerini çeken ve temayı kontrol eden fonksiyon
    async function initTheme() {
        const savedTheme = localStorage.getItem('user-theme');

        // 1. Kullanıcı manuel seçim yaptıysa onu uygula
        if (savedTheme) {
            applyTheme(savedTheme);
            return;
        }

        // 2. Manuel seçim yoksa API'ye göre otomatik belirle
        try {
            const response = await fetch('https://api.aladhan.com/v1/timingsByCity?city=Izmir&country=Turkey&method=13');
            const data = await response.json();

            const timings = data.data.timings;
            const fajr = timings.Fajr;      // Sabah ezanı
            const maghrib = timings.Maghrib; // Akşam ezanı

            const now = new Date();
            const currentTimeStr = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;

            // Gece vakti kontrolü
            if (currentTimeStr >= maghrib || currentTimeStr < fajr) {
                applyTheme('dark');
            } else {
                applyTheme('light');
            }
        } catch (error) {
            console.error("Ezan vakitleri alınamadı, saat bazlı varsayılana geçiliyor:", error);
            const hour = new Date().getHours();
            if (hour >= 19 || hour < 6) {
                applyTheme('dark');
            } else {
                applyTheme('light');
            }
        }
    }

    // Temayı ekrana uygulayan yardımcı fonksiyon
    function applyTheme(theme) {

        const icon = document.getElementById('theme-icon');
        const btnText = document.getElementById('theme-toggle');

        if (theme === 'dark') {
            document.body.classList.add('dark-mode');
            if (icon) icon.textContent = '☀️';
            if (btnText && btnText.lastChild) btnText.lastChild.textContent = ' Açık Mod';
        } else {
            document.body.classList.remove('dark-mode');
            if (icon) icon.textContent = '🌙';
            if (btnText && btnText.lastChild) btnText.lastChild.textContent = ' Koyu Mod';

        }
    }

    // Butona tıklandığında çalışacak fonksiyon
    function toggleTheme() {
        const isDark = document.body.classList.contains('dark-mode');
        const newTheme = isDark ? 'light' : 'dark';

        localStorage.setItem('user-theme', newTheme);
        applyTheme(newTheme);
    }

    // --- 3. BAŞLATMA (INITIALIZATION) ---

    // Sayfa yüklendiğinde temayı otomatik hesapla ve uygula
    initTheme();

    // Karanlık mod butonuna tıklama olayını bağla
    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', toggleTheme);
    }
});