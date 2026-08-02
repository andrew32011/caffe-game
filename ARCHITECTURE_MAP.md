# caffe-game — Карта архитектуры (skill: analyze)

> Компактный справочник по коду. Для экономии токенов: в новой итерации читать ЭТОТ файл вместо пересканирования.
> Дополняет `PROJECT_SCHEMATIC.md` (там дизайн/экономика/поток), здесь — код/архитектура/зависимости/долги.
> Активный проект: **`D:\GamePushMade\caffe-game`** (Unity, built-in RP, WebGL/Yandex через плагин YG2).

## 0. Слои (Assets/Scripts/…)
`Core` (менеджмент/данные) · `Gameplay` (готовка/гость) · `Story` (контент) · `Dialog` · `UI` · `Audio` · `SDK` (сейв/Yandex) · `CutScene` (интро-камера) · `Effects` (виньетки) · `StagesPointers` (маркеры этапов) · корень (`Stages`, `ProcessVisitor`, `Speech*`, `FlamesController`).
Сцены строятся **кодом** из `Assets/Editor/CoffeGameSceneSetup.cs` (MainScene, 1494 стр.) и `IntroSceneSetup.cs` (SampleScene). Меню: `Tools → CoffeGame → Build Scene Systems + UI` / `Build Intro`.

## 1. Карта систем (файл → тип → роль → ключевое API/деп)

### Core
- **GameManager** (656, *singleton*) — оркестратор: цикл 40 дней, фазы (`GamePhase`), сейв через `YG2.saves`, апгрейды, лидерборд, реклама (interstitial/transition), «Сон», финал. Держит ссылки на все системы. Публичн.: `AddCoins`, `CurrentDay`, `TotalCoins`, `Get/SetClientSatisfaction`, `RecordVisit/GetVisits/GetBestStars`, `CarriedCombo`, `MarkTipShown`, `PriceMultiplier/ToleranceBonus/MoodBonus`, `ResetProgressAndRestart`, **endless (Батч 9): `RunEndlessDays`, `EndlessActive/Day/BestDay`, `SubmitEndlessLeaderboard`, `WaitForClick`**. ⚠️ god-class.
- **DayController** (378) — один рабочий день: спавн гостя → этапы Stages → диалог → готовка → оценка → оплата (формула) → реакция → уход. Хуки: `RunDay`, `CurrentComboCount`, `IsSpecialDay`. Экономика оплаты тут.
- **GameEnums** (220) — enum'ы (`GamePhase/DayPhase/CoffeeType/Volume/SweetnessLevel/Topping/UpgradeType/CharacterType`) + `[Serializable]` `CoffeeOrder`, `CustomerData`, `GameSaveData`. ⚠️ `Topping` = предметы полки `ShelfItems` (BellPepper/BundtCake/Cookies/Salami/Salmon/Wasabi/Lollipop/Tomato/Pretzel); билдер сопоставляет по **имени** дочернего объекта (`ToppingByName`), не по индексу.
- **Difficulty** (35, *static*) — кривые `Tolerance(day)`, `EarlyEase(day)`, `FinalDay=40`. Клампится к [1,40], поэтому в endless (день>40) сложность = максимум финала.
- **EndlessMode** (*static*, Батч 9) — процедурный генератор `DayData` для бесконечного режима после дня 40: `BuildDay(endlessDay)` (детерминир. seed=день → корректный resume), `DisplayDayNumber` (40+N), `CustomersForDay` (2→5). Атмосферные реплики без спойлеров. Читает `GameManager.RunEndlessDays`.
- **Loc** (51, *static*) — язык из **`YG2.envir.language`** (EnvirData), фолбэк `YG2.lang`→"ru". `T(ru,en)`, `IsRu`, `Lang`. См. §5.
- **UiTranslations** (130, *static*) — таблица переводов статических подписей на 20 языков (ключ = рус. текст).
- **LocalizeYG** (85, *MonoBehaviour*) — вешается билдером на статик-подписи: применяет перевод из `UiTranslations`/встроенных ru/en/tr после `YG2.isSDKEnabled`.
- **CharacterNames** (67, *static*) — имя типа гостя + `FavoriteTopping` + `Status(симпатия)`.
- **GameInput** (12, *static*) — глобальный `Locked` (блок кликов).
- **PerformanceSetup** (34) — на Awake: targetFrameRate, тени off, AA off, pixelLightCount.

