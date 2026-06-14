/// <summary>
/// Все игровые перечисления, структуры данных и вспомогательные типы.
/// Сцена: Глобально (используется во всех скриптах)
/// Зависимости: Нет
/// SDK: Нет
/// </summary>
using UnityEngine;
using System;

// ─── Фазы игры ───────────────────────────────────────────────────────────────

public enum GamePhase
{
    Tutorial,       // Обучение
    Day,            // Рабочий день
    StoryVignette,  // Вставная сцена между днями
    DayResult,      // Экран результатов
    GameComplete    // Финал (день 20)
}

public enum DayPhase
{
    Intro,              // Начало дня (заставка)
    CustomerArriving,   // Гость входит
    Greeting,           // Приветственный диалог
    CoffeeOrder,        // Гость делает заказ
    CoffeeMaking,       // Игрок готовит кофе
    Serving,            // Подача
    CustomerReaction,   // Реакция гостя
    StoryReveal,        // Раскрытие сюжета (при удовлетворённом госте)
    CustomerLeaving,    // Гость уходит
    DayEnd              // Все гости обслужены
}

// ─── Типы напитков ────────────────────────────────────────────────────────────

public enum CoffeeType
{
    Espresso,       // Эспрессо
    Americano,      // Американо
    Cappuccino,     // Капучино
    Latte,          // Латте
    Mocha,          // Мокко
    HerbalTea,      // Травяной чай
    GreenTea,       // Зелёный чай
    Water,          // Вода
    HotChocolate,   // Горячий шоколад
    BlackCoffee,    // Чёрный кофе
    TruthBrew       // «Кофе Правды» (сюжетный)
}

public enum Volume
{
    Small,  // Маленький
    Medium, // Средний
    Large   // Большой
}

public enum SweetnessLevel
{
    None,   // Без сахара
    Low,    // Слабо сладкий
    Medium, // Средне
    High    // Очень сладкий
}

public enum Topping
{
    None,       // Без топпинга
    Cream,      // Сливки
    Cinnamon,   // Корица
    Caramel,    // Карамель
    Chocolate,  // Шоколад
    Mint        // Мята
}

// ─── Типы персонажей ──────────────────────────────────────────────────────────

public enum CharacterType
{
    Traveler,       // Странник (день 1)
    WaterGuard,     // Водяной страж (день 2)
    ShadowMerchant, // Теневой торговец (дни 3-4)
    FireAlchemist,  // Огненный алхимик (день 5)
    BookKeeper,     // Хранитель книг (дни 6, 12)
    MirrorThief,    // Зеркальный вор (дни 7)
    TimeCourier,    // Временной курьер (день 9)
    StarShepherd,   // Звёздный пастух (день 10)
    OrderStranger,  // Незнакомка Ордена (день 11)
    FogHunter,      // Туманный охотник (дни 12, 17)
    CrystalSinger,  // Кристаллическая певица (дни 15, 17)
    SteamEngineer,  // Паровой инженер (дни 13, 17)
    MoonSmith,      // Лунный кузнец (дни 14, 17)
    Andrei,         // Андрей — муж Анны (день 19-20)
    Lira,           // Лира — злодей (дни 11, 18)

    // ─── Новые персонажи расширенной истории (дни 18–37) ───────────────────
    Cartographer,   // Картограф — карты «тонких мест»
    Herbalist,      // Травница — зелья снов
    ClockKeeper,    // Часовщик — время и затмение
    GraveWarden,    // Смотритель погоста — мёртвые помнят
    Beekeeper,      // Пасечник — мёд правды
    EchoTwin,       // Эхо-близнец — зеркальный мир
    Bard,           // Бард — куплеты «Песни Якоря»
    Cartomancer,    // Гадалка — карты судьбы
    Lamplighter,    // Фонарщик — тайна Чёрного Фонаря
    Smuggler,       // Контрабандист — поставки Ордена
    Defector,       // Перебежчик Ордена — взгляд изнутри
    Widow,          // Вдова — потеряла близкого из-за Ордена
    Glassblower,    // Стеклодув — резонансные сосуды
    Grandmother     // Бабушка — откуда колыбельная
}

