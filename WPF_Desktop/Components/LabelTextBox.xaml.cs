using System.Windows;
using System.Windows.Controls;

namespace WPF_Desktop.Components;

public partial class LabelTextBox : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register("Label", typeof(string), typeof(LabelTextBox), new PropertyMetadata("Label"));

    public static readonly DependencyProperty TextBoxProperty = DependencyProperty.Register("Text", typeof(string), typeof(LabelTextBox), new PropertyMetadata("Text"));

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register("Placeholder", typeof(string), typeof(LabelTextBox), new PropertyMetadata("Placeholder"));


    public LabelTextBox()
    {
        InitializeComponent();
    }

    public string Label
    {
        get { return (string) GetValue(LabelProperty); }
        set { SetValue(LabelProperty, value); }
    }

    public string Text
    {
        get { return (string) GetValue(TextBoxProperty); }
        set { SetValue(TextBoxProperty, value); }
    }

    public string Placeholder
    {
        get { return (string)GetValue(TagProperty); }
        set { SetValue(TagProperty, value); }
    }
}
