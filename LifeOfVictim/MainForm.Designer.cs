namespace LifeOfVictim
{
    partial class LifeOfVictim
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
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.processingForLiveBt = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // processingForLiveBt
            // 
            this.processingForLiveBt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.processingForLiveBt.Location = new System.Drawing.Point(0, 0);
            this.processingForLiveBt.Name = "processingForLiveBt";
            this.processingForLiveBt.Size = new System.Drawing.Size(279, 61);
            this.processingForLiveBt.TabIndex = 0;
            this.processingForLiveBt.Text = "Begin Life";
            this.processingForLiveBt.UseVisualStyleBackColor = true;
            this.processingForLiveBt.Click += new System.EventHandler(this.processingForLiveBt_Click);
            // 
            // LifeOfVictim
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(279, 61);
            this.Controls.Add(this.processingForLiveBt);
            this.Name = "LifeOfVictim";
            this.Text = "Life Of Victim";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button processingForLiveBt;
    }
}

