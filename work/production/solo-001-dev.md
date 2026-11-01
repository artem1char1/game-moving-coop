# SOLO-001: developer evidence

Scope: Host -> Start Shift with one ready mover; connected pair still requires both ready. No late join during Active; sofa still requires two; quota remains 320. Development auto-driver waits for two by default, explicit `--moving-solo true` allows solo automation. HUD/notice and player instructions updated.

Changed files: Assets/Game/Domain/SessionState.cs; Bootstrap/GameBootstrap.cs; UnityAdapters/{DevelopmentScenario,GameHud,SnapshotNotice}.cs; Tests/Editor/AuthorityRulesTests.cs (under game-moving-coop/); outputs/iteration-01-play.md; this report. No packages, scenes or existing five renderer/cloud settings edited.

Unity CLI 1.0.0-beta.15, projectPath C:/Users/Artem/Desktop/Projects/game-moving-coop/game-moving-coop. Editor remained open, Play Mode stopped via `command editor.playmode stop`. Asset refresh/explicit CompilationPipeline.RequestScriptCompilation requested via script eval because no registered refresh/compile operation was found. First test runner result was stale 16 tests and is not new coverage. After compilation: `command test run --mode editor --project-path <projectPath> --json`: 21/21 PASS, zero skipped/failed (solo-001-tests.json). New tests cover empty, ready solo, unready/ready pair, client Start rejection, solo evacuation/restart/new Start. Existing sofa-two and ledger tests pass.

Build: `command build run --target StandaloneWindows64 --outputPath C:/Users/Artem/Desktop/Projects/game-moving-coop/work/Builds/Iteration01-Solo/MovingCrew.exe --confirm true --project-path <projectPath> --json`; poll `command build status`. Final report solo-001-build-status.json. Rebuilt after final waiting-notice adjustment.

Runtime commands (all Windows standalone, hidden background batchmode/nographics; no state injection):
- MovingCrew.exe -batchmode -nographics --moving-role host --moving-scenario quota --moving-solo true --moving-port 7891 --moving-seconds 280 --moving-evidence <repo>/work/production/solo-001-quota.jsonl -logFile <repo>/work/production/solo-001-player.log
- Two processes with --moving-role host/client --moving-scenario movement --moving-port 7892 --moving-seconds 18/16 --moving-evidence <repo>/work/production/solo-001-coop-host/client.jsonl -logFile <repo>/work/production/solo-001-coop-host/client.log
- Host --moving-script <repo>/work/production/solo-001-restart-script.json --moving-solo true --moving-port 7893 --moving-seconds 9; evidence solo-001-restart.jsonl.

Observed: solo quota delivered eight boxes, total320, `Success: Evacuated` at85.40s with one player. Process stopped after success evidence to permit final rebuild. Coop host had exactly one started event with two players, client has34 active two-player samples. Solo script evacuated shift1, restarted shift2, started and evacuated again; both below-quota results `Failure: Evacuated` as expected. These runtime checks preceded final notice-only adjustment; final executable smoke evidence separately recorded when available. Gameplay/authority code unchanged between them.

Limitations: headless automated input verifies actual transport/game loop/collisions, not human usability or manual GUI clicking. LAN/Steam not exercised. Historical console errors at09:40 predate this task; no compile errors. Independent QA pending. No commit/push.
Final build Succeeded: 0 warnings / 0 errors (build_6bceee07b140).

Final executable smoke on port7894 PASS: solo Start, evacuation, Restart to shift2, Start and evacuation again (solo-001-final-smoke.jsonl). Code/build frozen for QA; developer releases Editor write ownership. Main Editor left open, stopped. Gameplay/transport acceptance evidence above plus21/21Editor tests.
