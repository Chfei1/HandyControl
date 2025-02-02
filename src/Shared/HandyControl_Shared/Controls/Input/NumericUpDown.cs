﻿using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using HandyControl.Data;
using HandyControl.Interactivity;
using HandyControl.Tools;

namespace HandyControl.Controls;

/// <summary>
///     数值选择控件
/// </summary>
[TemplatePart(Name = ElementTextBox, Type = typeof(TextBox))]
public class NumericUpDown : Control
{
    private const string ElementTextBox = "PART_TextBox";

    private TextBox _textBox;

    public const string DoubleFixedPoint = "0.###################################################################################################################################################################################################################################################################################################################################################";

    public NumericUpDown()
    {
        CommandBindings.Add(new CommandBinding(ControlCommands.Prev, (s, e) =>
        {
            if (IsReadOnly) return;

            SetCurrentValue(ValueProperty, Value + Increment);
        }));
        CommandBindings.Add(new CommandBinding(ControlCommands.Next, (s, e) =>
        {
            if (IsReadOnly) return;

            SetCurrentValue(ValueProperty, Value - Increment);
        }));
        CommandBindings.Add(new CommandBinding(ControlCommands.Clear, (s, e) =>
        {
            if (IsReadOnly) return;

            SetCurrentValue(ValueProperty, ValueBoxes.Double0Box);
        }));
        DataObject.AddPastingHandler(this, DataObjectPasting);
    }

