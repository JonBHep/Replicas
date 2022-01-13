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
        this.Title = txt;
    }

    public void SetComment(string txt)
    {
        this.lblMessage.Text = txt;
    }

    public void AddToList(string txt)
    {
        this.lstExamples.Items.Add(txt);
    }
}