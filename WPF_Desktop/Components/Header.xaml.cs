using System.Windows;
using System.Windows.Controls;

namespace WPF_Desktop.Components;

public partial class Header : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(Header), new PropertyMetadata("Title"));
    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(Header), new PropertyMetadata("Subtitle"));


    public Header()
    {
        InitializeComponent();
    }

    public string Title
    {
        get
        {
            return (string) GetValue(TitleProperty);
        }

        set
        {
            SetValue(TitleProperty, value);
        }
    }

    public string Subtitle
    {
        get
        {
            return (string) GetValue(SubtitleProperty);
        }

        set
        {
            SetValue(SubtitleProperty, value);
        }
    }
}
