# GridBubbleHider (Revit add-in)

Команда: у оси переключает конец с **3D на 2D** на текущем виде и затем **скрывает кружок** оси.

## Использование
- Выделите оси заранее и запустите команду - обработаются все видимые 3D-концы.
- Либо запустите без выделения и кликайте оси; обрабатывается конец, ближе к которому кликнули. `Esc` - выход.

## Сборка и установка
```
dotnet build -c Release -p:RevitDir="C:\Program Files\Autodesk\Revit 2023"
```
Скопируйте `GridBubbleHider.dll` и `GridBubbleHider.addin` в
`%AppData%\Autodesk\Revit\Addins\2023\` (в `.addin` при необходимости укажите полный путь к dll).
Для Revit 2025+ смените `TargetFramework` на `net8.0-windows`.
