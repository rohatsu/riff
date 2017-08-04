namespace RIFF.Service
{
    partial class RFServiceHost
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                if(_serviceHost != null)
                {
                    _serviceHost.Close();
                }
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.rfEventLog = new System.Diagnostics.EventLog();
            ((System.ComponentModel.ISupportInitialize)(this.rfEventLog)).BeginInit();
            // 
            // RFService
            // 
            this.ServiceName = "RIFF.Service";
            ((System.ComponentModel.ISupportInitialize)(this.rfEventLog)).EndInit();

        }

        #endregion

        private System.Diagnostics.EventLog rfEventLog;
    }
}
