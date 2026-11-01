# SOLO-001 — независимое QA

Вердикт: PASS. Значимых дефектов в назначенном diff не найдено.

Проверен стабильный diff шести C# файлов: SessionState, GameBootstrap, DevelopmentScenario, GameHud, SnapshotNotice, AuthorityRulesTests; также инструкция outputs/iteration-01-play.md. CanStart разрешает одного готового игрока, Add сохраняет максимум два; пара требует готовности всех, Start доступен только host. Late join блокируется CanJoin; CargoSimulation по-прежнему двигает диван только при двух носильщиках. HUD использует общий canStart без требования второго игрока.

Editor: Unity CLI 1.0.0-beta.15; projectPath C:/Users/Artem/Desktop/Projects/game-moving-coop/game-moving-coop. Независимый `command editor status --project-path <project> --json`: ready, stopped, compiling=false. `command test run --mode editor --project-path <project> --json`: 21/21 PASS, 0 skipped/failed, включая новые solo/ready/client/restart и существующие sofa/ledger. Исходники и сцена QA не изменялись.

Независимые runtime проверки финальной work/Builds/Iteration01-Solo/MovingCrew.exe, headless, настоящие процессы/transport без инъекции состояния:

- Solo: host + --moving-script work/production/solo-001-restart-script.json --moving-solo true --moving-port 7901 --moving-seconds 9. Два started, shifts 1→2; две эвакуации ниже квоты дают Failure: Evacuated. Evidence solo-001-qa-solo.jsonl/.log.
- Coop: host/client + --moving-scenario movement, port7902, 16/14с. Ровно один host started, 29/30 active samples с двумя игроками. Evidence solo-001-qa-host/client.jsonl/.log.
- Без --moving-solo, один host movement, port7904, 6с: 0 started, остаётся Preparing. Evidence solo-001-qa-default.jsonl/.log.
- Client к уже Active solo port7903 отклонён: «Session full or shift already active. Join in preparation.» Evidence solo-001-qa-late.jsonl/.log.
- Полная solo-квота на финальной сборке: host --moving-scenario quota --moving-solo true --moving-port 7903 --moving-seconds 100. На85.892с: один игрок, восемь доставленных коробок, total320, phase2, Success: Evacuated, диван не доставлен. Evidence solo-001-qa-quota.jsonl/.log. Это новый независимый прогон, атрибуция прежней сборки не требуется.

Все процессы запускались с -batchmode -nographics, --moving-evidence и -logFile в work/production; завершились по собственному таймеру. Поиск Exception|Error|Failed в QA player logs совпадений не дал. Пользовательские игровые окна не закрывались. Editor оставлен открытым и остановленным.

Ограничения: ручные клики/визуальная пригодность HUD, LAN/Steam не проверялись; существующие изменения renderer/cloud settings вне scope. 300с неизменённый таймер повторно не прогонялся. Финальный exe SHA256: 9806ABDE4A406ED68647E9505852C49F6AA72A372F4F084E2AA2821AFFEA57A2 (игровая логика находится также в сопутствующих сборке данных).
