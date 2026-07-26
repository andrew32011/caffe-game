/// <summary>
/// Бесконечный режим (после 40-дневного сюжета). Процедурно собирает DayData со
/// случайными гостями и заказами, переиспользуя обычный поток дня (DayController).
/// Сложность зафиксирована на максимуме (Difficulty.Tolerance клампится к дню 40),
/// оплата — по верхней авторской ставке. Число гостей плавно растёт, удлиняя сессию.
/// Реплики — атмосферные и БЕЗ сюжетных спойлеров (история уже завершена).
///
/// Генерация ДЕТЕРМИНИРОВАНА по номеру бесконечного дня: обновление страницы (или
/// возврат из сцены сна) даёт тот же день и тех же гостей → корректно работает
/// продолжение дня с прерванного гостя (ResumeCustomerIndex).
///
/// Сцена: MainScene (вызывает GameManager).
/// Зависимости: GameEnums, CharacterNames, Difficulty, Loc.
/// SDK: Нет
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public static class EndlessMode
{
    /// <summary>Сколько гостей в бесконечный день N: 2 на старте, +1 каждые 4 дня, максимум 5.</summary>
    public static int CustomersForDay(int endlessDay) =>
        Mathf.Clamp(2 + (Mathf.Max(1, endlessDay) - 1) / 4, 2, 5);

    /// <summary>Под каким «номером дня» показывать бесконечный день (продолжает счёт после 40).</summary>
    public static int DisplayDayNumber(int endlessDay) => Difficulty.FinalDay + Mathf.Max(1, endlessDay);

    // Напитки, доступные в бесконечном режиме (без сюжетного «Кофе Правды»).
    static readonly CoffeeType[] Drinks =
    {
        CoffeeType.Espresso, CoffeeType.Americano, CoffeeType.Cappuccino, CoffeeType.Latte,
        CoffeeType.Mocha, CoffeeType.HerbalTea, CoffeeType.GreenTea, CoffeeType.HotChocolate,
        CoffeeType.BlackCoffee
    };

    static readonly (string ru, string en)[] Greetings =
    {
        ("Доброго дня! Говорят, ваша кофейня открыта даже на краю миров.",
         "Good day! They say your café stays open even at the edge of worlds."),
        ("Как здесь уютно. Сделаете мне что-нибудь по вкусу?",
         "It's so cozy here. Would you make me something to my taste?"),
        ("Долгий путь через туман. Согрейте чашкой, будьте добры.",
         "A long road through the fog. Warm me with a cup, please."),
        ("Слышал, здесь варят лучший кофе меж мирами. Проверим?",
         "I heard you brew the best coffee between the worlds. Shall we see?"),
    };

    static readonly (string ru, string en)[] Reveals =
    {
        ("Именно то, что нужно. Спасибо — я ещё вернусь.",
         "Exactly what I needed. Thank you — I'll be back."),
        ("Тепло и по сердцу. Кофейня живёт — и это главное.",
         "Warm and heartfelt. The café lives on — and that's what matters."),
        ("Прекрасно. Пусть двери «Междумирья» не закрываются никогда.",
         "Wonderful. May the doors of the Inbetween never close."),
    };

    static readonly (string ru, string en)[] Wrongs =
    {
        ("Хм, не совсем то. Но всё равно спасибо.",
         "Hmm, not quite right. Thanks anyway."),
        ("Я ожидал другого… В следующий раз получится.",
         "I expected something else… Next time for sure."),
    };

    /// <summary>Собирает данные одного бесконечного дня (детерминированно по номеру дня).</summary>
    public static DayData BuildDay(int endlessDay)
    {
        endlessDay = Mathf.Max(1, endlessDay);
        var rng = new System.Random(1000 + endlessDay); // seed = день → те же гости после reload

        var day = new DayData
        {
            dayNumber = DisplayDayNumber(endlessDay),
            coinsPerCorrectOrder = 50,   // верхняя ставка (сюжет давал 10..50)
            hasVignette = false,
            dayEndText   = "Ещё один день в «Междумирье». Кофейня не спит.",
            dayEndTextEn = "Another day at the Inbetween. The café never sleeps.",
            customers = new List<DayCustomerEntry>()
        };

        int count = CustomersForDay(endlessDay);
        int charCount = System.Enum.GetValues(typeof(CharacterType)).Length;

        for (int i = 0; i < count; i++)
        {
            var type = (CharacterType)rng.Next(0, charCount);
            string speaker = CharacterNames.Get(type); // имя на текущем языке

            var order = new CoffeeOrder
            {
                type    = Drinks[rng.Next(0, Drinks.Length)],
                volume  = (Volume)rng.Next(0, 3),
                sweet   = (SweetnessLevel)rng.Next(0, 4),
                topping = rng.Next(0, 3) == 0
                    ? Topping.None
                    : ToppingUtil.ShelfToppings[rng.Next(0, ToppingUtil.ShelfToppings.Length)],
            };

            var g = Greetings[rng.Next(0, Greetings.Length)];
            var r = Reveals[rng.Next(0, Reveals.Length)];
            var w = Wrongs[rng.Next(0, Wrongs.Length)];

            day.customers.Add(new DayCustomerEntry
            {
                characterType    = type,
                stickmanIndex    = rng.Next(0, 9),
                order            = order,
                greetingLines    = Line(speaker, g.ru, g.en),
                storyRevealLines = Line(speaker, r.ru, r.en),
                wrongOrderLines  = Line(speaker, w.ru, w.en),
            });
        }
        return day;
    }

    static List<DialogueLine> Line(string speaker, string ru, string en) => new List<DialogueLine>
    {
        new DialogueLine { speakerName = speaker, speakerNameEn = speaker, text = ru, textEn = en }
    };
}
