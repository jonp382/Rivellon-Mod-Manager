using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace ModManager.Controls;

// a selectable text block that renders the BBCode used in Steam workshop descriptions
// ([h1], [b], [i], [u], [strike], [list], [olist], [*], [url], [quote], [code], [hr] ...)
// instead of showing the raw tags. images, tables and unknown tags are stripped.
public partial class BBCodeTextBlock : SelectableTextBlock
{
    public static readonly StyledProperty<string?> BBCodeProperty =
        AvaloniaProperty.Register<BBCodeTextBlock, string?>(nameof(BBCode));

    public string? BBCode
    {
        get => GetValue(BBCodeProperty);
        set => SetValue(BBCodeProperty, value);
    }

    // keep the normal SelectableTextBlock styling
    protected override Type StyleKeyOverride => typeof(SelectableTextBlock);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BBCodeProperty) Rebuild();
    }

    // matches [tag], [/tag], [tag=value] and [*]
    [GeneratedRegex(@"\[(/?)(\*|[a-zA-Z0-9]+)(?:=([^\]]*))?\]")]
    private static partial Regex TagRegex();

    // the formatting that applies to the text currently being read
    private sealed class State
    {
        public int Bold, Italic, Underline, Strike, Code, Quote, Hidden;
        public double? HeadingSize;
        public string? LinkUrl;
        // set after a list bullet, so the item text lines up whether or not the source has a space after [*]
        public bool TrimNextLeading;
    }

    private void Rebuild()
    {
        var inlines = new InlineCollection();
        string text = (BBCode ?? string.Empty).Replace("\r\n", "\n").Trim();

        var state = new State();
        // each open list keeps its item counter (null for bullet lists)
        var lists = new Stack<int?>();
        bool noParse = false;
        int pos = 0;

        foreach (Match match in TagRegex().Matches(text))
        {
            AddText(inlines, text[pos..match.Index], state);
            pos = match.Index + match.Length;

            bool closing = match.Groups[1].Value == "/";
            string tag = match.Groups[2].Value.ToLowerInvariant();
            string value = match.Groups[3].Value;

            // inside [noparse] every tag is shown as plain text until [/noparse]
            if (noParse && !(closing && tag == "noparse"))
            {
                AddText(inlines, match.Value, state);
                continue;
            }

            switch (tag)
            {
                case "noparse": noParse = !closing; break;
                case "b": state.Bold += closing ? -1 : 1; break;
                case "i": state.Italic += closing ? -1 : 1; break;
                case "u": state.Underline += closing ? -1 : 1; break;
                case "strike": state.Strike += closing ? -1 : 1; break;
                case "spoiler": state.Italic += closing ? -1 : 1; break;
                case "code": state.Code += closing ? -1 : 1; break;
                case "quote": state.Quote += closing ? -1 : 1; break;

                // image links would just be noise in the description
                case "img":
                case "previewyoutube":
                    state.Hidden += closing ? -1 : 1;
                    break;

                case "h1": state.HeadingSize = closing ? null : FontSize + 6; break;
                case "h2": state.HeadingSize = closing ? null : FontSize + 4; break;
                case "h3": state.HeadingSize = closing ? null : FontSize + 2; break;

                case "url":
                    state.LinkUrl = closing ? null : (string.IsNullOrWhiteSpace(value) ? "" : value);
                    break;

                case "hr":
                    if (!closing) AddText(inlines, "\n────────────\n", state);
                    break;

                case "list":
                case "olist":
                    if (closing) { if (lists.Count > 0) lists.Pop(); }
                    else lists.Push(tag == "olist" ? 0 : null);
                    break;

                case "*":
                    string bullet = "•";
                    if (lists.Count > 0 && lists.Peek() is int count)
                    {
                        lists.Pop();
                        lists.Push(count + 1);
                        bullet = $"{count + 1}.";
                    }
                    // start each item on its own line, indented by how deeply the list is nested
                    TrimTrailingWhitespace(inlines);
                    if (inlines.Count > 0) inlines.Add(new LineBreak());
                    inlines.Add(new Run(new string(' ', Math.Max(0, lists.Count - 1) * 4) + bullet + " "));
                    state.TrimNextLeading = true;
                    break;

                // table cells: keep the text, separate cells with spaces and rows with line breaks
                case "tr": if (closing) AddText(inlines, "\n", state); break;
                case "td":
                case "th": if (closing) AddText(inlines, "   ", state); break;

                default:
                    // unknown tags are dropped, their text is kept
                    break;
            }
        }

        AddText(inlines, text[pos..], state);
        TrimTrailingWhitespace(inlines);

        Inlines = inlines;
    }

    private static void AddText(InlineCollection inlines, string text, State state)
    {
        if (string.IsNullOrEmpty(text) || state.Hidden > 0) return;

        if (state.TrimNextLeading)
        {
            text = text.TrimStart(' ', '\t');
            if (text.Length == 0) return;
            state.TrimNextLeading = false;
        }

        // never show more than one empty line in a row, steam descriptions often have lots of them
        var lines = Regex.Replace(text, @"\n{3,}", "\n\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) inlines.Add(new LineBreak());
            if (lines[i].Length == 0) continue;

            var run = new Run(lines[i]);
            if (state.Bold > 0 || state.HeadingSize != null) run.FontWeight = FontWeight.Bold;
            if (state.Italic > 0 || state.Quote > 0) run.FontStyle = FontStyle.Italic;
            if (state.HeadingSize is double size) run.FontSize = size;
            if (state.Code > 0) run.FontFamily = new FontFamily("Consolas, Menlo, monospace");

            var decorations = new TextDecorationCollection();
            if (state.Underline > 0 || state.LinkUrl != null) decorations.AddRange(Avalonia.Media.TextDecorations.Underline);
            if (state.Strike > 0) decorations.AddRange(Avalonia.Media.TextDecorations.Strikethrough);
            if (decorations.Count > 0) run.TextDecorations = decorations;

            if (state.LinkUrl != null && Application.Current?.TryGetResource("SystemAccentColor", Application.Current.ActualThemeVariant, out var accent) == true && accent is Color color)
            {
                run.Foreground = new SolidColorBrush(color);
            }

            inlines.Add(run);
        }
    }

    private static void TrimTrailingWhitespace(InlineCollection inlines)
    {
        while (inlines.Count > 0 && (inlines[^1] is LineBreak || (inlines[^1] is Run r && string.IsNullOrWhiteSpace(r.Text))))
        {
            inlines.RemoveAt(inlines.Count - 1);
        }
    }
}
