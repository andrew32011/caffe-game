// Кастомный мост к Yandex SDK «Ярлык на рабочий стол» (ysdk.shortcut).
// Плагин YG2 не предоставляет этот API, поэтому дёргаем глобальный ysdk напрямую.
// Все обращения защищены typeof/try-catch: если ysdk или shortcut недоступны —
// функции просто ничего не делают (сборку и рантайм не ломают).
mergeInto(LibraryManager.library, {

  YaShortcut_Available: function () {
    try {
      if (typeof ysdk !== 'undefined' && ysdk && ysdk.shortcut) return 1;
    } catch (e) {}
    return 0;
  },

  YaShortcut_Prompt: function () {
    try {
      if (typeof ysdk === 'undefined' || !ysdk || !ysdk.shortcut) return;
      ysdk.shortcut.canShowPrompt().then(function (p) {
        if (p && p.canShow) { ysdk.shortcut.showPrompt(); }
      }).catch(function (e) {});
    } catch (e) {}
  }

});
