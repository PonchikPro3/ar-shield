# Результат задания 6 — 17.10.2026

Сайт: https://ponchikpro3.github.io/ar-shield/

- Unity 6000.3.1f1, WebXR Export / Interactions 0.25.0.
- Модель `assets/sunny_kitty.glb`, SHA-256 `78643a0c6fbb624001ba7c06b1ab3320779afba82bcdedc6b589071581eddbfb`.
- Сцена `ViewerSpace.unity`: WebXRManager, WebXRCameraSet / WebXRCamera, TrackedPoseDriver, два WebXRController.
- Объект виден при запуске и следует за активной камерой на расстоянии 1,2 м. Генератор проверяет положение в центре viewport при перемещении и повороте камеры.
- WebGL: compression Disabled, decompression fallback OFF, threads OFF. Сборка Unity завершена без ошибок и предупреждений.
- Размер файлов Build: **38 285 876 байт (36,51 МиБ)**. Наибольший файл — WebGL.wasm: 31 574 538 байт. Это размер файлов до возможного транспортного сжатия CDN.
- Локальный HTTPS-сервер запущен; TLS проверен с доверенным CA, MIME WASM корректен, отданные файлы совпадают со сборкой.
- GitHub Pages: публикация из main, корень /, HTTPS enforced. Деплой исходного изменения: https://github.com/PonchikPro3/ar-shield/actions/runs/39010200206 (успешен).
- Обычный браузер: модель и текстура видны, ошибок/предупреждений консоли не обнаружено. Портретная компоновка 390×844 проверена; это не физический мобильный тест.
- На Android нужен Chrome с поддержкой immersive-ar, ARCore и Google Play Services for AR. На других устройствах приложение проверяет поддержку; без AR остаётся 3D-просмотр.
- Физическая проверка Android Chrome: пользователь подтвердил «Котик виден в AR по центру». Версии устройства/браузера и дополнительные сценарии не сообщены; см. MOBILE-TEST.md.

Подробности: `unity-build.json`, `verification.json`, `browser-check.json`; проверка опубликованных файлов после завершения — `pages-verification.json`.
