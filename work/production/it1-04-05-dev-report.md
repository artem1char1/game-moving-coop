# Latest candidate: Fix1 (01.11.2026)

Build `build_c3c2169c99d5`: completed/Succeeded,0errors/0warnings. New exe: `work/Builds/Iteration01-Fix1/MovingCrew.exe`; old running GUI/build untouched. Source frozen; new SHA list `work/production/it1-04-05-fix1-source-sha256.txt`. Editor free, scene Iteration01 loaded/clean/stopped.

QA fixes: Bootstrap passes previous phase to SnapshotNotice.Apply, so repeated Active snapshots retain disconnect notice; only the transition announces Active. Exact call site checked after writing. Approach dictionary now reserves (CargoId,HandleId) immediately; a later competing request returns Occupied, never gets attached. Cancel/focus/stale/disconnect/restart release reservations; dropping attached sofa also cancels pending approaches to it. Reservations do not count as carriers and suppress delivery dwell while pending. HUD labels now ASCII in UTF8; all C# scanned with strict UTF8 decoding and no remaining non-ASCII labels.

Regression checks: real CLI `test run --mode editor --filter MovingCoop.Tests --async_tests true` -> `test status`: **15/15 PASS**,0failed/skipped. Evidence copied to `work/production/spike/fix1-test-status.json`. Added far-first1.1m/near-second0.1m reservation race, cancel/reacquire/reset, persistent disconnect notice. Added UnityAdapterTests: an actual new InputSystem Keyboard receives QueueStateEvent W,D,W+D, then the production LocalInput.Read/ToWorld path checks camera-projected directions, normalized diagonal speed and focus zeroing. This is a reproducible software keyboard test, not a physical OS keyboard playtest. The actual authored Iteration01 scene is loaded; production camera follows its target, Physics queries hide occluding walls for the player and cargo point, restore walls when the target/point moves away, and leave colliders enabled. Initial camera-test coordinate was genuinely visible above the wall; corrected fixture geometry, production occlusion algorithm unchanged. Camera transforms/target restored; no scene save or asset changes. Scene list_open confirms isDirty=false.

Runtime retests are currently **NOT_RUN until terminal evidence**: independent ports7810 sofa,7811 quota,7812 focus,7813 stale,7814 disconnect,7815 race. Files `spike/fix1-<scenario>-<port>-<role>.jsonl` and .log. Added ordinary-input `--moving-scenario race`: host waits1.1m from handle until client waits0.1m away, requests first, client requests after seeing host approach start. GPU-rendered HUD capture requested on7816 at `spike/fix1-hud.png`. Existing real300s deadline PASS retained unchanged; deadline rules were not modified.

Editor ownership can pass to QA now. Source/build stable; developer only appends evidence/report until review findings.

---

# IT1-04/05 — разработчик, кандидат для независимого QA

Исходники заморожены 01.11.2026 после build_91a1c27bebe4. Editor свободен и передан QA. Изменять код до findings не буду. Точный перечень C#/asmdef и SHA256: `work/production/it1-04-05-source-sha256.txt`. Прочие URP/UnityConnect изменения не редактировал. Коммитов нет.

## Реализовано
- P2 OPEN: QA обнаружил, что исходная строка Active snapshot сохранилась. Первая замена не совпала; утверждение об исправлении было ошибочным. Будет исправлено общим раундом после QA findings. Текст disconnect обновлён, но перезаписывается Active snapshot.
- Domain CargoState: стабильные IDs, 9×40/диван200, ручки, лидер, terminal Delivered, ledger. Application CargoSimulation: подход к ближайшей свободной ручке <=1.2м через world, без snap; один/два носильщика, averaged input, leader rotation90°/с; общий rollback и разрешённые компоненты движения; release/focus/disconnect/stale500ms освобождают обе ручки дивана.
- AuthoritySession: deadline300с до очереди команд, полная сдача/0.5с, квота320, эвакуация, результат, новый ShiftId/restart, отбрасывание старых/повторных операций. Координаты/деньги не принимаются от клиента.
- Unity adapters: коробки/диван/ручки с подсветкой, E/Q/R/F, HUD сумма/таймер/цель/носильщики/лидер, локальный occlusion игрока и углов удерживаемого груза; капсулы, overlap проверки группы со стенами. Domain/Application вынесены в отдельные asmdef без Unity.
- Snapshots передаются ReliableFragmentedSequenced, буфер16KB: JSON с10грузами не помещается в обычный MTU. Частота20Hz, лимит2игрока; нагрузка/LAN потери ещё не измерены.
- DevelopmentCarryPlanner — только источник обычных намерений; читает опубликованный frame, идёт через двери/коридор, посылает обычные interact/evacuate. Нет телепорта/начисления денег/специальных authority bypass.

