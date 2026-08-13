/// <summary>
/// Централизованная таблица переводов статических подписей UI. Ключ — русский текст.
/// Значения — по кодам языка платформы Яндекса. Покрыт весь список языков плагина.
/// Языки, отсутствующие у ключа, падают на en, затем на русский ключ.
/// Примечание: ky/tg/tk/uz — черновой перевод, желательна native-вычитка.
/// Используется LocalizeYG. Сцена: Глобально. Зависимости: нет. SDK: нет.
/// </summary>
using System.Collections.Generic;

public static class UiTranslations
{
    static Dictionary<string,string> M(
        string en,string uk,string be,string kk,string tr,string az,string de,string fr,
        string es,string pt,string it,string ro,string id,string et,string lv,string lt,
        string ky,string tg,string tk,string uz) => new Dictionary<string,string>
    {
        {"en",en},{"uk",uk},{"be",be},{"kk",kk},{"tr",tr},{"az",az},{"de",de},{"fr",fr},
        {"es",es},{"pt",pt},{"it",it},{"ro",ro},{"id",id},{"et",et},{"lv",lv},{"lt",lt},
        {"ky",ky},{"tg",tg},{"tk",tk},{"uz",uz}
    };

    static readonly Dictionary<string, Dictionary<string,string>> Table =
        new Dictionary<string, Dictionary<string,string>>
    {
        { "Подать", M("Serve","Подати","Падаць","Ұсыну","Servis et","Ver","Servieren","Servir",
            "Servir","Servir","Servi","Servește","Sajikan","Serveeri","Pasniegt","Patiekti",
            "Берүү","Додан","Hödürle","Bering") },

        { "Подтвердить", M("Confirm","Підтвердити","Пацвердзіць","Растау","Onayla","Təsdiqlə","Bestätigen","Confirmer",
            "Confirmar","Confirmar","Conferma","Confirmă","Konfirmasi","Kinnita","Apstiprināt","Patvirtinti",
            "Ырастоо","Тасдиқ","Tassykla","Tasdiqlash") },

        { "Смотреть рекламу (+60)", M("Watch ad (+60)","Дивитися рекламу (+60)","Глядзець рэкламу (+60)","Жарнама көру (+60)","Reklam izle (+60)","Reklama bax (+60)","Werbung ansehen (+60)","Voir la pub (+60)",
            "Ver anuncio (+60)","Ver anúncio (+60)","Guarda l'annuncio (+60)","Vezi reclama (+60)","Tonton iklan (+60)","Vaata reklaami (+60)","Skatīties reklāmu (+60)","Žiūrėti reklamą (+60)",
            "Жарнама көрүү (+60)","Реклама тамошо (+60)","Mahabaty gör (+60)","Reklamani ko'rish (+60)") },

        { "Закрыть", M("Close","Закрити","Закрыць","Жабу","Kapat","Bağla","Schließen","Fermer",
            "Cerrar","Fechar","Chiudi","Închide","Tutup","Sulge","Aizvērt","Uždaryti",
            "Жабуу","Пӯшидан","Ýap","Yopish") },

        { "Подсказка", M("Hint","Підказка","Падказка","Кеңес","İpucu","İpucu","Tipp","Indice",
            "Pista","Dica","Suggerimento","Indiciu","Petunjuk","Vihje","Padoms","Užuomina",
            "Кеңеш","Маслиҳат","Maslahat","Maslahat") },

        { "За монеты", M("For coins","За монети","За манеты","Монетаға","Para ile","Sikkə ilə","Für Münzen","Contre des pièces",
            "Por monedas","Por moedas","Con le monete","Pe monede","Dengan koin","Müntide eest","Par monētām","Už monetas",
            "Тыйынга","Барои тангаҳо","Teňňe üçin","Tangalarga") },

        { "За рекламу", M("For an ad","За рекламу","За рэкламу","Жарнамаға","Reklam ile","Reklama görə","Für Werbung","Contre une pub",
            "Por un anuncio","Por um anúncio","Con un annuncio","Pe reclamă","Dengan iklan","Reklaami eest","Par reklāmu","Už reklamą",
            "Жарнамага","Барои реклама","Mahabat üçin","Reklama uchun") },

        { "Продолжить", M("Continue","Продовжити","Працягнуць","Жалғастыру","Devam et","Davam et","Weiter","Continuer",
            "Continuar","Continuar","Continua","Continuă","Lanjutkan","Jätka","Turpināt","Tęsti",
            "Улантуу","Идома","Dowam et","Davom etish") },

        { "Улучшить кофейню", M("Upgrade café","Покращити кав'ярню","Палепшыць кавярню","Кафені жақсарту","Kafeyi geliştir","Kafeni təkmilləşdir","Café verbessern","Améliorer le café",
            "Mejorar la cafetería","Melhorar o café","Migliora il caffè","Îmbunătățește cafeneaua","Tingkatkan kafe","Täienda kohvikut","Uzlabot kafejnīcu","Tobulinti kavinę",
            "Кафени жакшыртуу","Каферо беҳтар кунед","Kafeni gowulandyr","Kafeni yaxshilash") },

        { "Начать заново", M("Start over","Почати заново","Пачаць спачатку","Қайта бастау","Baştan başla","Yenidən başla","Neu starten","Recommencer",
            "Empezar de nuevo","Recomeçar","Ricomincia","Începe din nou","Mulai lagi","Alusta uuesti","Sākt no jauna","Pradėti iš naujo",
            "Кайра баштоо","Аз нав оғоз","Täzeden başla","Qaytadan boshlash") },

        { "Убрать рекламу", M("Remove ads","Прибрати рекламу","Прыбраць рэкламу","Жарнаманы өшіру","Reklamları kaldır","Reklamları sil","Werbung entfernen","Supprimer les pubs",
            "Quitar anuncios","Remover anúncios","Rimuovi annunci","Elimină reclamele","Hapus iklan","Eemalda reklaamid","Noņemt reklāmas","Pašalinti reklamas",
            "Жарнаманы өчүрүү","Рекламаро нест кардан","Mahabaty aýyr","Reklamani olib tashlash") },

        { "Отключить рекламу и поддержать автора?", M("Disable ads and support the author?","Вимкнути рекламу та підтримати автора?","Адключыць рэкламу і падтрымаць аўтара?","Жарнаманы өшіріп, авторды қолдайсыз ба?","Reklamları kapatıp yazarı destekle?","Reklamları söndürüb müəllifi dəstəklə?","Werbung deaktivieren und den Autor unterstützen?","Désactiver les pubs et soutenir l'auteur ?",
            "¿Desactivar anuncios y apoyar al autor?","Desativar anúncios e apoiar o autor?","Disattivare gli annunci e sostenere l'autore?","Dezactivezi reclamele și susții autorul?","Nonaktifkan iklan dan dukung penulis?","Keela reklaamid ja toeta autorit?","Atspējot reklāmas un atbalstīt autoru?","Išjungti reklamas ir paremti autorių?",
            "Жарнаманы өчүрүп, авторду колдойсузбу?","Рекламаро хомӯш карда, муаллифро дастгирӣ кунед?","Mahabaty öçürip, awtory goldaň?","Reklamani o'chirib, muallifni qo'llab-quvvatlaysizmi?") },

        { "Купить монеты", M("Buy coins","Купити монети","Купіць манеты","Монета сатып алу","Para satın al","Sikkə al","Münzen kaufen","Acheter des pièces",
            "Comprar monedas","Comprar moedas","Compra monete","Cumpără monede","Beli koin","Osta münte","Pirkt monētas","Pirkti monetas",
            "Тыйын сатып алуу","Танга харидан","Teňňe satyn al","Tanga sotib olish") },

        { "Меню", M("Menu","Меню","Меню","Мәзір","Menü","Menyu","Menü","Menu",
            "Menú","Menu","Menu","Meniu","Menu","Menüü","Izvēlne","Meniu",
            "Меню","Меню","Menýu","Menyu") },

        { "Таблица лидеров", M("Leaderboard","Таблиця лідерів","Табліца лідараў","Көшбасшылар кестесі","Lider tablosu","Liderlər cədvəli","Bestenliste","Classement",
            "Clasificación","Classificação","Classifica","Clasament","Papan peringkat","Edetabel","Līderu tabula","Lyderių lentelė",
            "Лидерлер тактасы","Ҷадвали пешсафон","Öňdebaryjylar tablisasy","Peshqadamlar jadvali") },

        { "Журнал", M("Journal","Журнал","Часопіс","Журнал","Günlük","Jurnal","Journal","Journal",
            "Diario","Diário","Diario","Jurnal","Jurnal","Päevik","Žurnāls","Žurnalas",
            "Журнал","Журнал","Žurnal","Jurnal") },

        { "Сброс", M("Reset","Скидання","Скід","Ысыру","Sıfırla","Sıfırla","Zurücksetzen","Réinitialiser",
            "Reiniciar","Redefinir","Reimposta","Resetare","Reset","Lähtesta","Atiestatīt","Atstatyti",
            "Кайра коюу","Бознишонӣ","Täzeden düz","Qayta o'rnatish") },

        { "Не хватает монет на ингредиент!", M("Not enough coins for the ingredient!","Не вистачає монет на інгредієнт!","Не хапае манет на інгрэдыент!","Ингредиентке монета жетпейді!","Malzeme için yeterli para yok!","İnqrediyent üçün kifayət qədər pul yoxdur!","Nicht genug Münzen für die Zutat!","Pas assez de pièces pour l'ingrédient !",
            "¡No hay monedas suficientes para el ingrediente!","Moedas insuficientes para o ingrediente!","Monete insufficienti per l'ingrediente!","Nu ai destule monede pentru ingredient!","Koin tidak cukup untuk bahan!","Münte ei jätku koostisosa jaoks!","Nepietiek monētu sastāvdaļai!","Nepakanka monetų ingredientui!",
            "Ингредиентке тыйын жетишсиз!","Танга барои компонент кофӣ нест!","Düzüm üçin teňňe ýeterlik däl!","Ingredient uchun tanga yetarli emas!") },

        { "Нужна подсказка?", M("Need a hint?","Потрібна підказка?","Патрэбна падказка?","Кеңес керек пе?","İpucu ister misin?","İpucu lazımdır?","Brauchst du einen Tipp?","Besoin d'un indice ?",
            "¿Necesitas una pista?","Precisa de uma dica?","Ti serve un suggerimento?","Ai nevoie de un indiciu?","Butuh petunjuk?","Kas vajad vihjet?","Vajadzīgs padoms?","Reikia užuominos?",
            "Кеңеш керекпи?","Маслиҳат лозим аст?","Maslahat gerekmi?","Maslahat kerakmi?") },

        { "Кофейня · улучшения", M("Café · upgrades","Кав'ярня · покращення","Кавярня · паляпшэнні","Кафе · жақсартулар","Kafe · geliştirmeler","Kafe · təkmilləşdirmələr","Café · Verbesserungen","Café · améliorations",
            "Cafetería · mejoras","Café · melhorias","Caffè · miglioramenti","Cafenea · îmbunătățiri","Kafe · peningkatan","Kohvik · täiendused","Kafejnīca · uzlabojumi","Kavinė · patobulinimai",
            "Кафе · жакшыртуулар","Кафе · беҳбудиҳо","Kafe · gowulanmalar","Kafe · yaxshilanishlar") },

        { "Настройки", M("Settings","Налаштування","Налады","Параметрлер","Ayarlar","Ayarlar","Einstellungen","Paramètres",
            "Ajustes","Configurações","Impostazioni","Setări","Pengaturan","Seaded","Iestatījumi","Nustatymai",
            "Жөндөөлөр","Танзимот","Sazlamalar","Sozlamalar") },

        { "Музыка", M("Music","Музика","Музыка","Музыка","Müzik","Musiqi","Musik","Musique",
            "Música","Música","Musica","Muzică","Musik","Muusika","Mūzika","Muzika",
            "Музыка","Мусиқӣ","Saz","Musiqa") },

        { "Звуки", M("Sound","Звуки","Гукі","Дыбыстар","Sesler","Səslər","Sound","Sons",
            "Sonidos","Sons","Suoni","Sunete","Suara","Helid","Skaņas","Garsai",
            "Үндөр","Садоҳо","Sesler","Tovushlar") },

        { "Голоса", M("Voices","Голоси","Галасы","Дауыстар","Sesler","Səslər","Stimmen","Voix",
            "Voces","Vozes","Voci","Voci","Suara","Hääled","Balsis","Balsai",
            "Үндөр","Овозҳо","Sesler","Ovozlar") },

        // ─── Промпты вовлечения (оценка игры / ярлык на рабочий стол) ───────────
        { "Нравится игра? Оцени её — это очень поможет!", M(
            "Enjoying the game? Rate it — it really helps!","Подобається гра? Оціни її — це дуже допоможе!","Падабаецца гульня? Ацані яе — гэта вельмі дапаможа!","Ойын ұнады ма? Бағалаңыз — бұл көп көмектеседі!","Oyunu beğendin mi? Puan ver — çok yardımcı olur!","Oyun xoşuna gəldi? Qiymətləndir — çox kömək edər!","Gefällt dir das Spiel? Bewerte es — das hilft sehr!","Le jeu te plaît ? Note-le — ça aide beaucoup !",
            "¿Te gusta el juego? Puntúalo, ¡ayuda mucho!","Gostando do jogo? Avalie — ajuda muito!","Ti piace il gioco? Valutalo — aiuta molto!","Îți place jocul? Evaluează-l — ajută mult!","Suka gamenya? Beri nilai — sangat membantu!","Kas mäng meeldib? Hinda seda — see aitab palju!","Patīk spēle? Novērtē to — tas ļoti palīdz!","Patinka žaidimas? Įvertink — tai labai padeda!",
            "Оюн жактыбы? Баалап кой — бул абдан жардам берет!","Бозӣ маъқул шуд? Онро баҳо диҳед — ин хеле кӯмак мекунад!","Oýun haladyňmy? Baha ber — bu köp kömek eder!","O'yin yoqdimi? Baholang — bu juda yordam beradi!") },

        { "Оценить", M("Rate","Оцінити","Ацаніць","Бағалау","Puan ver","Qiymətləndir","Bewerten","Noter",
            "Puntuar","Avaliar","Valuta","Evaluează","Beri nilai","Hinda","Novērtēt","Įvertinti",
            "Баалоо","Баҳо додан","Baha ber","Baholash") },

        { "Позже", M("Later","Пізніше","Пазней","Кейінірек","Sonra","Sonra","Später","Plus tard",
            "Más tarde","Mais tarde","Più tardi","Mai târziu","Nanti","Hiljem","Vēlāk","Vėliau",
            "Кийин","Баъдтар","Soňra","Keyinroq") },

        { "Добавить игру на рабочий стол, чтобы вернуться?", M(
            "Add the game to your home screen to come back?","Додати гру на робочий стіл, щоб повернутися?","Дадаць гульню на працоўны стол, каб вярнуцца?","Оралу үшін ойынды жұмыс үстеліне қосасыз ба?","Geri dönmek için oyunu ana ekrana ekle?","Qayıtmaq üçün oyunu ana ekrana əlavə et?","Das Spiel zum Startbildschirm hinzufügen, um zurückzukehren?","Ajouter le jeu à l'écran d'accueil pour revenir ?",
            "¿Añadir el juego a la pantalla de inicio para volver?","Adicionar o jogo à tela inicial para voltar?","Aggiungere il gioco alla schermata home per tornare?","Adaugi jocul pe ecranul principal ca să revii?","Tambahkan game ke layar utama agar bisa kembali?","Kas lisada mäng avakuvale, et tagasi tulla?","Pievienot spēli sākuma ekrānam, lai atgrieztos?","Pridėti žaidimą į pradžios ekraną, kad grįžtum?",
            "Кайра кирүү үчүн оюнду башкы экранга кошосузбу?","Барои баргаштан бозиро ба экрани асосӣ илова кунед?","Yzyna dolanmak üçin oýny baş ekrana goş?","Qaytish uchun o'yinni bosh ekranga qo'shasizmi?") },

        { "Добавить", M("Add","Додати","Дадаць","Қосу","Ekle","Əlavə et","Hinzufügen","Ajouter",
            "Añadir","Adicionar","Aggiungi","Adaugă","Tambah","Lisa","Pievienot","Pridėti",
            "Кошуу","Илова","Goş","Qo'shish") },

        { "Не сейчас", M("Not now","Не зараз","Не зараз","Қазір емес","Şimdi değil","İndi yox","Nicht jetzt","Pas maintenant",
            "Ahora no","Agora não","Non ora","Nu acum","Nanti saja","Mitte praegu","Ne tagad","Ne dabar",
            "Азыр эмес","Ҳозир не","Häzir däl","Hozir emas") },

        { "Журнал гостей · Завсегдатаи", M("Guest journal · Regulars","Журнал гостей · Завсідники","Журнал гасцей · Заўсёднікі","Қонақтар журналы · Тұрақтылар","Konuk günlüğü · Müdavimler","Qonaq jurnalı · Daimi müştərilər","Gästejournal · Stammgäste","Journal des clients · Habitués",
            "Diario de clientes · Habituales","Diário de clientes · Frequentadores","Diario ospiti · Clienti abituali","Jurnalul oaspeților · Clienți fideli","Jurnal tamu · Pelanggan tetap","Külaliste päevik · Püsikliendid","Viesu žurnāls · Pastāvīgie klienti","Svečių žurnalas · Nuolatiniai klientai",
            "Меймандар журналы · Туруктуу кардарлар","Журнали меҳмонон · Мизоҷони доимӣ","Myhman žurnaly · Hemişelik müşderiler","Mehmonlar jurnali · Doimiy mijozlar") },
    };

    /// <summary>Есть ли этот русский текст в таблице.</summary>
    public static bool Has(string ru) => ru != null && Table.ContainsKey(ru);

    /// <summary>Перевод по коду языка. Откат: точный язык → en → русский ключ.</summary>
    public static bool TryGet(string ru, string lang, out string text)
    {
        text = ru;
        if (lang == "ru") return true;
        if (ru == null || !Table.TryGetValue(ru, out var m)) { text = null; return false; }
        if (m.TryGetValue(lang, out text)) return true;
        if (m.TryGetValue("en", out text)) return true;
        text = ru;
        return true;
    }

    public static string Get(string ru, string lang) => TryGet(ru, lang, out string t) ? t : ru;
}
