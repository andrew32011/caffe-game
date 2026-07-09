/// <summary>
/// Батч 6: локализованные имена типов гостей (для журнала «Завсегдатаи»).
/// Переиспользует уже существующие названия из комментариев CharacterType (GameEnums).
/// Сцена: Глобально (читает GuestJournalUI)
/// Зависимости: Loc
/// SDK: Нет
/// </summary>
using UnityEngine;

public static class CharacterNames
{
    public static string Get(CharacterType t)
    {
        switch (t)
        {
            case CharacterType.Traveler:       return Loc.T("Странник", "Traveler");
            case CharacterType.WaterGuard:     return Loc.T("Водяной страж", "Water Guard");
            case CharacterType.ShadowMerchant: return Loc.T("Теневой торговец", "Shadow Merchant");
            case CharacterType.FireAlchemist:  return Loc.T("Огненный алхимик", "Fire Alchemist");
            case CharacterType.BookKeeper:     return Loc.T("Хранитель книг", "Book Keeper");
            case CharacterType.MirrorThief:    return Loc.T("Зеркальный вор", "Mirror Thief");
            case CharacterType.TimeCourier:    return Loc.T("Временной курьер", "Time Courier");
            case CharacterType.StarShepherd:   return Loc.T("Звёздный пастух", "Star Shepherd");
            case CharacterType.OrderStranger:  return Loc.T("Незнакомка Ордена", "Order Stranger");
            case CharacterType.FogHunter:      return Loc.T("Туманный охотник", "Fog Hunter");
            case CharacterType.CrystalSinger:  return Loc.T("Кристаллическая певица", "Crystal Singer");
            case CharacterType.SteamEngineer:  return Loc.T("Паровой инженер", "Steam Engineer");
            case CharacterType.MoonSmith:      return Loc.T("Лунный кузнец", "Moon Smith");
            case CharacterType.Andrei:         return Loc.T("Андрей", "Andrei");
            case CharacterType.Lira:           return Loc.T("Лира", "Lira");
            case CharacterType.Cartographer:   return Loc.T("Картограф", "Cartographer");
            case CharacterType.Herbalist:      return Loc.T("Травница", "Herbalist");
            case CharacterType.ClockKeeper:    return Loc.T("Часовщик", "Clock Keeper");
            case CharacterType.GraveWarden:    return Loc.T("Смотритель погоста", "Grave Warden");
            case CharacterType.Beekeeper:      return Loc.T("Пасечник", "Beekeeper");
            case CharacterType.EchoTwin:       return Loc.T("Эхо-близнец", "Echo Twin");
            case CharacterType.Bard:           return Loc.T("Бард", "Bard");
            case CharacterType.Cartomancer:    return Loc.T("Гадалка", "Cartomancer");
            case CharacterType.Lamplighter:    return Loc.T("Фонарщик", "Lamplighter");
            case CharacterType.Smuggler:       return Loc.T("Контрабандист", "Smuggler");
            case CharacterType.Defector:       return Loc.T("Перебежчик Ордена", "Order Defector");
            case CharacterType.Widow:          return Loc.T("Вдова", "Widow");
            case CharacterType.Glassblower:    return Loc.T("Стеклодув", "Glassblower");
            case CharacterType.Grandmother:    return Loc.T("Бабушка", "Grandmother");
            default:                           return Loc.T("Гость", "Guest");
        }
    }

    /// <summary>Батч 6: детерминированный «любимый топпинг» гостя (один из 5 реальных,
    /// без None). Одинаков для типа гостя во всех сессиях — основа апселла в журнале.</summary>
    public static Topping FavoriteTopping(CharacterType t)
    {
        int h = ((int)t * 73856093) ^ 19349663;
        int idx = Mathf.Abs(h) % 5;          // 0..4
        return (Topping)(idx + 1);           // Cream(1)..Mint(5), минуя None(0)
    }

    /// <summary>Статус отношений по симпатии 0..1 (для журнала).</summary>
    public static string Status(float sympathy)
    {
        if (sympathy >= 0.9f) return Loc.T("Завсегдатай ⭐", "Regular ⭐");
        if (sympathy >= 0.7f) return Loc.T("Друг кофейни", "Café friend");
        if (sympathy >= 0.5f) return Loc.T("Знакомый", "Acquaintance");
        if (sympathy >= 0.3f) return Loc.T("Прохладно", "Cool");
        return Loc.T("Незнакомец", "Stranger");
    }
}