### Gameplay
- **CoffeeCraftingSystem** (718, *singleton* + *static* реестр `_items`) — оркестратор готовки: зоны (ингредиенты→машина→топпинги→подача), подсказки (расплывчатая/точная за rewarded), комплимент, себестоимость, апселл любимого топпинга, `PrecisionBonus`. ⚠️ крупный.
- **MachineMinigame** (97) — 2 верт. шкалы (темп/объём), клик фиксирует бегунок. `Update()` пинг-понг.
- **CupController** (203) — кружка: движение по якорям зон, налив, передача гостю.
- **CustomerController** (352) — модель гостя: спавн/удаление, эмоции, маршрут через `ProcessVisitor`, `SatisfactionBar`. Содержит *static* реестр.
- **IngredientItem** (98) — кликабельный 3D-предмет (основа/топпинг), `displayName/displayNameEn`. `OnMouseDown`→крафтинг. `Update()` пульс.
- **SatisfactionBar** (120) — префаб-полоса над гостем. `Update()` следит за камерой.
- **DailyChallenge** — «Заказ дня»: детерминир. квест (seed=день), **10 типов** (`Kind`: EarnCoins/PerfectHits/ThreeStars/SellToppings/GoodDrinks/ServeDrinks/StarCollector/HighSatisfaction/TwoStarsPlus/BigEarnings), `BeginDay/ReportDrink/Claim`.
- **HeroIdle** (50) — процедурный idle ГГ (запасной). `Update()`.

### Story / Dialog
- **StoryDatabase** (1745, *ScriptableObject*) — контент 40 дней: `DayData/DayCustomerEntry/DialogueLine` (ru+en), пул шуток. `GetDay(n)`. Ассет `Assets/StoryDatabase.asset`. ⚠️ огромный, но это данные.
- **DialogueDisplayer** — показ реплики **по словам за ~3с** (`revealDuration`; 1-й клик=вся реплика, 2-й=дальше, `advanceGuard` от быстрого проскока), имя, заставки, `PlayDialogueLines`, `ShowMessage`, бубнёж через `SpeechMixer`. `continueHint` — мигающий (`BlinkText`) «нажмите для продолжения». ⚠️ 14 public-полей (UI-ссылки).
- **BlinkText** (*MonoBehaviour*, UI) — пульс альфы графики (для хинта продолжения); билдер вешает на `ContinueHint`.
- **DialogueManager** (164) + **DialogLine** (64, содержит *SO* `DialogueDatabase`) — СТАРАЯ система реплик по ID. ⚠️ **legacy**, сюжет идёт через StoryDatabase.

### UI (все — панели, оркестрируются билдером)
- **ButtonJuice** (MonoBehaviour) — лёгкая анимация кнопки: дыхание масштаба/качание/блеск/подскок при клике (unscaled). Билдер вешает через хелпер `Juice(btn,pulse,shine,wobble)` на важные кнопки (Подать=pulse+shine, Подтвердить/Продолжить=pulse, HUD-иконки). НЕ вешать на кнопки со своим рантайм-пульсом (Double/SaveCombo/AdHint) — конфликт масштаба.
- HUD-кнопки (Меню/Журнал/Подсказка) — **иконка+мини-подпись** (хелпер `IconBtn`), **цветные иконки** Settings/Book/Blue Energy из `Mini UI/Icons` (не тонируются, `Color.white`), правый вертикальный док (x≈0.9, сверху вниз). Спрайт кнопок — прямоугольный `Dark Long Btn DARK` (минимальное скругление). На обучении (день 1) `TutorialController` прячет `BtnJournal`/`BtnHint`, оставляя только настройки.
- **DayResultUI** (344) — экран итогов дня: rewarded «Удвоить»/«Сохранить комбо», трекер «Путь к 10000», предупреждение о стрике.
- **TutorialController** (292) — обучение. **UiEffects** (263, *singleton*) — 2D эффекты (монеты/звёзды/комбо/баннеры). **HintManager** (166) — панель подсказок. **UpgradeShopUI** (121) — магазин апгрейдов. **SettingsUI** (128) — настройки+пауза (громкость/фуллскрин/лидерборд). **DailyBonusUI** (123) — бонус за вход. **JourneyGateUI** (102) — гейт цели. **GuestJournalUI** (73) + **JournalCard** (32) — журнал «Завсегдатаи». **AdForCoins** (44) — реклама за монеты. **CoinsUI** (30) — касса, `Update()`.

