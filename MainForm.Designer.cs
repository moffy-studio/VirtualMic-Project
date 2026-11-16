namespace VirtualMicMixer
{
    // NOTE: The real UI is built in MainForm.Layout.cs.
    // Keeping this file minimal means the Visual Studio designer cannot wipe
    // the hand-written layout when the form is opened in design view.
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.Name = "MainForm";
            this.ResumeLayout(false);
        }
    }
}