## Выполненные проверки
CLI1.0.0-beta.15, Editor6000.6.7f1, Pipeline0.9.1-exp.1, project-path `C:/Users/Artem/Desktop/Projects/game-moving-coop/game-moving-coop`.

`unity command script recompile` → `script recompile_status`: completed, failed=false.
`unity command test run --mode editor --filter AuthorityRulesTests --async_tests true` → `test status`:10/10 PASS. Проверены ledger duplicate, partial bounds, гонка ручки, deadline перед эвакуацией, два restart/старый ShiftId, release dwell, averaged/встречный/одиночный ввод дивана, лидер вращения, полсекунды сдачи, terminal result/повтор эвакуации. Первая версия теста двух restart имела неверную sequence второй команды; исправлен тест, повторные6/9/10 прогоны PASS.
`unity command build run --target StandaloneWindows64 --outputPath ../work/Builds/Iteration01/MovingCrew.exe --confirm true` → `build status`: build_91a1c27bebe4 completed/Succeeded,0errors/0warnings.

Два реальных headless процесса:
- `spike/carry2-host.jsonl` / client: коробка, поворот90°, обе двери, отпускание,40, Failure: Evacuated, restart→Shift2/0/исходные спавны. Первый carry маршрут подтвердил блокировку группы боковой ручкой у двери.
- `spike/sofa2-host.jsonl` / client: диван двумя носильщиками,90°, проход обеих дверей, drop→Delivered200, ещё3коробки,320, Success: Evacuated (~48с). Первый sofa маршрут на280 обнаружил небезопасный waypoint у заднего носильщика; исправлен маршрут driver на z1.5, collision rules сохранены.

## Финальные запущенные проверки (до конечного evidence — NOT_RUN)
На финальном build запущены независимые пары: quota порт7780/130с; sofa-focus7781/14с; sofa-stale7782/14с; sofa disconnect7783 host13с/client7с; настоящий deadline300с на movement7784/308с. Логи: `work/production/spike/final-<scenario>-<port>-<role>.jsonl` и `.log`. Итоги будут добавлены после завершения.

## Воспроизведение
EXE: `work/Builds/Iteration01/MovingCrew.exe`.
Для каждой роли отдельно:
`MovingCrew.exe -batchmode -nographics --moving-role host --moving-scenario sofa --moving-port 7790 --moving-seconds 80 --moving-evidence <absolute-host.jsonl> -logFile <absolute-host.log>`
Второй процесс: та же команда с `--moving-role client`, отдельные evidence/log, тот же port. `--moving-scenario quota` =8коробок; `sofa` =диван+3коробки; `sofa-focus`/`sofa-stale` прерывают участие клиента при переноске. Disconnect: sofa обоим, клиенту7с, хосту13с. `movement` обоим308с проверяет настоящий deadline. По умолчанию port7777. Для ручного UI убрать batchmode/nographics/scenario и выбрать Host/Join; testport должен совпадать у обоих.
Есть произвольный input script: `--moving-script <absolute.json>`; format `{"steps":[{"at":1,"x":0,"z":1,"turn":0,"command":"interact"}]}`. at — секунды Active, x/z — world-intent; команды проходят обычный transport. Готовые сценарии в work/scenarios.

## Не объявлено пройденным
Ручное WASD/диагональ/камера/occlusion U02 и визуальная проверка HUD, LAN/jitter, полный независимый QA. Голая сборка не считается runtime-тестом. Нет результатов о Steam/интернет-соединениях. Таймер/сдача основаны на realtime authority, floor y у назначенных bounds фиксирован0..0.9, зона y0..2; четыре горизонтальных угла эквивалентны восьми 3D углам для этих назначенных bounds. Вращение проверяется малым sweep шагом/overlap; остаётся необходимым независимый тест тесных поворотов. IT1-04/05 не помечены done разработчиком.

