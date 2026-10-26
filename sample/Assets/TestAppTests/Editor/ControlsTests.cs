using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp.Tests
{
    public class ControlsTests
    {
        GameObject _host;
        PanelSettings _settings;
        VisualElement _root;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _host = new GameObject("ControlsTests");
            var document = _host.AddComponent<UIDocument>();
            document.panelSettings = _settings;
            _root = document.rootVisualElement;
            Assert.NotNull(_root?.panel, "The test needs a live panel to dispatch events");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_settings);
        }

        static void Click(VisualElement target)
        {
            using (var click = ClickEvent.GetPooled())
            {
                click.target = target;
                target.SendEvent(click);
            }
        }

        static void SendPointer<T>(VisualElement target, EventType type, Vector2 position) where T : PointerEventBase<T>, new()
        {
            var system = new Event { type = type, mousePosition = position, button = 0 };
            using (var evt = PointerEventBase<T>.GetPooled(system))
            {
                evt.target = target;
                target.SendEvent(evt);
            }
        }

        static void SendKey(VisualElement target, char character, KeyCode key)
        {
            using (var evt = KeyDownEvent.GetPooled(character, key, EventModifiers.None))
            {
                evt.target = target;
                target.SendEvent(evt);
            }
        }

        [Test]
        public void Segment_fires_once_on_a_click()
        {
            var picked = new List<int>();
            var segment = new Segment<int>(new[] { 1, 2, 3 }, 1, picked.Add);
            _root.Add(segment);

            Click(segment.Q(className: Segment<int>.OptionClass, name: "option-1"));

            CollectionAssert.AreEqual(new[] { 2 }, picked);
        }

        [Test]
        public void KeyValueRows_detects_the_type_while_typing()
        {
            var rows = new List<KeyValueRow> { new KeyValueRow { Key = "level", Value = "", Kind = ValueKind.Text } };
            var view = new KeyValueRows(rows);
            _root.Add(view);

            view.Q<TextField>("kvValue0").value = "12";

            Assert.AreEqual("123", view.Q<Label>(className: KeyValueRows.BadgeClass).text);
            Assert.AreEqual(ValueKind.Number, rows[0].Kind);
        }

        [Test]
        public void KeyValueRows_keeps_a_tapped_type()
        {
            var rows = new List<KeyValueRow> { new KeyValueRow { Key = "level", Value = "", Kind = ValueKind.Text } };
            var view = new KeyValueRows(rows);
            _root.Add(view);

            Click(view.Q<Label>(className: KeyValueRows.BadgeClass));
            view.Q<TextField>("kvValue0").value = "true";

            Assert.AreEqual("123", view.Q<Label>(className: KeyValueRows.BadgeClass).text);
            Assert.AreEqual(ValueKind.Number, rows[0].Kind);
        }

        [Test]
        public void ChipInput_commits_on_a_comma()
        {
            var values = new List<string> { "a" };
            var input = new ChipInput(values, max: 2);
            _root.Add(input);

            input.Q<TextField>().value = "b, c";

            CollectionAssert.AreEqual(new[] { "a", "b" }, values);
            Assert.AreEqual(" c", input.Q<TextField>().value);
        }

        [Test]
        public void Sheet_close_raises_closed_once()
        {
            var closed = 0;
            var handle = Sheet.Show(_root, new Label("content"));
            handle.Closed += () => closed++;

            Assert.NotNull(_root.Q(className: "sheet"));
            handle.Close();
            handle.Close();

            Assert.AreEqual(1, closed);
            Assert.IsFalse(handle.IsOpen);
        }

        [Test]
        public void Toast_replaces_the_previous_toast()
        {
            Toast.Show(_root, "First");
            Toast.Show(_root, "Copied");

            var toasts = _root.Query(className: "toast").ToList();
            Assert.AreEqual(1, toasts.Count);
            Assert.AreEqual("Copied", toasts[0].Q<Label>().text);
        }

        [Test]
        public void InfoButton_tap_does_not_toggle_its_row()
        {
            var changes = 0;
            var row = new SwitchRow("SDK enabled", false, _ => changes++, help: ("About", "Body"));
            _root.Add(row);
            var info = row.Q<InfoButton>();

            SendPointer<PointerDownEvent>(info, EventType.MouseDown, Vector2.zero);
            Click(info);

            Assert.IsFalse(row.Value);
            Assert.AreEqual(0, changes);
            Assert.NotNull(_root.Q(className: "dialog"), "The tap still opens the dialog");
        }

        [Test]
        public void Submitted_fires_once_per_enter_press()
        {
            var submitted = 0;
            var field = new AppTextField("name");
            field.Submitted += () => submitted++;
            _root.Add(field);
            field.Input.Focus();

            SendKey(field.Input, '\0', KeyCode.Return);
            SendKey(field.Input, '\n', KeyCode.None);

            Assert.AreEqual(1, submitted);
        }

        [Test]
        public void Leaving_a_field_without_the_keyboard_done_key_does_not_submit()
        {
            var submitted = 0;
            var blurred = 0;
            var field = new AppTextField("name");
            field.Submitted += () => submitted++;
            field.Blurred += () => blurred++;
            _root.Add(field);

            field.Input.Focus();
            field.Input.Blur();

            Assert.AreEqual(0, submitted);
            Assert.AreEqual(1, blurred);
        }

        [Test]
        public void Toast_never_catches_taps()
        {
            Toast.Show(_root, "Copied");

            var toast = _root.Q(className: "toast");
            toast.Query<VisualElement>().ForEach(e => Assert.AreEqual(PickingMode.Ignore, e.pickingMode, e.GetType().Name));
        }

        [Test]
        public void AppButton_press_ends_when_the_finger_moves()
        {
            var button = new AppButton("Run", () => { }, onLongPress: () => { });
            _root.Add(button);

            SendPointer<PointerDownEvent>(button, EventType.MouseDown, new Vector2(10, 10));
            SendPointer<PointerMoveEvent>(button, EventType.MouseDrag, new Vector2(13, 10));
            Assert.IsTrue(button.ClassListContains(Ui.PressedClass), "A small wobble keeps the press");

            SendPointer<PointerMoveEvent>(button, EventType.MouseDrag, new Vector2(30, 10));
            Assert.IsFalse(button.ClassListContains(Ui.PressedClass));
        }

        [Test]
        public void FoldRow_silent_setter_does_not_raise_toggled()
        {
            var toggled = 0;
            var row = new FoldRow("Advanced", () => "3 set", new Label("content"));
            row.Toggled += _ => toggled++;
            _root.Add(row);

            row.SetExpandedWithoutNotify(true);
            Assert.IsTrue(row.Expanded);
            Assert.AreEqual(0, toggled);

            row.Expanded = false;
            Assert.AreEqual(1, toggled);
        }

        [Test]
        public void ChipPicker_notifies_only_on_a_real_change()
        {
            var changes = new List<string>();
            var picker = new ChipPicker(new[] { "USD", "EUR", "GBP" }, "USD", changes.Add);
            _root.Add(picker);

            picker.Value = "USD";
            picker.Value = "EUR";
            picker.SetValueWithoutNotify("GBP");

            CollectionAssert.AreEqual(new[] { "EUR" }, changes);
            Assert.AreEqual("GBP", picker.Value);
        }

        [Test]
        public void Overlay_forgets_a_sheet_removed_without_close()
        {
            var host = new VisualElement();
            _root.Add(host);
            var stale = Sheet.Show(host, new Label("old screen"));

            host.RemoveFromHierarchy();
            var fresh = Sheet.Show(_root, new Label("new screen"));

            Assert.IsFalse(stale.IsOpen);
            Assert.AreSame(fresh, Overlay.Newest);
            fresh.Close();
            Assert.IsNull(Overlay.Newest);
        }

        [Test]
        public void Back_handler_can_keep_the_sheet_open()
        {
            var sheet = Sheet.Show(_root, new Label("detail"));
            var inDetail = true;
            sheet.BackHandler = () =>
            {
                if (!inDetail) return false;
                inDetail = false;
                return true;
            };

            sheet.HandleBack();
            Assert.IsTrue(sheet.IsOpen);
            sheet.HandleBack();
            Assert.IsFalse(sheet.IsOpen);
        }

        [Test]
        public void AppButton_ignores_clicks_while_disabled()
        {
            var clicks = 0;
            var button = new AppButton("Run", () => clicks++) { Enabled = false };
            _root.Add(button);

            Click(button);
            button.Enabled = true;
            Click(button);

            Assert.AreEqual(1, clicks);
        }
    }
}
