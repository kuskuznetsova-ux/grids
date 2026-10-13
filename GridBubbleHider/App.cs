using System;
using System.Reflection;
using Autodesk.Revit.UI;

namespace GridBubbleHider
{
    /// <summary>Создаёт вкладку "Оси" с кнопкой запуска команды.</summary>
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            const string tab = "Оси";
            try { app.CreateRibbonTab(tab); } catch (Exception) { /* вкладка уже есть */ }

            RibbonPanel panel = app.CreateRibbonPanel(tab, "Оси");
            var data = new PushButtonData(
                "GridBubbleHider",
                "Скрыть\nкружок",
                Assembly.GetExecutingAssembly().Location,
                typeof(HideGridBubbleCommand).FullName)
            {
                ToolTip = "Нажмите и кликайте по осям: конец 3D -> 2D и кружок скрывается. Esc - выход."
            };
            panel.AddItem(data);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;
    }
}
