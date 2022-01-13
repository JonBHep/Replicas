using System;
using System.Windows;

namespace Replicas;

public partial class InputBox : Window
{
    public InputBox()
    {
        InitializeComponent();
    }
    public InputBox(string BoxTitle, string PromptText, string DefaultResponse)
    {
        InitializeComponent();
        Title = BoxTitle;
        textblockPrompt.Text = PromptText;
        textboxResponse.Text = DefaultResponse;
    }

    public string ResponseText { get { return textboxResponse.Text; } }

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
        textboxResponse.Focus();
    }
}