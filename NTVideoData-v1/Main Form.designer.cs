namespace NTVideoData
{
    partial class MainForm
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
            this.cbSelectOne = new System.Windows.Forms.ComboBox();
            this.cbVictim = new System.Windows.Forms.ComboBox();
            this.btInsert = new System.Windows.Forms.Button();
            this.btUpdate = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.uploadBt = new System.Windows.Forms.Button();
            this.downloadBt = new System.Windows.Forms.Button();
            this.pnInput = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.pnInput.SuspendLayout();
            this.SuspendLayout();
            // 
            // cbSelectOne
            // 
            this.cbSelectOne.FormattingEnabled = true;
            this.cbSelectOne.Location = new System.Drawing.Point(4, 2);
            this.cbSelectOne.Name = "cbSelectOne";
            this.cbSelectOne.Size = new System.Drawing.Size(313, 21);
            this.cbSelectOne.TabIndex = 0;
            this.cbSelectOne.Text = "Select one";
            this.cbSelectOne.SelectedIndexChanged += new System.EventHandler(this.cbSelectOne_SelectedIndexChanged);
            // 
            // cbVictim
            // 
            this.cbVictim.FormattingEnabled = true;
            this.cbVictim.Location = new System.Drawing.Point(4, 29);
            this.cbVictim.Name = "cbVictim";
            this.cbVictim.Size = new System.Drawing.Size(312, 21);
            this.cbVictim.TabIndex = 1;
            this.cbVictim.Text = "Select one victim";
            this.cbVictim.SelectedIndexChanged += new System.EventHandler(this.cbVictim_SelectedIndexChanged);
            // 
            // btInsert
            // 
            this.btInsert.Location = new System.Drawing.Point(324, 4);
            this.btInsert.Name = "btInsert";
            this.btInsert.Size = new System.Drawing.Size(60, 48);
            this.btInsert.TabIndex = 2;
            this.btInsert.Text = "Insert";
            this.btInsert.UseVisualStyleBackColor = true;
            this.btInsert.Click += new System.EventHandler(this.btInsert_Click);
            // 
            // btUpdate
            // 
            this.btUpdate.Location = new System.Drawing.Point(390, 5);
            this.btUpdate.Name = "btUpdate";
            this.btUpdate.Size = new System.Drawing.Size(58, 48);
            this.btUpdate.TabIndex = 3;
            this.btUpdate.Text = "Update";
            this.btUpdate.UseVisualStyleBackColor = true;
            this.btUpdate.Click += new System.EventHandler(this.btUpdate_Click);
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.btInsert);
            this.panel1.Controls.Add(this.btUpdate);
            this.panel1.Location = new System.Drawing.Point(6, 6);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(458, 60);
            this.panel1.TabIndex = 5;
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.uploadBt);
            this.panel2.Controls.Add(this.downloadBt);
            this.panel2.Location = new System.Drawing.Point(6, 72);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(458, 59);
            this.panel2.TabIndex = 6;
            // 
            // uploadBt
            // 
            this.uploadBt.Location = new System.Drawing.Point(235, 5);
            this.uploadBt.Name = "uploadBt";
            this.uploadBt.Size = new System.Drawing.Size(213, 48);
            this.uploadBt.TabIndex = 5;
            this.uploadBt.Text = "Upload";
            this.uploadBt.UseVisualStyleBackColor = true;
            this.uploadBt.Click += new System.EventHandler(this.uploadBt_Click);
            // 
            // downloadBt
            // 
            this.downloadBt.Location = new System.Drawing.Point(6, 5);
            this.downloadBt.Name = "downloadBt";
            this.downloadBt.Size = new System.Drawing.Size(213, 48);
            this.downloadBt.TabIndex = 4;
            this.downloadBt.Text = "Download";
            this.downloadBt.UseVisualStyleBackColor = true;
            this.downloadBt.Click += new System.EventHandler(this.downloadBt_Click);
            // 
            // pnInput
            // 
            this.pnInput.Controls.Add(this.cbSelectOne);
            this.pnInput.Controls.Add(this.cbVictim);
            this.pnInput.Location = new System.Drawing.Point(9, 9);
            this.pnInput.Name = "pnInput";
            this.pnInput.Size = new System.Drawing.Size(319, 52);
            this.pnInput.TabIndex = 5;
            // 
            // MainForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(470, 136);
            this.Controls.Add(this.pnInput);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Enabled = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Main Form";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.pnInput.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox cbSelectOne;
        private System.Windows.Forms.ComboBox cbVictim;
        private System.Windows.Forms.Button btInsert;
        private System.Windows.Forms.Button btUpdate;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel pnInput;
        private System.Windows.Forms.Button downloadBt;
        private System.Windows.Forms.Button uploadBt;
    }
}

