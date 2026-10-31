# Unity и инструменты агента

Подключение проверено 31 октября 2026. Unity CLI установлен, соединение с открытым Unity Editor через Pipeline подтверждено.

## Основной вариант для дальнейшей работы

У Unity есть официальный Unity CLI и официальный unity-agent-plugin для Codex. CLI умеет работать с редакторами, проектами, сборками и тестами; через Unity Pipeline — обращаться к открытому Editor. Плагин содержит навыки, которые используют CLI для редакторских задач.

В текущем проекте уже был установлен `com.unity.pipeline: 0.9.1-exp.1`; переустановка и обновление пакета не потребовались. CLI обнаружил один главный Editor нужного проекта и доступный Pipeline на `127.0.0.1:7800`.

Ранее прямой запуск встроенного в MSIX Unity Hub файла возвращал «Отказано в доступе». Вместо изменения прав установлен самостоятельный CLI официальным установщиком `https://unity.com/install.ps1`, с проверкой SHA256 загрузки. Версия — `1.0.0-beta.15`, путь — `C:/Users/Artem/AppData/Local/Unity/bin/unity.exe`. Каталог добавлен в пользовательский PATH; уже открытым процессам может потребоваться перезапуск, поэтому агент может использовать полный путь.

Проверка локального соединения прошла без входа CLI в аккаунт Unity. `auth status` сообщает отсутствие сессии; это не мешало использованным локальным командам. Облачные команды и операции, требующие авторизации, не проверялись. Плагин unity-agent-plugin для Codex этим шагом не устанавливался.

## Проверенные команды PowerShell

Из корня репозитория:

```powershell
$taskUnity = 'C:/Users/Artem/AppData/Local/Unity/bin/unity.exe'
$taskProject = 'C:/Users/Artem/Desktop/Projects/game-moving-coop/game-moving-coop'
& $taskUnity --version
& $taskUnity pipeline editors --json
& $taskUnity command --project-path $taskProject --limit 8 --json
& $taskUnity command editor status --project-path $taskProject --json
& $taskUnity command scene list_open --project-path $taskProject --json
```

В beta.15 команда обнаружения — `pipeline editors`; пример `pipeline list` на странице документации вернул ошибку неизвестной команды. Для синтаксиса установленной версии следует проверять `--help`.

Фактический результат: Editor `6000.6.7f1`, status `ready`, compiling `false`, Play Mode `stopped`; одна загруженная активная `SampleScene`, isDirty `false`, три корневых объекта. Список команд Pipeline доступен. Это проверка связи и чтения состояния, не запуск игры, сборки или тестов. Сцены, скрипты, manifest и lock-файл не изменялись.

Соединение обнаруживается при каждом вызове CLI, постоянный отдельный процесс агента не требуется. Unity Editor должен оставаться открытым; адрес/порт после перезапуска могут измениться, поэтому предпочтительно обнаружение по project-path.

Официальная документация:

- CLI: https://docs.unity.com/en-us/unity-cli/use-unity-cli
- Pipeline и подключение к открытому Editor: https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package
- Плагин Unity для агентов, включая Codex: https://github.com/Unity-Technologies/unity-agent-plugin

## Альтернатива

CoplayDev/unity-mcp — сторонний открытый MCP-мост к Unity Editor: сцены, объекты, ассеты, скрипты и тесты. Это отдельный проект, не официальный продукт Unity. В текущем чате такой мост не подключён. Не нужен одновременно с CLI по умолчанию: сначала стоит проверить официальный путь и добавлять MCP только под недостающую возможность.

Источник: https://github.com/CoplayDev/unity-mcp

## Что уже работает без интеграции Editor

Агент может читать и изменять файлы рабочего Git-клона; GitHub Desktop видит эти изменения в той же папке. Unity отслеживает изменения своих файлов в проекте. Чтение состояния открытого Editor через CLI подтверждено; команды изменения сцен, Play Mode, сборок и тестов следует выполнять только в рамках соответствующего задания.

На текущем архитектурном этапе редакторские команды не нужны: новые игровые скрипты и ассеты не создаются.
