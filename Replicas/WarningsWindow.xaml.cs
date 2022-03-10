namespace Replicas;

internal partial class WarningsWindow
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
        LblMessage.Text = txt;
    }
    
}