# Финальный вердикт Fix1 — PASS в проверенных границах (01.11.2026)

Кандидат `build_c3c2169c99d5`, `work/Builds/Iteration01-Fix1/MovingCrew.exe`. Раунд2 завершён. Открытых воспроизведённых P0–P3 по назначенному срезу нет. Все SHA из `it1-04-05-fix1-source-sha256.txt` совпали. Код/сцена не изменялись QA. Ниже сохранён исторический раунд1: его CHANGES_REQUESTED заменён этим вердиктом.

## Закрытие замечаний

- **P2 disconnect notice — CLOSED.** В Bootstrap:86 сохраняется previousPhase до frame=incoming, :99 вызывает SnapshotNotice.Apply. Active announcement теперь только при переходе; повторный Active сохраняет disconnect notice. Независимый тест ActiveSnapshotsPreserveDisconnectNotice PASS (20повторов), реальная пара7824 подтверждает disconnect/удаление клиента/освобождение обеих ручек. Визуальный снимок текста после disconnect отдельно не делался; закрытие основано на production callsite+повторном тесте и реальном событии.
- **P2 reservation race — CLOSED.** Reservation проверяется при принятии approach, отменяется release/focus/stale/disconnect/reset; pending reservation запрещает delivery dwell. QA повторил unit far-first/near-second и cancel/reacquire/reset — PASS. Встроенный driver race7825 оказался недоказательным: условие client first.z>4.02 сработало при транзите хоста к4.1 до его E, выигрыш клиента не является дефектом gameplay. Поэтому QA выполнил собственный обычный inputscript7827 (hostE4s, clientE4.1s) через существующий transport без authority bypass. Host шёл из более дальней позиции, client ожидал около ручки. Реальный snapshot t6.031: **Occupied**, carrier=-1, host продолжает подход; t6.531: **Holding, box5.first=0**, client остался свободным. Клиент не перехватил первый валидный запрос. Evidence `spike/qa-fix1-race-{host,client}.json/jsonl/.log`.
- **P3 HUD encoding — CLOSED.** Native computer-use screenshot именно нового Fix1 окна подготовки показал читаемые ASCII HOST, подзаголовок, WASD и E/Q/R/F без mojibake. Дополнительно проверены source и строки нового compiled MovingCoop.UnityAdapters.dll. Старое пользовательское окно не трогалось, игровой клавиатурный ввод не посылался. Новое окно Fix1 оставлено открытым в подготовке.

## Независимые повторные проверки

`test run --mode editor --filter MovingCoop.Tests --async_tests true` → `test status completed`: **15/15 PASS,0failed/skipped**, duration0.79s. Проверен каждый итог теста, не только exit code.

**U02 software-input/реальная сцена PASS:** новый настоящий InputSystem.Keyboard получает QueueStateEvent W,D,W+D; production LocalInput.Read/ToWorld подтверждает экранные направления, равную длину диагонали и cardinal, zero при focus=false. Это software keyboard, не физический OS keyboard playtest. В настоящей authored Iteration01 production UpdateView следует за target, скрывает стены для player и cargo point, затем восстанавливает renderer после удаления перекрытия; colliders остаются enabled. Scene list_open после теста: loaded/active, isDirty=false. Это не заявление о полном ручном UI маршруте. Ранее наблюдавшийся пользовательский маршрут остаётся атрибутированным пользователю.

Две настоящие Windows роли на новом exe, свои порты7820–7827; все конечные логи `work/production/spike/qa-fix1-*`:

- 7820 sofa: **320, Success:Evacuated** host/client (63success samples клиента); диван+3коробки, поворот/двери и ledger после сдачи без повторного начисления.
- 7821 quota: **320, Success:Evacuated** host/client (99success samples клиента);8коробок.
- 7822 focus: оба носильщика наблюдались5samples, затем обе ручки=-1,0денег.
- 7823 stale: оба носильщика наблюдались6samples, затем обе ручки=-1,0денег.
- 7824 disconnect: оба носильщика наблюдались6samples, затем клиент удалён и обе ручки=-1,0денег.
- 7827 custom race: первый дальний запрос победил, второй получил Occupied, как описано выше.

