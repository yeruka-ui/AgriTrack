using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace agrify;

public sealed partial class MainPage : Page
{

        private SolidColorBrush _buttonBrush;

    public MainPage()
    {
        this.InitializeComponent();

        LoginButton.Loaded += LoginButton_Loaded;

       
    }

    private void LoginButton_Loaded(object sender, RoutedEventArgs e)
    {
        // Get the RootGrid inside the template
        var rootGrid = (Grid)LoginButton.GetTemplateChild("RootGrid");
        if (rootGrid != null)
        {
            _buttonBrush = rootGrid.Background as SolidColorBrush;

            // Attach hover events
            LoginButton.PointerEntered += LoginButton_PointerEntered;
            LoginButton.PointerExited += LoginButton_PointerExited;
        }
    }
    private void LoginButton_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_buttonBrush != null)
            _buttonBrush.Color = Color.FromArgb(255, 0, 128, 51); // lighter green
        LoginButton.Foreground = new SolidColorBrush(Colors.White);
    }

    private void LoginButton_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_buttonBrush != null)
            _buttonBrush.Color = Color.FromArgb(255, 0, 111, 49); // normal green
        LoginButton.Foreground = new SolidColorBrush(Colors.White);
    }

}
