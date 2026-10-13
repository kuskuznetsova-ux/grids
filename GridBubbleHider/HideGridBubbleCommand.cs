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
                    DatumEnds end = PickEnd(g, r.GlobalPoint, view);
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

        /// <summary>
        /// Какой конец оси обрабатывать. Если ровно один конец на виде 3D - берём его
        /// (именно его и хотят перевести в 2D). Иначе - ближайший к клику, причём
        /// положение конца берётся из кривой, как она отображается на этом виде
        /// (у 2D-конца оно отличается от модельного).
        /// </summary>
        private static DatumEnds PickEnd(Grid g, XYZ p, View view)
        {
            bool m0 = g.GetDatumExtentTypeInView(DatumEnds.End0, view) == DatumExtentType.Model;
            bool m1 = g.GetDatumExtentTypeInView(DatumEnds.End1, view) == DatumExtentType.Model;
            if (m0 && !m1) return DatumEnds.End0;
            if (m1 && !m0) return DatumEnds.End1;

            double d0 = PlanDistance(EndPointInView(g, DatumEnds.End0, view), p);
            double d1 = PlanDistance(EndPointInView(g, DatumEnds.End1, view), p);
            return d0 <= d1 ? DatumEnds.End0 : DatumEnds.End1;
        }

        private static XYZ EndPointInView(Grid g, DatumEnds end, View view)
        {
            DatumExtentType type = g.GetDatumExtentTypeInView(end, view);
            IList<Curve> curves = g.GetCurvesInView(type, view);
            Curve c = curves != null && curves.Count > 0 ? curves[0] : g.Curve;
            return c.GetEndPoint(end == DatumEnds.End0 ? 0 : 1);
        }

        // сравнение в плане (по XY), т.к. точка клика может иметь другую Z
        private static double PlanDistance(XYZ a, XYZ b) =>
            new XYZ(a.X - b.X, a.Y - b.Y, 0).GetLength();
    }
}
