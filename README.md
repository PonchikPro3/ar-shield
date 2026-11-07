# Sunny Kitty — Unity WebXR (задание 6)

**Приложение GitHub Pages:** https://ponchikpro3.github.io/ar-shield/

Прежняя страница MindAR заменена сценой Unity с WebXR Export от De-Panther. Используется **`assets/sunny_kitty.glb`**, скопированный из ассетов проекта без изменений. Модель видна сразу после загрузки, по центру, на расстоянии 1,2 м перед активной камерой. В AR она следует за направлением взгляда (viewer-space), без поиска поверхности и касаний для размещения.

Интерфейс: кнопка «Открыть AR» на совместимом устройстве; прогресс во время загрузки; короткое сообщение о 3D-режиме, если immersive-ar недоступен; маленькая обязательная атрибуция модели. Вход в AR требует действия пользователя по правилам браузера.

## Реализация

- Unity **6000.3.1f1**, Built-in Render Pipeline, WebGL 2, IL2CPP, Linear Color Space.
- `UnityProject/Assets/Scenes/ViewerSpace.unity` — единственная сцена сборки.
- В сцене: `WebXRManager`, `WebXRCameraSet`, `WebXRCamera`, `TrackedPoseDriver`, основная камера, камеры AR/VR и два `WebXRController` без визуальных моделей контроллеров.
- Раскладка камер повторяет BiRP-вариант WebXRCameraSet из De-Panther. WebXRLoader включён через XR Plug-in Management.
- Для BiRP включён `DisableXRDisplaySubsystem`. Reference space AR — `viewer`; hit-test, hand tracking, HDR, тени и постэффекты отключены.
- `Assets/Scripts/ViewerSpaceObject.cs` обновляет положение перед активной камерой; поддерживает смену режима и усреднение глаз для двух видов. Генератор сцены проверяет центр viewport на перемещённой и повёрнутой камере.
- `Assets/Prefabs/SunnyKitty.prefab` — статическая модель с исходной текстурой. 2 114 вершин, 3 528 треугольников, упрощение не применялось. UV и нормали перенесены из GLB; материал Unlit и двусторонний, как в оригинале.
- `Vendor/webxr` и `Vendor/webxr-interactions` — официальные пакеты De-Panther **0.25.0**, commit `1f7a06c57867f300dabfa2bbbee85a6b03fa0005`. Лицензия: `Vendor/LICENSE`.
- `Assets/WebGLTemplates/WebXRTemplate/` — минимальная адаптация официального **WebXRFullView2020**, с сохранением инициализации Unity и событий WebXR. В текущем репозитории нет шаблона с буквальным именем WebXRTemplate; это локальное имя адаптации.

Все пути `Assets/...` выше находятся внутри `UnityProject`.

## GitHub Pages

Сайт настроен на **Deploy from a branch → main → /(root)**. Корневой `index.html` перенаправляет в `WebGL/`, где находится готовая сборка. `.nojekyll` отключает обработку Jekyll. Зависимости Unity и исходные ассеты не запрашиваются приложением во время запуска.

**Compression Format: Disabled; Decompression Fallback: OFF; Threads Support: OFF.** Поэтому обычные `.data`, `.js`, `.wasm` работают на GitHub Pages без специальных заголовков `Content-Encoding`, COOP/COEP и compression fallback. Все пути относительные и учитывают префикс `/ar-shield/`. GitHub Pages обеспечивает доверенный HTTPS: сертификат на телефоне устанавливать не нужно.

Готовые файлы находятся в `WebGL/Build/`. Точный размер, контрольные суммы и результаты проверки — `reports/unity-build.json`, `reports/verification.json` и после публикации `reports/pages-verification.json`. `unity-build.json.bytes` — размер из Unity BuildReport с промежуточными результатами; для размера скачиваемых файлов используйте сумму в `verification.json.buildBytes`.

Примечание: HTTP-сжатие самим CDN может отличаться от размера файлов. Первое открытие требует загрузки WebAssembly; повторное открытие может использовать браузерный кэш GitHub Pages.

## Проверка Android Chrome

