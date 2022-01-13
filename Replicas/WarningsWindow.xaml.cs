using System.Windows;

namespace Replicas;

internal partial class WarningsWindow : Window
{
    internal WarningsWindow()
    {
        InitializeComponent();
    }
    public void SetCaption(string txt)
    {
        Title = txt;
    }

    public void SetComment(string txt)
    {
        lblMessage.Text = txt;
    }

    public void AddToList(string txt)
    {
        lstExamples.Items.Add(txt);
    }
}