## Завершившиеся финальные interruption прогоны
Три пары завершены: sofa-focus7781, sofa-stale7782, sofa disconnect7783. Во всех наблюдались два носильщика, затем обе ручки свободны; груз оставался в одной подтверждённой позе после release до завершения. Машиночитаемые выборки: `spike/final-interruptions-summary.json`; соответствующие host/client JSONL и player logs сохранены. Это проверка authority focus-команды/потери свежих input и настоящего disconnect; физическое снятие фокуса окна требует ручного GUI теста QA.

## Завершившийся финальный маршрут8коробок
`spike/final-quota-7780-host.jsonl` / client завершены: оба подтверждают320, phase2, Success: Evacuated. Только8коробок Delivered, диван не сдавался. Вторая квотная стратегия подтверждена обычными командами и authority; исходники заморожены.

## Завершившийся настоящий deadline300с
`spike/final-movement-7784-host.jsonl` / client: оба дошли до phase2, remaining0, Failure: Deadline с суммой0 после реальных300сек Active. Процессы завершены штатно по308сек harness. Во всех10 final player logs поиск Exception/Error/Assertion дал0 совпадений. Этот runtime-прогон не подменял время, координаты или правила deadline.

## Fix1 завершившиеся runtime retests
-7810 sofa: host/client320, Success: Evacuated. 7811 quota: host/client320, Success: Evacuated.
-7815 race: реально наблюдалось Occupied, затем коробка5 закреплена только заhost0, клиент1 свободен; дальний первый запрос победил близкий второй.
-7814 disconnect: после отключения клиента12 Active snapshots сохранили одно сообщение Client disconnected...evacuate...restart; обе ручки свободны, поза дивана неизменна.
-7812 focus: оба носильщика наблюдались, затем обе ручки свободны и CargoZ неизменен.
-7813 stale НЕ засчитан: поздний handshake (~12s) оставил только2s Active до harness exit; оба носильщика не наблюдались. Запущен отдельный повтор7817/25s после освобождения нагрузки. Это ограничение короткого тестового запуска, не PASS.
-Выборки: spike/fix1-runtime-summary.json, исходные JSONL host/client и logs.
-GPU screenshot попытки из batchmode и скрытого standalone завершились Failed to capture screen shot; PNG не создан. Статический UTF8/ASCII scan PASS, но фактическая визуальная P3 перепроверка новогоGUI остаётся дляQA. Существующий пользовательскийGUI не закрывался и клавиатурой не управлялся.

## Fix1 stale повтор и окончательный handoff
7817 sofa-stale/25s завершён: в host/client наблюдались обе занятые ручки, после потери свежего input обе свободны, CargoZ строго постоянен2.9800038 до конца. Player logs всех Fix1 игровых пар (кроме отдельной неудачной GPU capture попытки) не содержат Exception/Error/Assertion. Все автоматические процессы разработчика завершены. Исходники/билд остаются frozen; Editor остаётся уQA. Для закрытия P3 требуется фактический визуальный просмотр новогоGUI; автоматический InputSystem/scene U02 покрыт15/15PASS, физические OS-клавиши не выдаются за проверенные.

## Точечное покрытие Success: Deadline (после QA Fix1 PASS)
Изменён только Editor test AuthorityRulesTests.cs: DeliveredQuotaSucceedsAtDeadlineWithoutEvacuation. Тест ведёт настоящую AuthoritySession обычными input/interact командами в collision-free fake world: игрок последовательно подходит к8коробкам, захватывает, переносит к зоне, отпускает; authority подтверждает каждую сдачу после0.51с и набирает320. Fixture не присваивает ledger, total, позы грузов или result. До deadline смена остаётся Active; игрок уходит из зоны; Tick300 завершает Success: Deadline/320/remaining0, Tick301 сохраняет итог. Время здесь управляемое unit-test, а не ещё один реальный300s runtime прогон.
CLI script recompile_status completed/failed=false; test run --mode editor --filter MovingCoop.Tests --async_tests true -> test status completed:16/16PASS,0failed/skipped. Конечный ответ сохранён в spike/deadline-success-test-status.json.
SHA проверка относительно frozen Fix1 выявила ровно одно изменение — Editor test. Все runtime C#/asmdef совпали; проверка и hash существующего exe сохранены в spike/deadline-success-unchanged-runtime.json. Runtime/сцены/driver не менялись; build не запускался, текущий Fix1 exe остаётся поставкой. Editor передан QA для проверки теста/evidence без повторения5минутного таймера.
