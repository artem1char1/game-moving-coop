# ARCH-001 — независимое QA архитектуры

Вердикт: **PASS** в границах документационной задачи A1–A7. Обязательных замечаний не найдено. Это приёмка полноты и согласованности предложения, не доказательство работоспособности игры, физики или сети. PM может зафиксировать приёмку ARCH-001; реализация автоматически не разрешается.

## Проверяемое состояние

Канонический клон: `C:/Users/Artem/Desktop/Projects/game-moving-coop`; Unity-корень: `game-moving-coop/`; база сравнения: `84b61f3`.

| Документ | Подтверждённый SHA256 |
| --- | --- |
| docs/architecture/overview.md | CDB0BBA5CEA5A3C8069887FAEA19C1405B43C5D775B228075DC340B9A942AF4A |
| docs/architecture/decisions.md | 02E2DA903B2D0A9640D8F3F1BBFB0FB6BEACCFCDA33CE7B3C9DF197ABA2F2B27 |

Хеши независимо вычислены `Get-FileHash -Algorithm SHA256` и совпали со стабильным handoff координатора. Разработчик не редактировал документы во время QA. Единственный файл, созданный QA, — этот отчёт.

## Сопоставление критериев

| Критерий | Результат и доказательства |
| --- | --- |
| A1 — scope | PASS. Разработчиком переданы два назначенных Markdown-документа. `git diff --exit-code 84b61f3 -- game-moving-coop` дал пустой вывод и exit 0. `git ls-files --others --exclude-standard -- game-moving-coop` вернул 0 файлов. Изменения PM и координатора вне Unity-корня не приписываются разработчику. Код, сцены, пакеты и asmdef не создавались/менялись в этом этапе. |
| A2 — inventory | PASS. Независимо прочитаны ProjectVersion.txt, manifest.json, EditorBuildSettings.asset; сопоставлены packages-lock, SampleScene, Graphics/Quality/Time/Project settings, RPAsset meta GUID и InputSystem_Actions. Unity 6000.6.7f1 / URP 17.6.0 / Input System 1.20.1 / uGUI 2.6.0 / Pipeline 0.9.1-exp.1 подтверждены. Сцена SampleScene, perspective-camera, только два TutorialInfo C# файла, fixed step 0.02 и activeInputHandler 1 подтверждены. Найденные зависимости отделены от решений дизайна. |
| A3 — модули и lifecycle | PASS. overview §§2–3,6: направленный ациклический граф, Domain без Unity/transport, Application с узкими портами, адаптеры и ручной Bootstrap. Runtime-владение и read-only результаты явно отделены от конфигурации ScriptableObject и MonoBehaviour. Закрытие ingress, отмена callbacks, снятие связей, отписка и освобождение scopes согласованы. Глобальный service locator и преждевременный framework исключены. |
| A4 — authority и переходы | PASS. overview §§3–6: один authority, сериализация команд, привязка PlayerId к соединению, гонки ручек и disconnect; ledger по ShiftId+CargoId и ограниченное окно OperationId; атомарный in-memory commit Delivered/release/total/result; идемпотентная проекция мира; новый ShiftId и очистка при restart. Ошибка проекции не замалчивается. Сетевой lockstep и долговечность при падении не обещаются. |
| A5 — проверки и расширения | PASS. overview §7: конкретные domain/application/Unity/двухклиентские сценарии, включая гонки сдачи и завершения, старые команды, disconnect, restart и ошибку world projection. Описано пять расширений с изменяемыми границами и допустимой переработкой; нет требования интерфейса на каждый класс. |
| A6 — решения | PASS. decisions ADR-001–013 имеют различимые статусы, контекст/варианты, аргументы и последствия. Unity/URP/C# и границы задачи подтверждены; архитектурные выборы предложены; сеть, физическая модель и правила сдачи остаются TBD. Вопросы до соответствующего этапа перечислены отдельно. |
| A7 — независимое QA | PASS. Документы полностью прочитаны, сопоставлены между собой и с фактической базой; существенных противоречий, требующих исправления, не найдено. Повторная проверка дефектов не требовалась, поскольку обязательных замечаний нет. |

## Выполненные проверки

- Read-only PowerShell/Get-Content/rg/git: чтение требований и документов, inventory Unity, JSON-разбор input actions, перечисление отслеживаемых C# и неотслеживаемых Unity-файлов.
- Проверка хешей двух документов и неизменности отслеживаемого Unity-корня относительно базы.
- Статическое ревью владения состоянием, направления зависимостей, очереди команд, атомарности зачёта, границ restart/disconnect, ошибок проекции и объёма расширений.
- Открыты три первичные Unity Docs, на которые ссылаются документы. Общие положения о ScriptableObject, ограничениях порядка callbacks и влиянии переменного физического шага согласованы с изложением. Использование документации 6000.0 для общих свойств явно оговорено; API будущей реализации 6000.6 предстоит проверять отдельно.
- Координаторские outputs/change-audit.md и outputs/unity-tools.md прочитаны только на согласованность: наличие Pipeline не выдано за подключение Editor; рабочий клон и отсутствие реализации не противоречат архитектуре. Их полный внешний аудит не входит в A1–A7.

## Что не проверялось

Unity Editor, Play Mode, сборки, исполняемые тесты и два клиента не запускались. Файлы Unity и пакеты не изменялись. Транспорт, численность команды, физическая модель, reconnect и правила сдачи не выбраны этим QA. Наличие согласованной архитектуры не заменяет будущие технические эксперименты и игровые проверки.

Первичные источники, использованные при статической сверке:

- [ScriptableObject](https://docs.unity.com/en-us/engine/6000.0/manual/scripting/get-started/fundamental-unity-types/class-scriptable-object)
- [Event function execution order](https://docs.unity.com/en-us/engine/6000.0/manual/scripting/managing-update-order/execution-order)
- [Physics.Simulate](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/physics/simulate)
