using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Editor.MVC.Pages.Equipment.Views.Panel;
using Retinues.Editor.MVC.Shared.Views;
using Retinues.Interface.Widgets;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace Retinues.Tests.Cases
{
    public static class UltrawideLayoutTests
    {
        [GameTest("EquipmentLayoutUnchangedAt16By9AndNarrower", "ui-layout", RequiresCampaign = false)]
        public static void EquipmentLayoutUnchangedAt16By9AndNarrower()
        {
            using var ui = new LayoutFixture();
            foreach (var size in new[] { (1280, 720), (1920, 1080), (2560, 1440), (3840, 2160), (1920, 1200), (1280, 1024), (1366, 768) })
            foreach (float scale in new[] { 0.75f, 1f, 1.25f })
            {
                ui.Resize(size.Item1, size.Item2, scale);
                var original = ui.Panel(false);
                var actual = (RetinuesEditorPanelWidget)ui.Panel(true);
                actual.KeepWithPreview = true;
                ui.Layout(original);
                ui.Layout(actual);
                Tests.AssertEqual(original.Left, actual.Left);
                Tests.AssertEqual(original.Top, actual.Top);
                Tests.AssertEqual(original.Size, actual.Size);
                Tests.AssertEqual(original.GetChild(0).Left, actual.GetChild(0).Left);
                Tests.AssertEqual(original.GetChild(0).Size, actual.GetChild(0).Size);
                Tests.AssertEqual(16f, actual.MarginRight, "No inset at 16:9 or narrower, at any UI scale.");
            }
        }

        [GameTest("UltrawideEquipmentStaysWithPreview", "ui-layout", RequiresCampaign = false)]
        public static void UltrawideEquipmentStaysWithPreview()
        {
            using var ui = new LayoutFixture();
            foreach (var size in new[] { (2560, 1080), (3440, 1440), (3840, 1600), (5120, 1440) })
            foreach (float scale in new[] { 0.75f, 1f, 1.25f })
            {
                // Compare the actual engine layout to the old layout at the same
                // height/UI scale, with only the extra horizontal viewport removed.
                ui.Resize(size.Item2 * 16f / 9f, size.Item2, scale);
                var original = ui.Panel(false);
                ui.Layout(original);
                ui.Resize(size.Item1, size.Item2, scale);
                var actual = (RetinuesEditorPanelWidget)ui.Panel(true);
                actual.KeepWithPreview = true;
                ui.Layout(actual);
                Near(original.Left, actual.Left);
                Tests.AssertEqual(original.Top, actual.Top);
                Tests.AssertEqual(original.Size, actual.Size);
                Tests.AssertEqual(original.GetChild(0).Left, actual.GetChild(0).Left);
                var button = actual.GetChild(0);
                Tests.AssertTrue(button.Left >= 0 && button.Right <= actual.Size.X,
                    "The equipment control stays inside its relocated parent bounds.");
                float margin = actual.MarginRight;
                ui.Layout(actual);
                Tests.AssertEqual(margin, actual.MarginRight, "Repeated updates must not accumulate the inset.");
            }
        }

        [GameTest("EquipmentLayoutRestoresAfterResizeAndPageChange", "ui-layout", RequiresCampaign = false)]
        public static void EquipmentLayoutRestoresAfterResizeAndPageChange()
        {
            using var ui = new LayoutFixture();
            var panel = (RetinuesEditorPanelWidget)ui.Panel(true);
            panel.KeepWithPreview = true;
            ui.Resize(3440, 1440, 1f);
            ui.Layout(panel);
            Tests.AssertTrue(panel.MarginRight > 16f);
            panel.KeepWithPreview = false;
            ui.Layout(panel);
            Tests.AssertEqual(16f, panel.MarginRight);
            panel.KeepWithPreview = true;
            ui.Layout(panel);
            Tests.AssertTrue(panel.MarginRight > 16f);
            ui.Resize(1920, 1080, 1.25f);
            ui.Layout(panel);
            Tests.AssertEqual(16f, panel.MarginRight);
            ui.Resize(3840, 1600, 0.75f);
            ui.Layout(panel);
            var fresh = (RetinuesEditorPanelWidget)ui.Panel(true);
            fresh.KeepWithPreview = true;
            ui.Layout(fresh);
            Tests.AssertEqual(fresh.Left, panel.Left);
            Tests.AssertEqual(fresh.MarginRight, panel.MarginRight);
            foreach (float invalid in new[] { 0f, float.NaN, float.PositiveInfinity })
            {
                ui.Resize(3440, invalid, 1f);
                ui.Tick(panel);
                Tests.AssertEqual(16f, panel.MarginRight, "Unavailable viewport dimensions must not corrupt margins.");
            }
        }

        [GameTest("OnlyEquipmentPageOptsIntoUltrawideLayout", "ui-layout", RequiresCampaign = false)]
        public static void OnlyEquipmentPageOptsIntoUltrawideLayout()
        {
            foreach (var type in typeof(BasePanelVM).Assembly.GetTypes()
                .Where(t => typeof(BasePanelVM).IsAssignableFrom(t) && !t.IsAbstract))
            {
                var panel = (BasePanelVM)FormatterServices.GetUninitializedObject(type);
                Tests.AssertEqual(type == typeof(EquipmentPanelVM), panel.KeepWithPreview, type.FullName);
            }
        }

        private static void Near(float expected, float actual, float tolerance = 0.001f) =>
            Tests.AssertTrue(Math.Abs(expected - actual) <= tolerance, $"Expected {expected}, got {actual}.");

        // Exercise Gauntlet's real measure/layout and our normal OnUpdate callback,
        // without initializing graphics, game input or native resources. The isolated
        // event manager only receives layout-dirty flags; widgets are not attached to
        // the game's root. Restore the global widget metadata after each test.
        private sealed class LayoutFixture : IDisposable
        {
            private readonly FieldInfo _widgetInfos = AccessTools.Field(typeof(WidgetInfo), "_widgetInfos");
            private readonly object _previousInfos;
            private readonly UIContext _context = (UIContext)FormatterServices.GetUninitializedObject(typeof(UIContext));
            private readonly Viewport _viewport = new();
            private static readonly MethodInfo Update = AccessTools.Method(typeof(RetinuesEditorPanelWidget), "OnUpdate");
            private static readonly MethodInfo Measure = AccessTools.Method(typeof(Widget), "Measure");

            public LayoutFixture()
            {
                _previousInfos = _widgetInfos.GetValue(null);
                _widgetInfos.SetValue(null, new Dictionary<Type, WidgetInfo>
                {
                    [typeof(Widget)] = new WidgetInfo(typeof(Widget)),
                    [typeof(RetinuesEditorPanelWidget)] = new WidgetInfo(typeof(RetinuesEditorPanelWidget)),
                });
                SetContext("EventManager", FormatterServices.GetUninitializedObject(typeof(EventManager)));
                SetContext("TwoDimensionContext", new TwoDimensionContext(
                    (ITwoDimensionPlatform)_viewport.GetTransparentProxy(), null, null));
                Resize(1920, 1080, 1f);
            }

            public void Resize(float width, float height, float scaleModifier)
            {
                _viewport.Width = width;
                _viewport.Height = height;
                float scale = height / 1080f;
                SetContext("Scale", scale);
                SetContext("InverseScale", 1f / scale);
                SetContext("CustomScale", scale * scaleModifier);
                SetContext("CustomInverseScale", 1f / (scale * scaleModifier));
            }

            public Widget Panel(bool corrected)
            {
                Widget panel = corrected ? new RetinuesEditorPanelWidget(_context) : new Widget(_context);
                panel.WidthSizePolicy = SizePolicy.Fixed;
                panel.HeightSizePolicy = SizePolicy.StretchToParent;
                panel.SuggestedWidth = 820;
                panel.MarginRight = 16;
                panel.MarginTop = 16;
                panel.HorizontalAlignment = HorizontalAlignment.Right;
                panel.AddChild(new Widget(_context)
                {
                    WidthSizePolicy = SizePolicy.Fixed,
                    HeightSizePolicy = SizePolicy.Fixed,
                    SuggestedWidth = 160,
                    SuggestedHeight = 52,
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
                return panel;
            }

            public void Tick(Widget panel)
            {
                if (panel is RetinuesEditorPanelWidget)
                    Update.Invoke(panel, new object[] { 0.016f });
            }

            public void Layout(Widget panel)
            {
                Tick(panel);
                // The editor body is shorter than the screen (header and footer).
                // This must never be used to decide whether the screen is ultrawide.
                float bodyHeight = _viewport.Height - 263f * _context.CustomScale;
                // The reference sets contain two Vector2 identities. Use the one
                // required by this engine version without changing assembly aliases.
                var size = Activator.CreateInstance(Measure.GetParameters()[0].ParameterType, _viewport.Width, bodyHeight);
                Measure.Invoke(panel, new[] { size });
                panel.Layout(0, bodyHeight, _viewport.Width, 0);
            }

            private void SetContext(string name, object value) =>
                AccessTools.Property(typeof(UIContext), name).SetValue(_context, value);

            public void Dispose() => _widgetInfos.SetValue(null, _previousInfos);
        }

        private sealed class Viewport() : RealProxy(typeof(ITwoDimensionPlatform))
        {
            internal float Width;
            internal float Height;

            public override IMessage Invoke(IMessage message)
            {
                var call = (IMethodCallMessage)message;
                object value = call.MethodName switch
                {
                    "get_Width" => Width,
                    "get_Height" => Height,
                    _ => throw new NotSupportedException(call.MethodName),
                };
                return new ReturnMessage(value, null, 0, call.LogicalCallContext, call);
            }
        }
    }
}
