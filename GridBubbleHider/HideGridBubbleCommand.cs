using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace GridBubbleHider
{
    /// <summary>
    /// Для выбранной оси: переключает конец с 3D на 2D (на текущем виде)
    /// и скрывает кружок (снимает галочку "Показать номер").
    /// Если оси выбраны заранее - сначала обрабатываются все их 3D-концы.
    /// Затем режим выбора: кликайте оси по одной, обрабатывается конец,
    /// ближайший к точке клика. Esc - завершить.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class HideGridBubbleCommand : IExternalCommand
    {
        private class GridFilter : ISelectionFilter
        {
            public bool AllowElement(Element e) => e is Grid;
            public bool AllowReference(Reference r, XYZ p) => false;
        }

        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            UIDocument uidoc = data.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            if (view is View3D)
            {
                message = "Запустите команду на 2D-виде (план, разрез, фасад).";
                return Result.Failed;
            }

            var pre = new List<Grid>();
            foreach (ElementId id in uidoc.Selection.GetElementIds())
                if (doc.GetElement(id) is Grid g) pre.Add(g);

            using (var tg = new TransactionGroup(doc, "Оси: 3D -> 2D, скрыть кружок"))
            {
                tg.Start();

                if (pre.Count > 0)
                {
                    using (var t = new Transaction(doc, "Скрыть кружки осей"))
                    {
                        t.Start();
                        foreach (Grid g in pre)
                            foreach (DatumEnds end in new[] { DatumEnds.End0, DatumEnds.End1 })
                                if (g.GetDatumExtentTypeInView(end, view) == DatumExtentType.Model)
                                    Process(g, end, view);
                        t.Commit();
                    }
                    uidoc.Selection.SetElementIds(new List<ElementId>());
                }

                // дальше - выбор осей по одной до Esc
                while (true)
                {
                    Reference r;
                    try
                    {
                        r = uidoc.Selection.PickObject(ObjectType.Element, new GridFilter(),
                            "Кликайте по осям (Esc - завершить)");
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        break;
                    }

                    var g = (Grid)doc.GetElement(r);
                    DatumEnds end = NearestEnd(g, r.GlobalPoint);
                    using (var t = new Transaction(doc, "Скрыть кружок оси"))
                    {
                        t.Start();
                        Process(g, end, view);
                        t.Commit();
                    }
                }

                tg.Assimilate();
            }
            return Result.Succeeded;
        }

        private static void Process(Grid g, DatumEnds end, View view)
        {
            if (g.GetDatumExtentTypeInView(end, view) != DatumExtentType.ViewSpecific)
                g.SetDatumExtentType(end, view, DatumExtentType.ViewSpecific);
            g.HideBubbleInView(end, view);
        }

        private static DatumEnds NearestEnd(Grid g, XYZ p)
        {
            Curve c = g.Curve;
            XYZ p0 = c.GetEndPoint(0), p1 = c.GetEndPoint(1);
            // сравнение в плане (по XY), т.к. точка клика может иметь другую Z
            double d0 = new XYZ(p0.X - p.X, p0.Y - p.Y, 0).GetLength();
            double d1 = new XYZ(p1.X - p.X, p1.Y - p.Y, 0).GetLength();
            return d0 <= d1 ? DatumEnds.End0 : DatumEnds.End1;
        }
    }
}
