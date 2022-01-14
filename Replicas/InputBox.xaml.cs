using System;
using System.Windows;

namespace Replicas;

public partial class InputBox
{
    public InputBox()
    {
        InitializeComponent();
    }
    public InputBox(string boxTitle, string promptText, string defaultResponse)
    {
        InitializeComponent();
        Title = boxTitle;
        TextBlockPrompt.Text = promptText;
        TextBoxResponse.Text = defaultResponse;
    }

    public string ResponseText => TextBoxResponse.Text;

    private void buttonOkay_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void buttonCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        Icon = Owner.Icon;
        TextBoxResponse.Focus();
    }
}