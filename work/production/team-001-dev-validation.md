# TEAM-001 — техническая валидация разработчиком

Результат: конфигурация синтаксически корректна и согласована с назначенными ролями. Передано на независимое QA; это не самостоятельная приёмка задачи.

## Проверки и фактические результаты

- `python --version`: Python 3.14.4, exit code 0.
- Прочитаны `AGENTS.md`, три файла ролей, `work/production/backlog.md`, `outputs/team-orchestration.md` и `.codex/config.toml` через `Get-Content -LiteralPath`.
- Настоящий парсер стандартной библиотеки `tomllib` разобрал все четыре проектных TOML. Итоговый запуск приведён ниже, exit code 0.
- Ровно три роли; `name`, `description`, `developer_instructions` непустые; имена уникальны и совпадают с именами файлов.
- `agents.enabled = true`, `agents.max_concurrent_threads_per_session = 3`. Это три субагента, не три агента вместе с координатором.
- В проекте нет переопределений модели и reasoning effort. Пользовательский `~/.codex/config.toml` также разобран: `[agents]` не содержит проверяемых переопределений, корневые `model` и `model_reasoning_effort` присутствуют. Их значения и остальные настройки не выводились. Проект не фиксирует модель; фактическое разрешение настроек клиентом здесь не проверено.
- Каждой роли запрещено запускать вложенных субагентов. PM владеет задачами, разработчик реализацией, QA независимыми проверками. Цикл возврата дефектов и стабильное состояние на ревью закреплены в AGENTS.md и памятке.
- Движок не выбран; backlog отделяет подтверждённый brief от предложений. Ранний кооп и сетевые риски включены в предложенные этапы.
- Памятка `outputs/team-orchestration.md` согласована с файлами. Она явно отделяет сохранённую конфигурацию от фонового сервиса и автоматической загрузки клиентом.

Первый проверочный запуск завершился exit code 1: проверка запрета вложенных агентов сравнивала строку с учётом регистра и не приняла строчное «не запускай» у PM. Ошибка была в проверочной команде, не TOML. Проверка исправлена на `.casefold()` и повторена успешно; проектные файлы не менялись.

## Точная команда успешного разбора

Запуск в PowerShell из корня проекта:

```powershell
@'
import hashlib
import json
import pathlib
import tomllib

root = pathlib.Path.cwd()
config_path = root / '.codex/config.toml'
config = tomllib.loads(config_path.read_text(encoding='utf-8-sig'))
assert config['agents']['enabled'] is True
assert config['agents']['max_concurrent_threads_per_session'] == 3
assert 'default_subagent_model' not in config['agents']
assert 'default_subagent_reasoning_effort' not in config['agents']
print('CONFIG PASS: enabled=true; concurrent subagents=3; no model/effort override')
expected = {'project_manager', 'gamedev_developer', 'qa_reviewer'}
paths = sorted((root / '.codex/agents').glob('*.toml'))
assert len(paths) == 3
names = []
for path in paths:
    agent = tomllib.loads(path.read_text(encoding='utf-8-sig'))
    for field in ('name', 'description', 'developer_instructions'):
        assert isinstance(agent.get(field), str) and agent[field].strip(), (path, field)
    assert path.stem == agent['name']
    assert 'model' not in agent and 'model_reasoning_effort' not in agent
    assert 'не запускай субагентов' in agent['developer_instructions'].casefold()
    names.append(agent['name'])
    print('ROLE PASS:', agent['name'], '| fields nonempty | no model/effort override | nested spawning forbidden')
assert len(set(names)) == 3 and set(names) == expected
print('ALL FOUR TOML FILES PARSED; UNIQUE ROLE NAMES PASS')
user_config_path = pathlib.Path.home() / '.codex/config.toml'
if user_config_path.exists():
    user_config = tomllib.loads(user_config_path.read_text(encoding='utf-8-sig'))
    safe_keys = ('enabled', 'max_concurrent_threads_per_session', 'max_threads', 'default_subagent_model', 'default_subagent_reasoning_effort')
    user_agents = user_config.get('agents', {})
    print('USER CONFIG AGENT SETTINGS ONLY:', json.dumps({k:user_agents[k] for k in safe_keys if k in user_agents}, ensure_ascii=False))
    print('USER CONFIG HAS ROOT MODEL:', 'model' in user_config)
    print('USER CONFIG HAS ROOT EFFORT:', 'model_reasoning_effort' in user_config)
else:
    print('USER CONFIG: absent')
check_paths = [root/'AGENTS.md', config_path, *paths, root/'work/production/backlog.md', root/'outputs/team-orchestration.md']
for path in check_paths:
    print('SHA256', path.relative_to(root).as_posix(), hashlib.sha256(path.read_bytes()).hexdigest())
'@ | python -
```

Вывод: CONFIG PASS; ROLE PASS для каждой из трёх ролей; ALL FOUR TOML FILES PARSED; UNIQUE ROLE NAMES PASS; USER CONFIG AGENT SETTINGS ONLY: {}; USER CONFIG HAS ROOT MODEL: True; USER CONFIG HAS ROOT EFFORT: True. Далее выведены SHA256 из таблицы ниже.

## Стабильный набор для QA

| Файл | SHA256 |
| --- | --- |
| AGENTS.md | e5c167a9ad9e4f68a3abb564cf629b0fc38e46758f87d5f9956a113822ba8fb7 |
| .codex/config.toml | af00171d0ae3470aa41fb82e4165d91e5871e912d635e7545f9dc8f4a7752fc9 |
| .codex/agents/gamedev_developer.toml | 2b49381b2d5a14202232f8170ba17e50e584f4a4de57d73e69048d6197951983 |
| .codex/agents/project_manager.toml | b83fa9ad3eb4ca6343b5cf4095e54ccb739fed097d02d7c60ad3e7afda2d31dc |
| .codex/agents/qa_reviewer.toml | 49f5e68db772398b3276fbf26454c084f0b1e853935b34bfcc3a0f3688cc7610 |
| work/production/backlog.md | 5abd0b6980576462c4fab21b7397a56baa4af2974cbb5521bd34c16e9ae12e12 |
| outputs/team-orchestration.md | 673fe0b75346af5ba8e1743050545bb2509028278888b261f3a037fc37bed1b4 |

Также QA получает этот отчёт. Разработчик после передачи не изменяет проверяемые материалы.

## Границы доказательств

Изменён только этот отчёт. Игра, сборка и мультиплеер не запускались: их пока нет, и они вне TEAM-001. TOML-разбор не подтверждает загрузку пользовательских ролей клиентом или применение лимита новым сеансом. Проверка реального запуска трёх ролей, межагентных передач и окончательное закрытие PM должны опираться на сообщения координатора и агентов; разработчик независимо подтвердил только собственное получение роли и задания PM через координатора.
