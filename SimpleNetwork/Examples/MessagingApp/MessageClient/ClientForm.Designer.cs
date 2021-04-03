
namespace MessageClient
{
    partial class ClientForm
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
            this.SubmitButton = new System.Windows.Forms.Button();
            this.MessagingBox = new System.Windows.Forms.RichTextBox();
            this.SendText = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // SubmitButton
            // 
            this.SubmitButton.Enabled = false;
            this.SubmitButton.Location = new System.Drawing.Point(12, 437);
            this.SubmitButton.Name = "SubmitButton";
            this.SubmitButton.Size = new System.Drawing.Size(318, 47);
            this.SubmitButton.TabIndex = 0;
            this.SubmitButton.Text = "SUBMIT";
            this.SubmitButton.UseVisualStyleBackColor = true;
            this.SubmitButton.Click += new System.EventHandler(this.SubmitButton_Click);
            // 
            // MessagingBox
            // 
            this.MessagingBox.Enabled = false;
            this.MessagingBox.Location = new System.Drawing.Point(12, 12);
            this.MessagingBox.Name = "MessagingBox";
            this.MessagingBox.ReadOnly = true;
            this.MessagingBox.Size = new System.Drawing.Size(318, 391);
            this.MessagingBox.TabIndex = 1;
            this.MessagingBox.Text = "";
            // 
            // SendText
            // 
            this.SendText.Location = new System.Drawing.Point(12, 409);
            this.SendText.Name = "SendText";
            this.SendText.Size = new System.Drawing.Size(318, 22);
            this.SendText.TabIndex = 2;
            this.SendText.TextChanged += new System.EventHandler(this.SendText_TextChanged);
            // 
            // ClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(346, 495);
            this.Controls.Add(this.SendText);
            this.Controls.Add(this.MessagingBox);
            this.Controls.Add(this.SubmitButton);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(364, 542);
            this.MinimumSize = new System.Drawing.Size(364, 542);
            this.Name = "ClientForm";
            this.Text = "Nerd Chat";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button SubmitButton;
        private System.Windows.Forms.RichTextBox MessagingBox;
        private System.Windows.Forms.TextBox SendText;
    }
}