### Audio / SDK / CutScene / Effects / прочее
- **AudioController** (*singleton*) — музыка/SFX/бубнёж. **Три независимых канала громкости** (Батч 9): `MusicVolume`, `SfxVolume`, `VoiceVolume` (бубнёж; `SpeechMixer` читает его, а не Sfx). Сейв `music/sfx/voiceVolume`; `SetVolumes(m,s,v)`/`ApplySavedVolumes(m,s,v)` — 3 аргумента. Превью: `PlaySfxPreview` (реальный SFX-клик), `PlayVoicePreview` (бубнёж). Клипы берёт из **`SoundBank`** (`_bank`, приоритет) с фолбэком на старые поля. Методы `PlayCoin/Star/...`, `PlayClick/UiOpen/UiClose/Combo`, `PauseMusic/ResumeMusic` (музыка молчит ночью), **`PlayNight/StopNight`** — ночной эмбиент `_nightAmbience` (`Assets/Audio/night_ambience.mp3`, отдельный зацикленный `_nightSource`; вкл. во «Сне», выкл. до рекламы). ⚠️ Порядок между днями: сон → StopNight → реклама (`Transition`) → ResumeMusic.
- **SoundBank** (*ScriptableObject*, `Assets/SoundBank.asset`) — именованные события→AudioClip + `music` + `all[]` (50 клипов пакета `Assets/Casual Game Sounds U6/DM-CGS-01..50`). Билдер (`BuildSoundBank`) наполняет: `all`=все клипы, `music`=`Assets/Audio/bg_music_celtic.mp3`, события — черновой best-guess по индексу (пустые слоты; ручные назначения сохраняются). Фон-музыка — кельтская.
- **ButtonClickSound** (MonoBehaviour) — клик-звук на ВСЕ кнопки (в фабриках Btn/IconBtn), transform не трогает.
- Камера: `Stages` поднимает камеру на этапе ожидания гостя (`guestWaitStageIndex`/`guestWaitCameraLift`, +Y на endPos).
- **SpeechFragment/SpeechMixer** (131) — нарезка бубнежа `Пер3.ogg`, кэш клипов, громкость = `SfxVolume`. **SpeechPlayer** (39).
- **SavesYG.Game** (56, *partial* `SavesYG`) — поля облачного сейва YG2. **YandexManager** (146, *singleton*) — инициализация YG2.
- **Stages** (176) — машина этапов 0–7 (двигает камеру по `cameraTarget`), `JumpToStage`, `OnStageEntered`, `IsTransitioning`. `Update()` debug. + **StagesPointers/Stage0–7** — маркеры фаз (OnEnable-триггеры).
- **ProcessVisitor** (184) — маршрут гостя по точкам. `Update()`.
- **CameraWaypointController** (387, интро) — плавный облёт камеры → `IntroStoryUI.Begin`. ⚠️ 17 public-полей (конфиг), комменты в битой кодировке.
- **IntroStoryUI** (149, *singleton*) — интро-история + таймер кнопки + пропуск при `introSeen`.
- **VisualEffectsController** (307) — виньетки/переходы/затемнения/тряска.

## 2. Граф зависимостей (главное)
```
YandexManager ─ инициализация ─▶ YG2 (плагин) ◀─ Loc, SavesYG, GameManager, LocalizeYG
GameManager ─(держит)▶ DayController, DialogueDisplayer, VisualEffectsController,
             DayResultUI, AudioController, HintManager, Stages, StoryDatabase,
             JourneyGateUI, DailyBonusUI, TutorialController
DayController ─▶ Stages, CustomerController, CoffeeCraftingSystem, DialogueDisplayer,
                 DailyChallenge, GameManager(Instance), UiEffects(Instance), AudioController
CoffeeCraftingSystem ─▶ Stages, CupController, MachineMinigame, IngredientItem(реестр),
                        GameManager(Instance), GameInput, Loc
CustomerController ─▶ ProcessVisitor, SatisfactionBar
Все UI ─▶ GameManager.Instance, Loc/UiTranslations
Билдер (Editor) ─ создаёт объекты и проставляет [SerializeField] через SerializedObject (класс W)
```
Связь преимущественно через **singleton.Instance** и `[SerializeField]`-ссылки, проставляемые билдером. Нет DI/event-bus/сервис-локатора.

