namespace MyWinFormsApp;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(18, 18, 20);
        ClientSize = new Size(860, 620);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        MinimumSize = new Size(720, 560);
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "QA Automation Studio";
        ResumeLayout(false);
    }

    #endregion
}