    public override void OnApplyTemplate()
    {
        if (_textBox != null)
        {
            _textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
            _textBox.TextChanged -= TextBox_TextChanged;
            _textBox.LostFocus -= TextBox_LostFocus;
        }

        base.OnApplyTemplate();

        _textBox = GetTemplateChild(ElementTextBox) as TextBox;

        if (_textBox != null)
        {
            _textBox.SetBinding(SelectionBrushProperty, new Binding(SelectionBrushProperty.Name) { Source = this });
#if NET48_OR_GREATER
            _textBox.SetBinding(SelectionTextBrushProperty, new Binding(SelectionTextBrushProperty.Name) { Source = this });
#endif
            _textBox.SetBinding(SelectionOpacityProperty, new Binding(SelectionOpacityProperty.Name) { Source = this });
            _textBox.SetBinding(CaretBrushProperty, new Binding(CaretBrushProperty.Name) { Source = this });

            _textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
            _textBox.TextChanged += TextBox_TextChanged;
            _textBox.PreviewTextInput += TextBox_PreviewTextInput;
            _textBox.LostFocus += TextBox_LostFocus;
            _textBox.Text = CurrentText;
        }
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_textBox.Text))
        {
            SetCurrentValue(ValueProperty, ValueBoxes.Double0Box);
        }
        else if (double.TryParse(_textBox.Text, out double value))
        {
            SetCurrentValue(ValueProperty, value);
        }
        else
        {
            SetCurrentValue(ValueProperty, Value);
        }
        UpdateText();
    }

    private void UpdateText()
    {
        if (_textBox != null)
        {
            string after = CurrentText.TrimStart('0');
            if (after.Length == 0)
            {
                after = "0";
            }
            else if (after.StartsWith("."))
            {
                after = "0" + after;
            }
            else if (after.Contains("."))
            {
                after = after.TrimEnd('0');
            }
            _textBox.Text = after;
            _textBox.Select(_textBox.Text.Length, 0);
        }
    }

    private static readonly char[] validChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '.' };
    private static readonly Regex _regex = new("[^0-9.-]+"); //regex that matches disallowed text
    private static bool IsTextAllowed(string wholeText, string newText, bool onlyInteger = false)
    {
        wholeText += newText;
        //if (wholeText.Length >= MaximumLengthProperty)
        //{
        //    return false;
        //}
        if (onlyInteger)
        {
            return int.TryParse(newText, out int _);
        }
        else
        {
            return !_regex.IsMatch(newText) /*&& double.TryParse(wholeText, out double _)*/;
        }
    }

    private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        //e.Handled = new Regex("[^0-9|\\.]").IsMatch(e.Text);
        e.Handled = !IsTextAllowed(_textBox.Text, e.Text, DecimalPlaces == 0);
        if (e.Text == ".")
        {
            e.Handled = _textBox.Text.Contains(e.Text);
        }
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (double.TryParse(_textBox.Text, out double value))
        {
            if (value >= Minimum && value <= Maximum && _textBox.Text.Length < MaximumLength)
            {
                string text = _textBox.Text;
                if (text.Contains("."))
                {
                    if (text.EndsWith("."))
                    {

                    }
                    else
                    {
                        if (text.EndsWith("0"))
                        {

                        }
                        else
                        {
                            SetCurrentValue(ValueProperty, value);
                        }
                    }
                }
                else if (text.Length != 1 && text.StartsWith("0"))
                {

                }
                else
                {
                    SetCurrentValue(ValueProperty, value);
                }
            }
            else
            {
                SetCurrentValue(ValueProperty, Value);//assign current value to update it
            }
        }

        //if (string.IsNullOrWhiteSpace(_textBox.Text))
        //{
        //    SetCurrentValue(ValueProperty, ValueBoxes.Double0Box);
        //}

    }

    protected virtual void DataObjectPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            string text = (string) e.DataObject.GetData(typeof(string));
            if (!IsTextAllowed(_textBox.Text, text, DecimalPlaces == 0))
            {
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
    }

    private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            e.Handled = true;
        }

        if (IsReadOnly) return;

        if (e.Key == Key.Up)
        {
            Value += Increment;
        }
        else if (e.Key == Key.Down)
        {
            Value -= Increment;
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (_textBox.IsFocused && !IsReadOnly)
        {
            Value += e.Delta > 0 ? Increment : -Increment;
            e.Handled = true;
        }
    }

    private string CurrentText
    {
        get
        {
            if (double.TryParse(_textBox.Text, out double number))
            {
                if (number >= Minimum && number <= Maximum)
                {
                    if (DecimalPlaces.HasValue)
                    {
                        return Value.ToString($"#0.{new string('0', DecimalPlaces.Value)}");
                    }
                    else
                    {
                        return _textBox.Text;
                    }
                }
                else
                {
                    if (number < Minimum)
                    {
                        return Minimum.ToString();
                    }
                    else
                    {
                        return Maximum.ToString();
                    }
                }
            }
            else
            {
                if (DecimalPlaces.HasValue)
                {
                    if (string.IsNullOrWhiteSpace(ValueFormat))
                    {
                        return Value.ToString($"#0.{new string('0', DecimalPlaces.Value)}");
                    }
                    else
                    {
                        return Value.ToString(ValueFormat);
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(ValueFormat))
                    {
                        return Value.ToString();
                    }
                    else
                    {
                        return Value.ToString(ValueFormat);
                    }
                }
            }
        }
    }

    private string CurrentTextByValue
    {
        get
        {
            if (DecimalPlaces.HasValue)
            {
                if (string.IsNullOrWhiteSpace(ValueFormat))
                {
                    return Value.ToString($"#0.{new string('0', DecimalPlaces.Value)}");
                }
                else
                {
                    return Value.ToString(ValueFormat);
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(ValueFormat))
                {
                    return Value.ToString(DoubleFixedPoint);
                }
                else
                {
                    return Value.ToString(ValueFormat);
                }
            }
        }
    }

    protected virtual void OnValueChanged(FunctionEventArgs<double> e)
    {
        RaiseEvent(e);
    }

    /// <summary>
    ///     值改变事件
    /// </summary>
    public static readonly RoutedEvent ValueChangedEvent =
        EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble,
            typeof(EventHandler<FunctionEventArgs<double>>), typeof(NumericUpDown));

    /// <summary>
    ///     值改变事件
    /// </summary>
    public event EventHandler<FunctionEventArgs<double>> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>
    ///     当前值
    /// </summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(NumericUpDown),
        new FrameworkPropertyMetadata(ValueBoxes.Double0Box, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnValueChanged, CoerceValue), ValidateHelper.IsInRangeOfDouble);

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (NumericUpDown) d;
        var v = (double) e.NewValue;
        ctl.SetTextByValue();

        ctl.OnValueChanged(new FunctionEventArgs<double>(ValueChangedEvent, ctl)
        {
            Info = v
        });
    }

    private void SetText()
    {
        if (_textBox != null)
        {
            _textBox.Text = CurrentText;
            _textBox.Select(_textBox.Text.Length, 0);
        }
    }

    private void SetTextByValue()
    {
        if (_textBox != null)
        {
            _textBox.Text = CurrentTextByValue;
            _textBox.Select(_textBox.Text.Length, 0);
        }
    }

    private static object CoerceValue(DependencyObject d, object basevalue)
    {
        var ctl = (NumericUpDown) d;
        var minimum = ctl.Minimum;
        var num = (double) basevalue;
        if (num < minimum)
        {
            ctl.Value = minimum;
            return minimum;
        }
        var maximum = ctl.Maximum;
        if (num > maximum)
        {
            ctl.Value = maximum;
        }
        ctl.SetTextByValue();
        return num > maximum ? maximum : num;
    }

    /// <summary>
    ///     当前值
    /// </summary>
    public double Value
    {
        get => (double) GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    ///     最大长度值
    /// </summary>
    public static readonly DependencyProperty MaximumLengthProperty = DependencyProperty.Register(
        nameof(MaximumLength), typeof(int), typeof(NumericUpDown), new PropertyMetadata(20, OnMaximumLengthChanged));

    private static void OnMaximumLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (NumericUpDown) d;
        ctl.CoerceValue(MaximumLengthProperty);
        ctl.CoerceValue(ValueProperty);
    }

    public int MaximumLength
    {
        get => (int) GetValue(MaximumLengthProperty);
        set => SetValue(MaximumLengthProperty, value);
    }

    /// <summary>
    ///     最大值
    /// </summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(NumericUpDown), new PropertyMetadata(double.MaxValue, OnMaximumChanged, CoerceMaximum), ValidateHelper.IsInRangeOfDouble);

    private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (NumericUpDown) d;
        ctl.CoerceValue(MinimumProperty);
        ctl.CoerceValue(ValueProperty);
    }

    private static object CoerceMaximum(DependencyObject d, object basevalue)
    {
        var minimum = ((NumericUpDown) d).Minimum;
        return (double) basevalue < minimum ? minimum : basevalue;
    }

    /// <summary>
    ///     最大值
    /// </summary>
    public double Maximum
    {
        get => (double) GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>
    ///     最小值
    /// </summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(NumericUpDown), new PropertyMetadata(double.MinValue, OnMinimumChanged, CoerceMinimum), ValidateHelper.IsInRangeOfDouble);

    private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (NumericUpDown) d;
        ctl.CoerceValue(MaximumProperty);
        ctl.CoerceValue(ValueProperty);
    }

    private static object CoerceMinimum(DependencyObject d, object basevalue)
    {
        var maximum = ((NumericUpDown) d).Maximum;
        return (double) basevalue > maximum ? maximum : basevalue;
    }

    /// <summary>
    ///     最小值
    /// </summary>
    public double Minimum
    {
        get => (double) GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>
    ///     指示每单击一下按钮时增加或减少的数量
    /// </summary>
    public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(
        nameof(Increment), typeof(double), typeof(NumericUpDown), new PropertyMetadata(ValueBoxes.Double1Box));

    /// <summary>
    ///     指示每单击一下按钮时增加或减少的数量
    /// </summary>
    public double Increment
    {
        get => (double) GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    /// <summary>
    ///     指示要显示的小数位数
    /// </summary>
    public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(
        nameof(DecimalPlaces), typeof(int?), typeof(NumericUpDown), new PropertyMetadata(default(int?)));

    /// <summary>
    ///     指示要显示的小数位数
    /// </summary>
    public int? DecimalPlaces
    {
        get => (int?) GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    /// <summary>
    ///     指示要显示的数字的格式
    /// </summary>
    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(NumericUpDown), new PropertyMetadata(default(string)));

    /// <summary>
    ///     指示要显示的数字的格式，这将会覆盖 <see cref="DecimalPlaces"/> 属性
    /// </summary>
    public string ValueFormat
    {
        get => (string) GetValue(ValueFormatProperty);
        set => SetValue(ValueFormatProperty, value);
    }

    /// <summary>
    ///     是否显示上下调值按钮
    /// </summary>
    public static readonly DependencyProperty ShowUpDownButtonProperty = DependencyProperty.Register(
        nameof(ShowUpDownButton), typeof(bool), typeof(NumericUpDown), new PropertyMetadata(ValueBoxes.TrueBox));

    /// <summary>
    ///     是否显示上下调值按钮
    /// </summary>
    public bool ShowUpDownButton
    {
        get => (bool) GetValue(ShowUpDownButtonProperty);
        set => SetValue(ShowUpDownButtonProperty, ValueBoxes.BooleanBox(value));
    }

    /// <summary>
    ///     标识 IsReadOnly 依赖属性。
    /// </summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(NumericUpDown), new PropertyMetadata(ValueBoxes.FalseBox));

    /// <summary>
    ///     获取或设置一个值，该值指示NumericUpDown是否只读。
    /// </summary>
    public bool IsReadOnly
    {
        get => (bool) GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, ValueBoxes.BooleanBox(value));
    }

    public static readonly DependencyProperty SelectionBrushProperty =
        TextBoxBase.SelectionBrushProperty.AddOwner(typeof(NumericUpDown));

    public Brush SelectionBrush
    {
        get => (Brush) GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

#if NET48_OR_GREATER

    public static readonly DependencyProperty SelectionTextBrushProperty =
        TextBoxBase.SelectionTextBrushProperty.AddOwner(typeof(NumericUpDown));

    public Brush SelectionTextBrush
    {
        get => (Brush) GetValue(SelectionTextBrushProperty);
        set => SetValue(SelectionTextBrushProperty, value);
    }

#endif

    public static readonly DependencyProperty SelectionOpacityProperty =
        TextBoxBase.SelectionOpacityProperty.AddOwner(typeof(NumericUpDown));

    public double SelectionOpacity
    {
        get => (double) GetValue(SelectionOpacityProperty);
        set => SetValue(SelectionOpacityProperty, value);
    }

    public static readonly DependencyProperty CaretBrushProperty =
        TextBoxBase.CaretBrushProperty.AddOwner(typeof(NumericUpDown));

    public Brush CaretBrush
    {
        get => (Brush) GetValue(CaretBrushProperty);
        set => SetValue(CaretBrushProperty, value);
    }
}