## 3. Паттерны (оценка)
- **Singleton** — GameManager/AudioController/CoffeeCraftingSystem/UiEffects/YandexManager/IntroStoryUI. Работает, но GameManager и CoffeeCraftingSystem тяготеют к god-class.
- **State machine** — `Stages` (этапы 0–7) + `DayPhase`/`GamePhase`. Норм.
- **ScriptableObject-конфиг** — StoryDatabase (контент). Хорошо.
- **Observer** — только `YG2.onSwitchLang`, `Stages.OnStageEntered`, UnityEvents. Событийность слабая, больше прямые вызовы `Instance`.
- **Code-driven scene** — вся сцена собирается билдером (нет ручной расстановки). Оригинально; правки UI = правка билдера + пересборка.
- Нет: пулинга (гостей спавним по одному — ок), Assembly Definitions (весь код в Assembly-CSharp).

## 4. Анти-паттерны / тех-долг (флаги)
- **`FindObjectOfType`/`GameObject.Find`**: `GameManager`(свет), `Stage0`/`Stage7`(ProcessVisitor). Мелочь; лучше ссылки.
- **`Update()`-полинг**: частично разгружен (Батч 9-perf). `Stages.Update` — теперь целиком под `#if UNITY_EDITOR` (в билде метода нет). `IngredientItem` — пульс переведён на корутину (десяток предметов больше не гоняет пустой Update; работает только подсвеченный). `JournalBadge.Update` — троттлинг ~2×/сек. Осталось оправданным: MachineMinigame (только при активном минигейме), CameraWaypoint/ProcessVisitor/CustomerController (движение), SatisfactionBar (billboard, живёт только при госте), DialogueDisplayer (ввод), ButtonJuice (juice HUD-кнопок — при желании можно ужать `_shine`, т.к. смена color/кадр дёргает canvas rebuild). `CoinsUI` — Update есть, но с guard'ом (пишет только при смене баланса) → фактически бесплатен.
- **Legacy-код**: `DialogueManager` + `DialogueDatabase`(в DialogLine.cs) — не используются сюжетом, кандидаты на удаление.
- **public-поля вместо `[SerializeField] private`**: `DialogueDisplayer`(14), `CameraWaypointController`(17), `Stages`(16) — ослабляют инкапсуляцию.
- **Битая кодировка комментариев** (mojibake) в `CameraWaypointController`, местами в `Stages`/`DialogLine` (старые файлы, cp1251).
- **god-class**: `GameManager`(656) и `CoffeeCraftingSystem`(718) — можно дробить (сейв/реклама/апгрейды вынести из GM).
- **Аудио**: клипы музыки/SFX не назначены — реальный звук только бубнёж (осознанно, музыку добавят позже).
- **Билдер 1494 стр.** — единая точка отказа; ссылки на объекты сцены по именам (`GameObject.Find`) хрупкие.

## 5. Критичная конфигурация платформы (см. [[caffe-game-localization]])
- Язык: `Loc` → `YG2.envir.language` (EnvirData). Статик-подписи — `LocalizeYG` (после `isSDKEnabled`).
- ⚠️ Define **`YandexGamesPlatform_yg`** ДОЛЖЕН быть активен (иначе билд использует generic `navigator.language`, реклама/сейвы — заглушки). Задаётся через `SettingsYG2.asset` → `Basic.platform` (ссылка на `YandexGames.asset`) + WebGL scripting defines. Проверка билда: `brotli -d -c *.framework.js.br | grep LangRequest_js`.
- `setLanguageMod = EveryGameLaunch` (перечитывать язык каждый старт).

## 6. Диаграмма (поток)
```
SampleScene: CameraWaypoint → IntroStoryUI(история, 1-й вход) ──LoadScene──▶ MainScene
MainScene: YandexManager→SDK ▶ GameManager.StartGameFlow
  → Tutorial → цикл дней: DayController.RunDay
      Stages(0 приход →1 диалог →2/3/4 готовка[CoffeeCraftingSystem+Cup+Machine] →5 подача →6 реакция →7 уход)
      → оплата(GameManager.AddCoins) → DayResultUI → PauseMusic → «Сон»(VFX+ночной эмбиент) → StopNight → Transition(interstitial) → ResumeMusic → след. день
  → день 40: JourneyGate(10000) → финал
UI-слой (Canvas, всегда активен): Settings/Journal/Hint/Shop/DailyBonus/Coins/UiEffects
```
