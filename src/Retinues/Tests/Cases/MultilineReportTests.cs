using System;
using System.Linq;
using Retinues.Interface.Services.Popups;
using Retinues.Interface.Widgets.TextEditing;

namespace Retinues.Tests.Cases
{
    public static class MultilineReportTests
    {
        [GameTest("ReportEditingPreservesParagraphsAndLiteralText", "bug-reports", RequiresCampaign = false)]
        public static void ReportEditingPreservesParagraphsAndLiteralText()
        {
            var buffer = new MultilineTextBuffer();
            buffer.Insert("Étapes:\r\n\r\n<item>\t崩溃\rFin\u2028Suite\0");
            Tests.AssertEqual("Étapes:\n\n<item>    崩溃\nFin\nSuite", buffer.Text);
            buffer.MoveTo(0);
            buffer.MoveHorizontal(1, true, true);
            Tests.AssertEqual("Étapes:\n\n", buffer.SelectedText);
            buffer.Insert("Reproduire:\n");
            Tests.AssertTrue(buffer.Text.StartsWith("Reproduire:\n<item>"));
            buffer.Undo();
            Tests.AssertEqual("Étapes:\n\n", buffer.SelectedText);
            buffer.Undo(true);
            Tests.AssertTrue(buffer.Text.StartsWith("Reproduire:\n<item>"));
            buffer.SelectAll();
            buffer.Delete(true);
            Tests.AssertEqual("", buffer.Text);
            buffer.Undo();
            buffer.Insert("Replacement");
            Tests.AssertEqual("Replacement", buffer.Text);
            Tests.AssertFalse(buffer.Undo(true), "A new edit discards the redo branch.");
        }

        [GameTest("ReportEditingRespectsUnicodeAndCapacity", "bug-reports", RequiresCampaign = false)]
        public static void ReportEditingRespectsUnicodeAndCapacity()
        {
            var buffer = new MultilineTextBuffer();
            buffer.Insert("A😀e\u0301");
            buffer.MoveHorizontal(-1, false, false);
            Tests.AssertEqual(3, buffer.Caret, "The accent is part of the preceding character.");
            buffer.Delete(true);
            Tests.AssertEqual("Ae\u0301", buffer.Text, "Backspace removes an entire surrogate pair.");
            buffer.Delete(false);
            Tests.AssertEqual("A", buffer.Text);
            buffer.SetText("\u0301");
            buffer.MoveTo(0);
            buffer.Insert("e");
            Tests.AssertEqual(2, buffer.Caret, "Insertion must leave the caret after a merged character.");
            buffer.SetText(new string('a', MultilineTextBuffer.CharacterLimit - 1));
            Tests.AssertFalse(buffer.Insert("😀"), "A full character must fit; never retain half a surrogate pair.");
            buffer.Insert("z!");
            Tests.AssertEqual(MultilineTextBuffer.CharacterLimit, buffer.Text.Length);
            Tests.AssertTrue(buffer.Text.EndsWith("z"));
            buffer.MoveTo(0);
            buffer.MoveTo(3, true);
            buffer.Insert("ABCDE");
            Tests.AssertTrue(buffer.Text.StartsWith("ABCaaa"));
            Tests.AssertEqual(MultilineTextBuffer.CharacterLimit, buffer.Text.Length);
            buffer.SetText(new string('a', MultilineTextBuffer.CharacterLimit - 1) + "😀");
            Tests.AssertEqual(MultilineTextBuffer.CharacterLimit - 1, buffer.Text.Length);
            Tests.AssertEqual("ok", MultilineTextBuffer.Normalize("\ud800ok\udc00"));
        }

        [GameTest("ReportWrapsKeepBlankLinesAndCaretPositions", "bug-reports", RequiresCampaign = false)]
        public static void ReportWrapsKeepBlankLinesAndCaretPositions()
        {
            const string text = "alpha beta\n\nend\n";
            var layout = new MultilineTextLayout(text, 6, s => s.Length);
            Tests.AssertEqual("alpha |beta||end|", string.Join("|", layout.Lines.Select(l => text.Substring(l.Start, l.End - l.Start))));
            Tests.AssertEqual(1, layout.FindLine(6));
            Tests.AssertEqual(0, layout.FindLine(6, 0), "End affinity stays on the previous visual line.");
            Tests.AssertEqual(6, layout.HitTest(0, 100));
            Tests.AssertEqual(0, layout.HitTest(0, -100));
            Tests.AssertEqual(8, layout.HitTest(1, 2.1f));
            Tests.AssertEqual(2f, layout.X(1, 8));
            Tests.AssertEqual(4, layout.FindLine(text.Length));
            Tests.AssertEqual(1, new MultilineTextLayout("", 0, s => 1).Lines.Count);
            var unicode = new MultilineTextLayout("😀e\u0301X", 1, s => 1);
            Tests.AssertEqual(3, unicode.Lines.Count);
            Tests.AssertEqual(2, unicode.Lines[0].End);
            Tests.AssertEqual(4, unicode.Lines[1].End);
        }

        [GameTest("ReportWrappingBoundsLongAndVariableWidthText", "bug-reports", RequiresCampaign = false)]
        public static void ReportWrappingBoundsLongAndVariableWidthText()
        {
            var random = new Random(521);
            for (int trial = 0; trial < 80; trial++)
            {
                string text = string.Concat(Enumerable.Range(0, 200).Select(_ => "aW \n"[random.Next(4)]));
                float width = random.Next(1, 20);
                float Measure(string s) => s == "W" ? 3 : 1;
                var layout = new MultilineTextLayout(text, width, Measure);
                int previousEnd = 0;
                foreach (var line in layout.Lines)
                {
                    Tests.AssertTrue(line.Start >= previousEnd && line.End >= line.Start);
                    Tests.AssertTrue(text.Substring(previousEnd, line.Start - previousEnd).All(c => c == '\n'), "Only explicit newlines are omitted from display rows.");
                    Tests.AssertFalse(text.Substring(line.Start, line.End - line.Start).Contains("\n"));
                    Tests.AssertTrue(layout.X(layout.Lines.IndexOf(line), line.End) <= width || line.End - line.Start == 1,
                        "Only a single oversized glyph may exceed the wrapping width.");
                    previousEnd = line.End;
                }
                Tests.AssertEqual(text.Length, previousEnd);
            }
        }

        [GameTest("ReportSubmissionPreservesDraftOnFailureAndClosesOnce", "bug-reports", RequiresCampaign = false)]
        public static void ReportSubmissionPreservesDraftOnFailureAndClosesOnce()
        {
            int submitted = 0, closed = 0;
            string received = null;
            var vm = new BugReportPopupVM(text => { received = text; return ++submitted == 1 ? "Save in progress" : null; }, () => closed++);
            vm.Description = "Étapes\n\n<item>\n";
            vm.ExecuteCreate();
            Tests.AssertEqual(0, closed);
            Tests.AssertEqual("Save in progress", vm.ErrorText);
            Tests.AssertEqual("Étapes\n\n<item>\n", vm.Description);
            vm.ExecuteCreate();
            vm.ExecuteCreate();
            vm.ExecuteCancel();
            Tests.AssertEqual(2, submitted);
            Tests.AssertEqual(1, closed);
            Tests.AssertEqual(vm.Description, received);
            var cancelled = new BugReportPopupVM(_ => { submitted++; return null; }, () => closed++);
            cancelled.ExecuteCancel();
            cancelled.ExecuteCreate();
            Tests.AssertEqual(2, submitted, "Cancel must never create an archive.");
        }
    }
}
