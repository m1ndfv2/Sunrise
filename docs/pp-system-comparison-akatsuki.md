# Сравнение новой логики PP в Sunrise с osuAkatsuki через Observatory

## Что сейчас делает Sunrise

1. **Перед запросом на калькулятор удаляет RX/AP моды** (`Relax`, `Relax2`) через `IgnoreNotStandardModsForRecalculation`, поэтому во внешний калькулятор уходит «очищенный» набор модов.  
2. **После ответа калькулятора применяет локальный пересчёт PP для RX/AP** в `ApplyNotStandardModRecalculationsIfNeeded` (Relax STD/CTB и Autopilot STD), используя собственные формулы `RelaxPerformanceCalculator`.  
3. В новой логике учёта лучших скорoв для статы, если включён `UseNewPerformanceCalculationAlgorithm`, персональный best может определяться **по PP**, а не только по score value (`SelectUsersPersonalBestScores(..., rankByPerformancePoints: true)` и `BestScoreForPerformanceCalculation`).

## Что делает osuAkatsuki (bancho.py)

По их публичной реализации (`app/usecases/performance.py`):

- Нормализуется `NC => DT` перед расчётом.
- Вызывается движок `akatsuki_pp_py`.
- Результат `pp` после расчёта:
  - клэмпится на `NaN/Inf => 0`;
  - округляется до **3 знаков**.

## Как это реализовано в Observatory (репозиторий m1ndfv2/Observatory)

В parity-режиме (`PP_ENGINE_MODE=akatsuki_parity`):

- Нормализуется `NC => DT`.
- RX/AP не пересчитываются отдельными формулами; вместо этого применяются **конфигурируемые мультипликаторы** (`PpRelax*Multiplier`, `PpAutopilotStdMultiplier`).
- Финальный PP приводится к Akatsuki-стилю: `!finite => 0`, иначе `toFixed(3)`.

## Ключевые расхождения Sunrise vs Akatsuki/Observatory parity

1. **RX/AP pipeline отличается.**
   - Sunrise: remove RX/AP перед расчётом + локальные формулы после ответа.
   - Observatory parity: normal calculation + server-side multipliers.

2. **Округление финального PP.**
   - Akatsuki/Observatory parity: фиксировано 3 знака.
   - Sunrise: сохраняет `double` без явного финального `round(3)` в пайплайне score/beatmap калькуляции.

3. **Источник «best score для PP» в новой логике Sunrise.**
   - Sunrise может выбирать лучшую попытку по PP (а не по total score), что влияет на пересчёт user stats и на weighted PP.
   - Это поведение нужно отдельно валидировать с live-паритетом, так как в Akatsuki-системах исторически часто используется best-per-beatmap, но детали зависят от сервера и таблицы.

## Практический вывод

Если цель — максимально близкий паритет с Akatsuki через Observatory, то текущая Sunrise-реализация уже частично покрывает миграцию (новый флаг и выбор best по PP), но **по RX/AP и финализации PP (round/clamp) есть семантические отличия**. Для полного паритета стоит ориентироваться на модель из Observatory parity-документа: dual-run, сбор дельт, метрики gate, и только затем cutover.

## Внешние источники для проверки

- osuAkatsuki: <https://github.com/osuAkatsuki/bancho.py/blob/master/app/usecases/performance.py>
- Observatory parity doc: <https://github.com/m1ndfv2/Observatory/blob/master/docs/pp-akatsuki-parity-migration.md>
