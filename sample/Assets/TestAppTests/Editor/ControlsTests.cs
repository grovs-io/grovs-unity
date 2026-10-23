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
