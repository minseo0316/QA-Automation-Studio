using System.Windows.Forms;

namespace MyWinFormsApp.Controls;

internal class DarkLogTextBox : TextBox
{
    public DarkLogTextBox()
    {
        Multiline = true;
        ReadOnly = true;
        ScrollBars = ScrollBars.None;
        BorderStyle = BorderStyle.None;
        BackColor = UiTheme.LogBackground;
        ForeColor = Color.FromArgb(226, 232, 240);
        Font = UiTheme.LogFont;
        WordWrap = false;
    }
}
