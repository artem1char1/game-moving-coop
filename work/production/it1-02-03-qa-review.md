# Независимый QA IT1-02/03 — 2026-11-01

Вердикт: **CHANGES_REQUESTED**. Стабильный снимок Assets/Game (13 runtime C# плюс editor authoring), Iteration01.unity, Windows build от 31.10.2026 17:58. Код и сцена не менялись. Пакеты NGO 2.13.3 / builtin Transport 6.6.0.

## Дефект

**P2 — сообщение отключения клиента немедленно теряется.** `game-moving-coop/Assets/Game/Bootstrap/GameBootstrap.cs:95` безусловно заменяет `hud.Notice` на SHIFT ACTIVE при каждом authority snapshot. В `:71` обработчик disconnect устанавливает полезное сообщение, но следующий snapshot (примерно 0.05 s) его стирает. Repro: host+client, Ready→Start, закрыть клиент, наблюдать следующий snapshot хоста. Expected: хост устойчиво видит изменение состава/отключение. Actual: снова предложение исследовать дом, текст disconnect практически невидим. Подтверждено трассировкой реального disconnect и чтением безусловного пути; отдельно визуально текст после disconnect не захвачен.

## Независимое evidence

- Два настоящих Windows процесса: `work/production/spike/qa-host.jsonl`, `qa-client.jsonl`, соответствующие .log. Host 40 s, client 23 s; `--moving-role host|client --moving-scenario movement --moving-seconds N --moving-evidence ABS.jsonl`, отдельный `-logFile ABS.log`.
- Ready после initial snapshot, Start, оба участника Active. 39 sampled revisions совпали по полному массиву players. По маршруту x = ±0.5, z от -6 до 8.5 и обратно около 0.5; скорость 4 m/s и fixed 0.02 просмотрены в коде. Ограничитель/стена не пройдены. Клиент удалён из host snapshot после выхода; хост остался Active.
- Повтор `qa2-host/client/late.jsonl`: клиент ушёл, поздний клиент получил `Session full or shift already active. Join in preparation.` и завершился по deadline. Первый qa-late прогон не доказателен: попал после ухода хоста.
- Unity CLI beta.15: Editor 6000.6.7f1 ready, compiling=false, playMode=stopped; Iteration01 active, loaded, isDirty=false. console status: 0 errors, 0 warnings, compilationFailed=false. В QA player логах нет Exception/Error/Warning (есть общие D3D info queue diagnostics).
- Computer-use GUI: подготовка реально отображает читаемые Host/адрес/Join и нижние подсказки, ортографический greybox/фургон/зона видимы. Hidden scenario processes не экспонируют окна, ScreenCapture PNG не создан. Активный HUD, local wall hide/restore, ручной WASD и диагональ не подтверждены визуальным независимым маршрутом; нормализация и mapper просмотрены статически. IT1-02 нельзя закрывать как полностью принятую только по этим данным.

Ограничения: localhost, без LAN/задержек/потерь. Сборка повторно не создавалась, бинарное соответствие исходникам отдельно не доказано; driver auto-quit реально работает. Груз/квота/restart IT1-04/05 вне этой приёмки. Editor не изменялся и освобождён stopped; тестовые процессы завершаются по deadline, интерактивное окно закрыто отдельно.

