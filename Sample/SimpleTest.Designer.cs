﻿namespace Sample
{
    partial class SimpleTest
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
            this.cmdDetectDevices = new System.Windows.Forms.Button();
            this.log = new System.Windows.Forms.TextBox();
            this.cmdReadVoltage = new System.Windows.Forms.Button();
            this.txtVoltage = new System.Windows.Forms.TextBox();
            this.cmdReadVin = new System.Windows.Forms.Button();
            this.txtReadVin = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.checkBoxLogJ2534 = new System.Windows.Forms.CheckBox();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.textBoxVolts = new System.Windows.Forms.TextBox();
            this.button5 = new System.Windows.Forms.Button();
            this.ignoreProrgammingVoltageCheckBox = new System.Windows.Forms.CheckBox();
            this.autoDetectCheckBox = new System.Windows.Forms.CheckBox();
            this.button6 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // cmdDetectDevices
            // 
            this.cmdDetectDevices.Location = new System.Drawing.Point(13, 13);
            this.cmdDetectDevices.Name = "cmdDetectDevices";
            this.cmdDetectDevices.Size = new System.Drawing.Size(154, 23);
            this.cmdDetectDevices.TabIndex = 0;
            this.cmdDetectDevices.Text = "Detect J2534 Devices";
            this.cmdDetectDevices.UseVisualStyleBackColor = true;
            this.cmdDetectDevices.Click += new System.EventHandler(this.CmdDetectDevicesClick);
            // 
            // log
            // 
            this.log.Location = new System.Drawing.Point(173, 15);
            this.log.Multiline = true;
            this.log.Name = "log";
            this.log.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.log.Size = new System.Drawing.Size(791, 160);
            this.log.TabIndex = 1;
            // 
            // cmdReadVoltage
            // 
            this.cmdReadVoltage.Location = new System.Drawing.Point(12, 188);
            this.cmdReadVoltage.Name = "cmdReadVoltage";
            this.cmdReadVoltage.Size = new System.Drawing.Size(155, 23);
            this.cmdReadVoltage.TabIndex = 2;
            this.cmdReadVoltage.Text = "Read Voltage";
            this.cmdReadVoltage.UseVisualStyleBackColor = true;
            this.cmdReadVoltage.Click += new System.EventHandler(this.CmdReadVoltageClick);
            // 
            // txtVoltage
            // 
            this.txtVoltage.Location = new System.Drawing.Point(173, 190);
            this.txtVoltage.Name = "txtVoltage";
            this.txtVoltage.Size = new System.Drawing.Size(429, 20);
            this.txtVoltage.TabIndex = 3;
            // 
            // cmdReadVin
            // 
            this.cmdReadVin.Location = new System.Drawing.Point(12, 217);
            this.cmdReadVin.Name = "cmdReadVin";
            this.cmdReadVin.Size = new System.Drawing.Size(155, 23);
            this.cmdReadVin.TabIndex = 4;
            this.cmdReadVin.Text = "Read VIN";
            this.cmdReadVin.UseVisualStyleBackColor = true;
            this.cmdReadVin.Click += new System.EventHandler(this.ReadVinClick);
            // 
            // txtReadVin
            // 
            this.txtReadVin.Location = new System.Drawing.Point(174, 219);
            this.txtReadVin.Name = "txtReadVin";
            this.txtReadVin.Size = new System.Drawing.Size(428, 20);
            this.txtReadVin.TabIndex = 5;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(13, 246);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(154, 23);
            this.button1.TabIndex = 6;
            this.button1.Text = "Send Recieve Test";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(13, 275);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(155, 23);
            this.button2.TabIndex = 7;
            this.button2.Text = "Read Flash";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.ReadFlash_Click);
            // 
            // checkBoxLogJ2534
            // 
            this.checkBoxLogJ2534.AutoSize = true;
            this.checkBoxLogJ2534.Location = new System.Drawing.Point(13, 43);
            this.checkBoxLogJ2534.Name = "checkBoxLogJ2534";
            this.checkBoxLogJ2534.Size = new System.Drawing.Size(120, 17);
            this.checkBoxLogJ2534.TabIndex = 8;
            this.checkBoxLogJ2534.Text = "Log J2534 API calls";
            this.checkBoxLogJ2534.UseVisualStyleBackColor = true;
            this.checkBoxLogJ2534.CheckStateChanged += new System.EventHandler(this.checkBoxLogJ2534_CheckStateChanged);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(13, 304);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(155, 23);
            this.button3.TabIndex = 9;
            this.button3.Text = "Request Download";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.RequestDownload_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(13, 333);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(155, 23);
            this.button4.TabIndex = 10;
            this.button4.Text = "Toggle Pin 13";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.setProgrammingVoltage);
            // 
            // textBoxVolts
            // 
            this.textBoxVolts.Location = new System.Drawing.Point(174, 333);
            this.textBoxVolts.Name = "textBoxVolts";
            this.textBoxVolts.Size = new System.Drawing.Size(77, 20);
            this.textBoxVolts.TabIndex = 11;
            this.textBoxVolts.Text = "18000";
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(13, 362);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(155, 23);
            this.button5.TabIndex = 12;
            this.button5.Text = "Disable Pin 13";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.SetVoltage_Click);
            // 
            // ignoreProrgammingVoltageCheckBox
            // 
            this.ignoreProrgammingVoltageCheckBox.AutoSize = true;
            this.ignoreProrgammingVoltageCheckBox.Location = new System.Drawing.Point(174, 275);
            this.ignoreProrgammingVoltageCheckBox.Name = "ignoreProrgammingVoltageCheckBox";
            this.ignoreProrgammingVoltageCheckBox.Size = new System.Drawing.Size(204, 17);
            this.ignoreProrgammingVoltageCheckBox.TabIndex = 13;
            this.ignoreProrgammingVoltageCheckBox.Text = "Ignore Incorrect Programming Voltage";
            this.ignoreProrgammingVoltageCheckBox.UseVisualStyleBackColor = true;
            // 
            // autoDetectCheckBox
            // 
            this.autoDetectCheckBox.AutoSize = true;
            this.autoDetectCheckBox.Checked = true;
            this.autoDetectCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoDetectCheckBox.Location = new System.Drawing.Point(12, 66);
            this.autoDetectCheckBox.Name = "autoDetectCheckBox";
            this.autoDetectCheckBox.Size = new System.Drawing.Size(83, 17);
            this.autoDetectCheckBox.TabIndex = 14;
            this.autoDetectCheckBox.Text = "Auto Detect";
            this.autoDetectCheckBox.UseVisualStyleBackColor = true;
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(12, 89);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(155, 23);
            this.button6.TabIndex = 15;
            this.button6.Text = "Change J2534 Device";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.ChangeDevice_Click);
            // 
            // SimpleTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(976, 433);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.autoDetectCheckBox);
            this.Controls.Add(this.ignoreProrgammingVoltageCheckBox);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.textBoxVolts);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.checkBoxLogJ2534);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.txtReadVin);
            this.Controls.Add(this.cmdReadVin);
            this.Controls.Add(this.txtVoltage);
            this.Controls.Add(this.cmdReadVoltage);
            this.Controls.Add(this.log);
            this.Controls.Add(this.cmdDetectDevices);
            this.Name = "SimpleTest";
            this.Text = "J2534DotNet Sample";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdDetectDevices;
        private System.Windows.Forms.TextBox log;
        private System.Windows.Forms.Button cmdReadVoltage;
        private System.Windows.Forms.TextBox txtVoltage;
        private System.Windows.Forms.Button cmdReadVin;
        private System.Windows.Forms.TextBox txtReadVin;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox checkBoxLogJ2534;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.TextBox textBoxVolts;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.CheckBox ignoreProrgammingVoltageCheckBox;
        private System.Windows.Forms.CheckBox autoDetectCheckBox;
        private System.Windows.Forms.Button button6;
    }
}