1. Убедитесь, что телефон поддерживает ARCore и установлены Google Play Services for AR.
2. Откройте https://ponchikpro3.github.io/ar-shield/ в Chrome и дождитесь модели.
3. Нажмите «Открыть AR», разрешите использование камеры.
4. Проверьте, что котик появляется сразу поверх камеры, без поиска поверхности. При повороте и перемещении телефона он должен оставаться по центру поля зрения.
5. Завершите AR системной кнопкой браузера и войдите снова. Проверьте портретную/альбомную ориентацию и отказ в разрешении камеры.
6. Запишите модель телефона, версии Android/Chrome, результат и замечания в `reports/MOBILE-TEST.md`.

17.10.2026 пользователь подтвердил на физическом Android с Chrome: «Котик виден в AR по центру». Модель телефона, версии и дополнительные сценарии не сообщены; подробности в `reports/MOBILE-TEST.md`. Изменение размера desktop viewport проверяет только компоновку.

| Среда | Поведение |
|---|---|
| Android Chrome + ARCore + Google Play Services for AR | immersive-ar по HTTPS; требуется проверка на конкретном телефоне |
| Desktop Chromium с WebGL 2 | обычный 3D-просмотр; AR зависит от подключённого XR-устройства |
| Браузер без immersive-ar | 3D-просмотр, кнопка AR скрыта |
| Safari / другие мобильные браузеры | поддержка определяется через `isSessionSupported('immersive-ar')`; без физической проверки AR не обещается |

Требования: [Google WebXR](https://developers.google.com/ar/develop/webxr/requirements). Подключение WebXR и BiRP: [De-Panther Getting Started](https://github.com/De-Panther/unity-webxr-export/blob/master/Documentation/Getting-Started.md). Публикация: [GitHub Pages](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site).

## Пересборка

Откройте `UnityProject` в Unity 6000.3.1f1 с модулем WebGL. Меню **Shield → Build WebGL** пересоздаёт сцену, сохраняет настройки и экспортирует `WebGL/`. Для ручных изменений сцены используйте обычный Build: генератор при следующем запуске перезапишет сцену.

```powershell
node tools/convert-kitty.cjs
unity build .\UnityProject --editor-version 6000.3.1f1 --target WebGL --execute-method Shield.Editor.WebARBuild.Build
```

Конвертер использует только стандартную библиотеку Node.js; декодирование GLB происходит до сборки. Изменение размера и центрирование выполняются в сцене. Источник, SHA-256 и лицензия записаны в `UnityProject/Assets/Models/conversion.json`.

Для обновления GitHub Pages загрузите изменения сборки вместе с `index.html` и `.nojekyll` в `main`; дождитесь завершения `pages build and deployment`.

## Дополнительный локальный HTTPS

Для выполнения локальной части задания сохранён `start-server.bat` / `server.ps1`. Нужны Node.js и Python с `cryptography`; на этом компьютере используется встроенный Python Codex. На другом компьютере: `python -m pip install cryptography`.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\server.ps1
node tools/verify-build.cjs
```

- HTTPS: https://localhost:8443/.
- Desktop preview без установки сертификата: http://localhost:8080/ (loopback — secure context).
- Для телефона в той же Wi-Fi-сети: `https://IP-КОМПЬЮТЕРА:8443/`; сначала установите `.certs/rootCA.cer` как доверенный CA. Для GitHub Pages этот шаг не нужен.

Сертификат содержит IP на момент создания и действует 30 дней. При смене адреса: `server.ps1 -RegenerateCertificate`. Сервер автоматически не меняет настройки доверия Windows/Android. `.certs/` исключена из Git; сервер выдаёт только публичный сертификат по `/ca.cer`, приватные файлы недоступны. Приватный ключ CA не сохраняется.

Проверка опубликованных файлов:

```powershell
node tools/verify-build.cjs https://ponchikpro3.github.io/ar-shield/WebGL/
```

## Модель и лицензия

**Sunny Kitty** — [Ponter](https://sketchfab.com/ponter), [исходная модель](https://sketchfab.com/3d-models/sunny-kitty-bdd7369e67f74ff89dc74273879db395), [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Атрибуция взята из метаданных GLB. Подробности преобразования — `assets/SUNNY-KITTY-LICENSE.md`; копия включена в сайт.
