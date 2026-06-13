/// <summary>
/// Глобальный замок управления. Когда Locked = true — клики по предметам
/// (IngredientItem) игнорируются. GameManager поднимает замок в переходах,
/// на экране результатов и во время затемнений; опускает на время рабочего дня.
/// Сцена: Глобально
/// Зависимости: Нет
/// SDK: Нет
/// </summary>
public static class GameInput
{
    public static bool Locked = true;
}