// ─── Заказ кофе ──────────────────────────────────────────────────────────────

[Serializable]
public class CoffeeOrder
{
    public CoffeeType type       = CoffeeType.Espresso;
    public Volume volume         = Volume.Medium;
    public SweetnessLevel sweet  = SweetnessLevel.None;
    public Topping topping       = Topping.None;

    /// <summary>Проверяет, совпадает ли приготовленный кофе с заказом.</summary>
    public bool Matches(CoffeeOrder other)
    {
        if (other == null) return false;
        return type == other.type &&
               volume == other.volume &&
               sweet == other.sweet &&
               topping == other.topping;
    }

    /// <summary>Отображаемое название для UI.</summary>
    public string GetDisplayName()
    {
        string name = GetTypeName();
        string vol  = GetVolumeName();
        string sw   = sweet != SweetnessLevel.None ? $", {GetSweetnessName()}" : "";
        string top  = topping != Topping.None ? $" + {GetToppingName()}" : "";
        return $"{vol} {name}{sw}{top}";
    }

    public string GetTypeName() => type switch
    {
        CoffeeType.Espresso     => Loc.T("Эспрессо", "Espresso"),
        CoffeeType.Americano    => Loc.T("Американо", "Americano"),
        CoffeeType.Cappuccino   => Loc.T("Капучино", "Cappuccino"),
        CoffeeType.Latte        => Loc.T("Латте", "Latte"),
        CoffeeType.Mocha        => Loc.T("Мокко", "Mocha"),
        CoffeeType.HerbalTea    => Loc.T("Травяной чай", "Herbal tea"),
        CoffeeType.GreenTea     => Loc.T("Зелёный чай", "Green tea"),
        CoffeeType.Water        => Loc.T("Вода", "Water"),
        CoffeeType.HotChocolate => Loc.T("Горячий шоколад", "Hot chocolate"),
        CoffeeType.BlackCoffee  => Loc.T("Чёрный кофе", "Black coffee"),
        CoffeeType.TruthBrew    => Loc.T("Кофе Правды", "Truth Brew"),
        _                       => Loc.T("Напиток", "Drink")
    };

    public string GetVolumeName() => volume switch
    {
        Volume.Small  => Loc.T("Маленький", "Small"),
        Volume.Medium => Loc.T("Средний", "Medium"),
        Volume.Large  => Loc.T("Большой", "Large"),
        _             => ""
    };

    public string GetSweetnessName() => sweet switch
    {
        SweetnessLevel.Low    => Loc.T("слабо сладкий", "lightly sweet"),
        SweetnessLevel.Medium => Loc.T("средне сладкий", "medium sweet"),
        SweetnessLevel.High   => Loc.T("очень сладкий", "very sweet"),
        _                     => ""
    };

    public string GetToppingName() => topping switch
    {
        Topping.Cream     => Loc.T("Сливки", "Cream"),
        Topping.Cinnamon  => Loc.T("Корица", "Cinnamon"),
        Topping.Caramel   => Loc.T("Карамель", "Caramel"),
        Topping.Chocolate => Loc.T("Шоколад", "Chocolate"),
        Topping.Mint      => Loc.T("Мята", "Mint"),
        _                 => ""
    };
}

// ─── Данные гостя ─────────────────────────────────────────────────────────────

[Serializable]
public class CustomerData
{
    public CharacterType characterType;
    public CoffeeOrder order;
    public int stickmanPrefabIndex;    // Какой префаб stickman использовать (0-8)
    public Color characterColor = Color.white;
}

// ─── Данные сохранения ────────────────────────────────────────────────────────

[Serializable]
public class GameSaveData
{
    public int currentDay    = 0; // 0 = обучение ещё не пройдено
    public bool tutorialDone = false;
    public int totalCoins    = 100; // стартовый капитал кофейни (хватает на первые ингредиенты)

    // Память удовлетворённости по уникальным клиентам (пункт 4.3):
    // параллельные списки (JsonUtility не умеет Dictionary).
    public System.Collections.Generic.List<int>   clientKeys = new System.Collections.Generic.List<int>();
    public System.Collections.Generic.List<float> clientSats = new System.Collections.Generic.List<float>();
}