В проверенных новых player logs поиск Exception/Error/Assertion —0совпадений. GPU batch Screenshot PNG снова не создан; дополнительные попытки прекращены, P3 проверен native screenshot.

## Сохранённые проверки и пределы

AuthoritySession/PlayerWorld/CargoState/SessionState SHA не изменились относительно предыдущего кандидата. Сохранён независимый реальный300s Failure:Deadline/remaining0 на обоих процессах7794, а также40→Failure→restart и два restart до Shift3/Preparing/0 из раунда1. Deadline не повторялся без изменения кода. **Success:Deadline unit PASS (дополнение 01.11.2026):** QA просмотрел новый DeliveredQuotaSucceedsAtDeadlineWithoutEvacuation и независимо повторил весь MovingCoop.Tests: completed,16/16PASS,0failed/skipped,duration0.35s. Тест доставляет8коробок обычными application input/interact в collision-free world, проверяет320 и сохранение Active при квоте; уводит игрока вне зоны фургона, вызывает authority.Tick(300) и подтверждает Success:Deadline/Finished/remaining0, неизменность320 после следующего Tick. Ledger/позиции/результат fixture напрямую не присваивает. Это моделируемое authority-время, **runtime300s Success:Deadline не запускался**. Реальный300s Failure PASS сохранён. QA пересчитал SHA: production-файлы Fix1 неизменны; менялся только Editor test, новая сборка не нужна.

Физическое удержание клавиш/OS focus-loss, LAN/jitter/loss, Steam/интернет и исчерпывающий перебор всех тесных поворотов отдельно не проверялись. Результат PASS относится к localhost прототипу и описанному software-input/Unity runtime coverage. Editor освобождён после проверок; сцена clean, PlayMode stopped. Ревью не разрешает приписывать непроведённые проверки.

---
# Итерация 01 — независимый QA, раунд 1, 01.11.2026

**CHANGES_REQUESTED.** Кандидат `build_91a1c27bebe4`, `work/Builds/Iteration01/MovingCrew.exe`. Все SHA256 из it1-04-05-source-sha256.txt совпали. Код/сцена QA не менялись. Editor освобождён (последний статус ready/stopped). Пользовательское GUI-окно оставлено открытым; клавиатурой больше не вмешиваться.

## Дефекты

1. **P2: disconnect notice по-прежнему стирается.** `Assets/Game/Bootstrap/GameBootstrap.cs:98` каждый Active snapshot заменяет текст из `:72` на устаревший «Carrying is in the next batch». GameHud выводит Notice без защиты. Repro: host+client→Start→закрыть client→следующий snapshot (20Hz). Expected устойчивое сообщение о новом составе и возможности эвакуации; actual общий SHIFT ACTIVE. Это прежний P2, заявление dev-report об исправлении не соответствует frozen source. Реальный disconnect подтверждён JSONL; визуальное состояние после disconnect отдельно не снято.
2. **P2: гонка ручки решается скоростью подхода вместо порядка валидных команд.** `Assets/Game/Application/CargoSimulation.cs:17–20`. Interact проверяет только занятые Carriers, но не уже принятые approach. Repro на application contract: два свободных игрока с clear path к одной ручке, первый в1.1м, второй в0.1м; последовательно Interact(first), Interact(second), Step(dt=.02). Оба приняты в approach; второй достигает ручки первым и выигрывает. Expected первый валидный запрос резервирует ручку; второй получает Occupied. Actual первый позже тихо удаляется из approach (`ContainsKey` branch), без Occupied. Вывод по прямому исполнению ветвей исходника; отдельный двухпроцессный custom race не запускался. Имеющийся HandleRaceHasOneWinner проверяет лишь CargoState.Grab и этот путь не покрывает.
3. **P3: повреждённая кодировка HUD.** `Assets/Game/UnityAdapters/GameHud.cs:22,29,47,48` (включая составные строки), также ряд строк Bootstrap. В реальном финальном GUI вместо разделителя bullet и тире видны «вЂў»/«вЂ”». Repro открыть финальный exe: подзаголовок, HOST, WASD. Expected читаемые символы. UI остаётся понятным, но это видимый дефект сборки. Проверять результат в новом exe, не только текст source.

