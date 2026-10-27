using System;
using System.Collections.Generic;
using System.Linq;
using GrovsSDK;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>The fields of the event and purchase forms. Edits go straight into the form, then changed is called.</summary>
    public static class EventFormFields
    {
        public static readonly string[] DefaultEventNames = { "demo_tapped", "signup", "level_up", "share" };

        static readonly string[] Currencies = { "USD", "EUR", "GBP", "RON" };
        static readonly string[] Products = { "pro_monthly", "pro_yearly", "coins_100" };
        static readonly string[] StartLabels = { "Now", "1 day ago", "30 days ago" };
        static readonly int[] StartDays = { 0, 1, 30 };
        static readonly string[] SuggestedPropertyKeys = { "level", "source" };

        public static IList<string> EventSuggestions(EventForm form) => form.Recent.Concat(DefaultEventNames).Distinct().ToList();

        public static string EventError(EventForm form) => EventRules.NameError(form.Name) ?? EventRules.TagsError(form.Tags);

        public static bool PurchaseValid(PurchaseForm form) =>
            Money.ParseCents(form.Amount) != null
            && !string.IsNullOrWhiteSpace(form.Currency)
            && !string.IsNullOrWhiteSpace(form.Product);

        public static VisualElement BuildEvent(EventForm form, Action changed, Action sendAnyway, Action submit = null)
        {
            var root = Ui.Box("event-form");
            FoldRow more = null;

            var name = new SuggestField(EventSuggestions(form), mono: true);
            var anyway = new TextAction("Send anyway", sendAnyway);
            anyway.AddToClassList("event-form__anyway");
            void Refresh()
            {
                name.Error = EventError(form);
                anyway.style.display = EventRules.IsReserved(form.Name) ? DisplayStyle.Flex : DisplayStyle.None;
                more?.RefreshSummary();
            }
            name.SetValueWithoutNotify(form.Name);
            name.Changed += v =>
            {
                form.Name = v;
                Refresh();
                changed?.Invoke();
            };
            if (submit != null) name.Field.Submitted += submit;
            root.Add(name);
            root.Add(anyway);

            var properties = new KeyValueRows(form.Properties, "Properties", SuggestedPropertyKeys);
            var tags = new ChipInput(form.Tags, "Tags", "Add tag", EventRules.MaxTags);
            tags.AddToClassList("event-form__gap");
            var content = Ui.Box();
            content.Add(properties);
            content.Add(tags);
            void Edited()
            {
                Refresh();
                changed?.Invoke();
            }
            properties.Changed += Edited;
            tags.Changed += Edited;

            more = new FoldRow("Properties and tags", () => EventSummary(form), content,
                help: Help.Get("eventPropertiesAndTags"));
            root.Add(more);
            Refresh();
            return root;
        }

        public static void SetEventSuggestions(VisualElement eventForm, EventForm form) =>
            eventForm.Q<SuggestField>()?.SetSuggestions(EventSuggestions(form));

        static string EventSummary(EventForm form)
        {
            var properties = string.Join(", ", form.Properties.Where(r => !string.IsNullOrWhiteSpace(r.Key)).Select(r => r.Key + "=" + r.Value));
            var tags = string.Join(", ", form.Tags.Select(t => "tag " + t));
            var summary = string.Join(" · ", new[] { properties, tags }.Where(p => p.Length > 0));
            return summary.Length == 0 ? "none" : summary;
        }

        /// <summary>The purchase fields. The store receipt fold is added when logReceipt is given.</summary>
        public static VisualElement BuildPurchase(PurchaseForm form, Action changed, Action logReceipt = null)
        {
            var root = Ui.Box("purchase-form");

            root.Add(new Segment<GrovsTransactionType>(
                (GrovsTransactionType[])Enum.GetValues(typeof(GrovsTransactionType)), form.Type, v =>
                {
                    form.Type = v;
                    changed?.Invoke();
                }, label: v => v.ToString()));

            var money = new MoneyField(form.Currency);
            money.AddToClassList("event-form__gap");
            money.SetValueWithoutNotify(form.Amount);
            money.Changed += v =>
            {
                form.Amount = v;
                changed?.Invoke();
            };
            root.Add(money);

            var currency = new ChipPicker(Currencies, form.Currency, v =>
            {
                form.Currency = v.ToUpperInvariant();
                money.Currency = form.Currency;
                changed?.Invoke();
            }, "Currency", "Code, e.g. JPY");
            currency.AddToClassList("event-form__gap");
            root.Add(currency);

            var product = new ChipPicker(Products, form.Product, v =>
            {
                form.Product = v;
                changed?.Invoke();
            }, "Product", "product id");
            product.AddToClassList("event-form__gap");
            root.Add(product);

            root.Add(StartDate(form, changed));
            if (logReceipt != null) root.Add(StoreReceipt(form, changed, logReceipt));
            return root;
        }

        static FoldRow StartDate(PurchaseForm form, Action changed)
        {
            ChipRow picker = null;
            FoldRow fold = null;
            picker = new ChipRow(StartLabels, StartLabel(form.DaysAgo), label =>
            {
                form.DaysAgo = StartDays[Array.IndexOf(StartLabels, label)];
                picker.Selected = label;
                fold?.RefreshSummary();
                changed?.Invoke();
            });
            picker.AddToClassList("event-form__gap");
            fold = new FoldRow("Start date", () => StartLabel(form.DaysAgo) ?? form.DaysAgo + " days ago", picker,
                help: Help.Get("startDate"));
            return fold;
        }

        static string StartLabel(int days)
        {
            var index = Array.IndexOf(StartDays, days);
            return index < 0 ? null : StartLabels[index];
        }

        static FoldRow StoreReceipt(PurchaseForm form, Action changed, Action logReceipt)
        {
            var ios = Application.platform == RuntimePlatform.IPhonePlayer;
            var content = Ui.Box();
            var field = new AppTextField(ios ? "transaction id" : "purchase.originalJson", true, multiline: true);
            field.AddToClassList("receipt-field");
            field.SetValueWithoutNotify(form.StoreReceipt);
            var button = new AppButton(ios ? "Log App Store purchase" : "Log Play purchase", logReceipt, AppButtonStyle.Outline);
            button.AddToClassList("event-form__gap");
            button.Enabled = !string.IsNullOrWhiteSpace(form.StoreReceipt);

            FoldRow fold = null;
            field.Changed += v =>
            {
                form.StoreReceipt = v;
                button.Enabled = !string.IsNullOrWhiteSpace(v);
                fold?.RefreshSummary();
                changed?.Invoke();
            };
            content.Add(field);
            content.Add(button);
            fold = new FoldRow(ios ? "App Store transaction id" : "Google Play purchase JSON",
                () => string.IsNullOrWhiteSpace(form.StoreReceipt) ? "empty" : "pasted", content, help: Help.Get("storeReceipt"));
            return fold;
        }
    }
}
