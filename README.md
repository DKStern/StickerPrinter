# StickerPrinter

`StickerPrinter` - desktop-приложение для создания и предпросмотра этикеток на основе ZPL-кода.

## Стек

- C#;
- .NET 10;
- WPF;
- MVVM;
- Microsoft.Extensions.Hosting;
- Dependency Injection;
- HttpClientFactory;
- Polly;
- Serilog.

## Возможности MVP

- редактирование ZPL-кода в многострочном редакторе;
- настройка ширины и высоты этикетки в дюймах;
- выбор плотности печати: 8, 12 или 24 dpmm;
- создание предпросмотра этикетки через внешний HTTP API;
- отображение PNG-предпросмотра в приложении;
- сохранение полученного PNG-файла на диск;
- валидация ZPL-кода и параметров этикетки;
- логирование работы приложения и ошибок.

## Рендеринг

Этикетка не рендерится локально. Приложение отправляет ZPL-код во внешний сервис рендеринга и получает PNG-изображение.

По умолчанию используется Labelary:

```json
{
  "ZplRenderer": {
    "BaseUrl": "http://api.labelary.com",
    "RenderEndpoint": "/v1/printers/{density}dpmm/labels/{width}x{height}/0/",
    "TimeoutSeconds": 30
  }
}
```

Настройки находятся в:

- `StickerPrinter/appsettings.json`;
- `StickerPrinter/appsettings.Development.json`.

## Логи

Логи пишутся в отдельную папку для каждой сессии запуска:

```text
logs/
  yyyy-MM-dd_HH-mm-ss/
    application.log
    errors.log
```

Полный ZPL-код в логи не записывается.

## Планы

В дальнейшем планируется добавить поддержку EPL-кода.
