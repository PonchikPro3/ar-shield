# AR-страница с 3D-моделью (model-viewer)

Демо-страница с размещением 3D-модели в дополненной реальности через
[`<model-viewer>`](https://modelviewer.dev).

**Опубликована на GitHub Pages:** <https://ponchikpro3.github.io/ar-shield/>
(HTTPS из коробки — AR на телефоне работает сразу: откройте ссылку в
Chrome на Android или Safari на iPhone).

## Структура проекта

```
ar-page/
├── index.html                  — страница с <model-viewer>
├── server.ps1                  — локальный HTTP-сервер (PowerShell, без зависимостей)
├── start-server.bat            — запуск сервера двойным кликом
├── README.md
└── assets/
    ├── shield.stl              — исходная модель (174 МБ, 3,5 млн треугольников)
    ├── shield.glb              — AR-версия в glTF (конвертация из STL: нормализация
    │                             масштаба, прореживание до ~450 тыс. полигонов,
    │                             стальной материал, Draco-сжатие)
    ├── shield.usdz             — та же модель в USDZ для Quick Look на iOS
    └── lightroom_14b.hdr       — HDRI-карта окружения для освещения модели
```

## Откуда взялись GLB и USDZ

`<model-viewer>` не читает STL напрямую — он работает только с glTF/GLB (веб +
Scene Viewer) и USDZ (Quick Look). Исходный `shield.stl` конвертирован в оба
формата headless-Blender'ом (см. `../tools/convert_shield.py`):

- масштаб нормализован до реальных метров (наибольшая сторона — 0,8 м,
  как у обычного щита; STL не хранит единицы измерения);
- меш прорежен с 3,5 млн до ~450 тыс. треугольников — 174 МБ STL не тянут
  мобильные AR-просмотрщики;
- назначен простой стальной PBR-материал (STL не хранит материалы);
- GLB дополнительно сжат Draco.

Скрипт можно перезапустить с другими параметрами (размер, число полигонов):

```
blender -b --python convert_shield.py -- shield.stl shield.glb shield.usdz 0.8 450000
```

## Как соответствуют требования

| Требование | Где реализовано |
|---|---|
| 3D-модель в AR через `<model-viewer>` | `index.html`, тег `<model-viewer>` |
| Режимы AR (`ar`, `ar-modes`) | атрибуты `ar` и `ar-modes="webxr scene-viewer quick-look"` |
| Quick Look (iOS) | атрибут `ios-src="assets/shield.usdz"` + `quick-look` в `ar-modes` |
| Scene Viewer (Android) | `src="assets/shield.glb"` + `scene-viewer` в `ar-modes` |
| Форматы glTF и USDZ | `assets/shield.glb`, `assets/shield.usdz` |
| HDRI-освещение | `environment-image="assets/lightroom_14b.hdr"` |

## Запуск

AR и камера работают только из **secure context**: `https://` или `http://localhost`.
Открытие `index.html` двойным кликом (протокол `file://`) не подойдёт.

### Локально (этот компьютер)

1. Запустите `start-server.bat` (двойной клик) — поднимется сервер на `http://localhost:8080`.
2. Откройте в браузере <http://localhost:8080/>.

Вращение модели — мышью/стрелками, зум — колесом.

### С телефона (нужен HTTPS)

Телефон обращается к вашему компьютеру по имени/IP — это уже не `localhost`,
поэтому обязателен HTTPS (самоподписанный сертификат подойдёт для проверки,
но его нужно принять на телефоне). Варианты:

- **Node.js**: `npx serve .` — затем туннель `npx localtunnel --port 3000`
  или `cloudflared tunnel --url http://localhost:3000` (даёт публичный HTTPS-URL).
- **ngrok**: запустить любой локальный сервер и `ngrok http 8080` — получите HTTPS-URL.
- Любой HTTPS-хостинг (GitHub Pages, Netlify и т.п.) — просто загрузите папку целиком.

## Проверка AR

- **Android** (Chrome, поддержка ARCore): кнопка **View in your space** открывает
  Scene Viewer, модель размещается на поверхности.
- **iOS / iPadOS** (Safari): та же кнопка открывает Quick Look с `Astronaut.usdz`.
- **Десктоп**: кнопка AR скрыта (нет поддержки AR), модель можно крутить и приближать.

На десктопе AR также можно включить через эмуляцию: Chrome DevTools →
Sensors → отключить/изменить флаги WebXR, либо открыть страницу в Chrome
с флагом `--enable-features=WebXRIncubations`.
