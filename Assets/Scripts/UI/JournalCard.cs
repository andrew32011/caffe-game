/// <summary>
/// Батч 6: одна карточка гостя в журнале «Завсегдатаи». Вешается на шаблон-карточку;
/// поля TMP/Image назначаются в инспекторе. Заполняется через Bind().
/// Сцена: MainScene (UI)
/// Зависимости: CharacterNames, Loc, TMPro
/// SDK: Нет
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JournalCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private TextMeshProUGUI _visitsText;
    [SerializeField] private TextMeshProUGUI _starsText;
    [SerializeField] private Image           _sympathyFill; // Image: Filled, Horizontal
    [SerializeField] private TextMeshProUGUI _sympathyText;

    public void Bind(CharacterType type, float sympathy, int visits, int bestStars)
    {
        int s = Mathf.Clamp(bestStars, 0, 3);

        if (_nameText    != null) _nameText.text    = CharacterNames.Get(type);
        if (_statusText  != null) _statusText.text  = CharacterNames.Status(sympathy);
        if (_visitsText  != null) _visitsText.text  = Loc.T("Визитов: ", "Visits: ") + visits;
        if (_starsText   != null) _starsText.text   = s + " / 3";
        if (_sympathyFill != null) _sympathyFill.fillAmount = Mathf.Clamp01(sympathy);
        if (_sympathyText != null) _sympathyText.text = Mathf.RoundToInt(sympathy * 100f) + "%";
    }
}