## Выполнено независимо

- CLI1.0.0-beta.15 / Unity6000.6.7f1 / Pipeline0.9.1-exp.1. `test run --mode editor --filter AuthorityRulesTests --async_tests true` → `test status completed`: **10/10 PASS**, 0failed/skipped. Ledger idempotence, partial bounds, domain handle race, deadline ordering, two restarts/old shift, averaging/leader rotation, dwell, terminal result. Эти unit tests не подменяют Unity collision/runtime.
- Реальные host/client на финальном exe, localhost, свои порты7790–7797, `-batchmode -nographics`. Команды по dev-report с `--moving-port`, раздельными абсолютными evidence/log. Логи `work/production/spike/independent-*`.
- **7790 sofa:** оба держали диван; поворот/проход дверей, Delivered200 и ещё3коробки → **320, Success: Evacuated** на обоих процессах. **7791 quota:** 8коробок → **320, Success: Evacuated**. Подтверждённые суммы не удваивались после сдачи. Детальный sweep всех тесных поворотов отдельно NOT_RUN.
- **7792 sofa-focus:** оба носильщика были привязаны, затем focus_release, обе ручки=-1, диван остановился околоz3.84,0денег. **7793 sofa-stale:** оба носильщика были привязаны, клиент прекратил input, обе ручки=-1, диван остановился околоz2.96,0денег. **7795 disconnect:** оба привязаны, client вышел, host остался один, обе ручки=-1,0денег.
- **7796 existing box-host script:**40 после доставки, Failure:Evacuated, затем Shift2/Preparing/total0 и исходные грузы. **7797 custom ordinary-command script:** evacuate1s→restart2s→start3s→evacuate4s→restart5s; итог Shift3/Preparing/0. Старый ShiftId проверен unit test; сетевую инъекцию старого packet отдельно не выполнял.
- В независимых player logs на момент отчёта нет Exception/Error/Assertion. Snapshot reliability и sender binding просмотрены в исходнике; LAN/jitter/loss не измерялись.

## GUI/U02 и оставшиеся критерии

Через computer-use QA лично запустил финальный exe, нажал Host и Start после Ready второго настоящего процесса. Проверены HUD сумма/таймер/оба участника, цвета игроков и изометрический вид. Короткие sky.press_key W taps не дали надёжно измеримого WASD-маршрута; нельзя считать это провалом игры или PASS управления. Далее tool сообщил user input. Следующий наблюдаемый кадр: синий игрок уже в Room02, Holding коробку, leader0, камера переместилась за ним, передняя стена локально скрыта. Эти перемещения/E выполнены пользователем, а не QA. Отдельная ручная диагональ, её равная скорость, восстановление стены после ухода и углы удерживаемого дивана **NOT_RUN независимо**. Пользовательский прогон не объявляется автоматическим независимым тестом.

Реальный deadline независимой пары7794 завершён: host t310.889 — Failure: Deadline, remaining0, total0; client имеет 14 samples того же результата. Deadline runtime **PASS**. Unit deadline ordering PASS отдельно. Оба автоматических процесса завершились по driver deadline.

До финального PASS нужны исправления выше и целевые перепроверки; U02 и незакрытые проверки честно остаются ограничением. Исходники возвращаются разработчику только для конкретных fixes. Новый build писать в отдельную папку, чтобы не затронуть открытую пользовательскую сборку.




