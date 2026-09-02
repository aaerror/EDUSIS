using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WPF_Desktop.Components;

public partial class PasswordComponent : UserControl
{
    private bool _isChanging;
    public static readonly DependencyProperty ClaveProperty =
        DependencyProperty.Register("Clave",
                                    typeof(SecureString),
                                    typeof(PasswordComponent),
                                    new FrameworkPropertyMetadata(defaultValue: null,
                                                                  flags: FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                                                  propertyChangedCallback: PasswordPropertyChangedCallback,
                                                                  coerceValueCallback: null,
                                                                  isAnimationProhibited: false,
                                                                  defaultUpdateSourceTrigger: UpdateSourceTrigger.PropertyChanged));


    public PasswordComponent()
    {
        InitializeComponent();
    }

    public SecureString Clave
    {
        get
        {
            return (SecureString) GetValue(ClaveProperty);
        }

        set
        {
            SetValue(ClaveProperty, value);
        }
    }

    private static void PasswordPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PasswordComponent passwordComponent)
        {
            passwordComponent.RefreshPasswordControl();
        }
    }

    private void Password_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _isChanging = true;
        Clave = PasswordBoxControl.SecurePassword;
        _isChanging = false;
    }

    private void RefreshPasswordControl()
    {
        if (!_isChanging)
        {
            PasswordBoxControl.Password = Clave.ToString();
        }
    }
}
