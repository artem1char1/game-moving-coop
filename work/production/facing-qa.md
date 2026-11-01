# FACING-001 — независимое QA

Вердикт: PASS в проверенном объёме; дефектов P0–P2 не выявлено.

Статическое ревью: кратчайший угол; box вращается вокруг носильщика, sofa вокруг центра. TryPose/fallback сохраняют проверки препятствий. Q/R приоритетнее WASD; idle сохраняет угол. ModelRoot отделяет будущую модель от позиции.

Актуальная проверка после принудительного обновления/компиляции:
- Build build_e7aa049d44ac: completed Succeeded, 0 warnings/errors; facing-rebuild-status.json. SHA256 актуальных игровых DLL: facing-recheck-sha.json. Сравнение со старой сборкой невозможно: прежние SHA не сохранялись, поэтому runtime повторён.
- Два реальных процесса новой сборки, штатный transport/authority, сценарий sofa: обе ручки, разворот 0→90°, перенос через двери, доставка200. В 15 кадрах одинаковой revision — 0 несовпадений позиции/facing: facing-recheck-network-summary.json и facing-recheck-{host,client}.jsonl.
- Повтор facing-box-script.json на актуальном exe: WASD box5 0→86.4°, idle, ручной поворот до88.2° у препятствия, WASD →−90°. Facing=angle+90°, связь ручки сохранена. У задней стены player z7.94, box z8.59, грань z8.89<wall8.91. Evidence: facing-recheck-box.jsonl/log. Исключений в трёх логах не найдено.
- Ранее подтверждён completed 24/24 EditMode запуск автора: facing-test-status.json; без повторения неизменённых тестов.

NOT_RUN: визуальная оценка маркера/перекрытий и физические клавиши; скрытое окно первого запуска не создало screenshot. Humanoid/анимации вне scope. Исходники QA не менял; собственные процессы завершились.
