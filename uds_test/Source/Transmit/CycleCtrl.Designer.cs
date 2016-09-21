namespace User_Control
{
    partial class CycleCtrl
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.checkBoxEnable = new System.Windows.Forms.CheckBox();
            this.textBoxId = new System.Windows.Forms.TextBox();
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.textBoxDlc = new System.Windows.Forms.TextBox();
            this.textBoxData7 = new System.Windows.Forms.TextBox();
            this.textBoxIntTime = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxData2 = new System.Windows.Forms.TextBox();
            this.textBoxData5 = new System.Windows.Forms.TextBox();
            this.textBoxData3 = new System.Windows.Forms.TextBox();
            this.textBoxData4 = new System.Windows.Forms.TextBox();
            this.textBoxData0 = new System.Windows.Forms.TextBox();
            this.textBoxData1 = new System.Windows.Forms.TextBox();
            this.textBoxData6 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // checkBoxEnable
            // 
            this.checkBoxEnable.AutoSize = true;
            this.checkBoxEnable.Location = new System.Drawing.Point(7, 5);
            this.checkBoxEnable.Name = "checkBoxEnable";
            this.checkBoxEnable.Size = new System.Drawing.Size(15, 14);
            this.checkBoxEnable.TabIndex = 0;
            this.checkBoxEnable.TabStop = false;
            this.checkBoxEnable.UseVisualStyleBackColor = true;
            this.checkBoxEnable.CheckedChanged += new System.EventHandler(this.textBoxAll_TextChanged);
            // 
            // textBoxId
            // 
            this.textBoxId.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxId.ContextMenuStrip = this.contextMenuStrip;
            this.textBoxId.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.textBoxId.Location = new System.Drawing.Point(30, 2);
            this.textBoxId.MaxLength = 3;
            this.textBoxId.Name = "textBoxId";
            this.textBoxId.Size = new System.Drawing.Size(24, 21);
            this.textBoxId.TabIndex = 1;
            this.textBoxId.Text = "000";
            this.textBoxId.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxId.TextChanged += new System.EventHandler(this.textBoxAll_TextChanged);
            this.textBoxId.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxHex_KeyPress);
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(61, 4);
            // 
            // textBoxDlc
            // 
            this.textBoxDlc.ContextMenuStrip = this.contextMenuStrip;
            this.textBoxDlc.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.textBoxDlc.Location = new System.Drawing.Point(68, 2);
            this.textBoxDlc.MaxLength = 1;
            this.textBoxDlc.Name = "textBoxDlc";
            this.textBoxDlc.Size = new System.Drawing.Size(14, 21);
            this.textBoxDlc.TabIndex = 2;
            this.textBoxDlc.Text = "8";
            this.textBoxDlc.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBoxDlc.TextChanged += new System.EventHandler(this.textBoxAll_TextChanged);
            this.textBoxDlc.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxValue_KeyPress);
            // 
            // textBoxData7
            // 
            this.textBoxData7.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxData7.ContextMenuStrip = this.contextMenuStrip;
            this.textBoxData7.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.textBoxData7.Location = new System.Drawing.Point(301, 2);
            this.textBoxData7.MaxLength = 2;
            this.textBoxData7.Name = "textBoxData7";
            this.textBoxData7.Size = new System.Drawing.Size(19, 21);
            this.textBoxData7.TabIndex = 10;
            this.textBoxData7.Text = "00";
            this.textBoxData7.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxData7.TextChanged += new System.EventHandler(this.textBoxAll_TextChanged);
            this.textBoxData7.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxHex_KeyPress);
            // 
            // textBoxIntTime
            // 
            this.textBoxIntTime.AllowDrop = true;
            this.textBoxIntTime.ContextMenuStrip = this.contextMenuStrip;
            this.textBoxIntTime.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.textBoxIntTime.Location = new System.Drawing.Point(339, 2);
            this.textBoxIntTime.MaxLength = 4;
            this.textBoxIntTime.Name = "textBoxIntTime";
            this.textBoxIntTime.Size = new System.Drawing.Size(34, 21);
            this.textBoxIntTime.TabIndex = 11;
            this.textBoxIntTime.Text = "100";
            this.textBoxIntTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxIntTime.TextChanged += new System.EventHandler(this.textBoxAll_TextChanged);
            this.textBoxIntTime.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxValue_KeyPress);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(378, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(17, 12);
            this.label1.TabIndex = 12;
            this.label1.Text = "ms";
            // 
            // textBoxData2
            // 
            this.textBoxData2.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxData2.ContextMenuStrip = this.contextMenuStrip;
            this.textBoxData2.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.textBoxData2.Location = new System.Drawing.Point(156, 2);
            this.textBoxData2.MaxLength = 2;
            this.textBoxData2.Name = "textBoxData2";
            this.textBoxData2.Size = new System.Drawing.Size(19, 21);
            this.textBoxData2.TabIndex = 5;
            this.textBoxData2.Text = "00";
            this.textBoxData2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxData2.TextChanged += new System.EventHandler(this.textBoxAll_TextChanged);
            this.textBoxData2.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxHex_KeyPress);
            // 
            // textBoxData5
            // 
            this.textBoxData5.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxData5.ContextMenuStrip = this.contextMenuStrip;
            this.textBoxData5.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.textBoxData5.Location = new System.Drawing.Point(243, 2);
            this.textBoxData5.MaxLength = 2;
            this.textBoxData5.Name = "textBoxData5";
            this.textBoxData5.Size = new System.Drawing.Size(19, 21);
            this.textBoxData5.TabIndex = 8;
            this.textBoxData5.Text = "00";
            this.textBoxData5.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxData5.TextChanged += new System.EventHandler(this.textBoxAll_TextChanged);
            this.textBoxData5.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxHex_KeyPress);
            // 
            // textBoxData3
            // 
            this.textBoxData3.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxData3.ContextMenuStrip = this.contextMenuStrip;
            this.textBoxData3.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.textBoxData3.Location = new System.Drawing.Point(185, 2);
            this.textBoxData3.MaxLength = 2;
            this.textBoxData3.Name = "textBoxData3";
            this.textBoxData3.Size = new System.Drawing.Size(19, 21);
            this.textBoxData3.TabIndex = 6;
            this.textBoxData3.Text = "00";
            this.textBoxData3.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxData3.TextChanged += new System.EventHandler(this.textBoxAll_TextChanged);
            this.textBoxData3.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxHex_KeyPress);
            // 
            // textBoxData4
            // 
            this.textBoxData4.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxData4.ContextMenuStrip = this.contextMenuStrip;
            this.textBoxData4.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.textBoxData4.Location = new System.Drawing.Point(214, 2);
            this.textBoxData4.MaxLength = 2;
            this.textBoxData4.Name = "textBoxData4";
            this.textBoxData4.Size = new System.Drawing.Size(19, 21);
            this.textBoxData4.TabIndex = 7;
            this.textBoxData4.Text = "00";
            this.textBoxData4.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxData4.TextChanged += new System.EventHandler(this.textBoxAll_TextChanged);
            this.textBoxData4.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxHex_KeyPress);
            // 
            // textBoxData0
            // 
            this.textBoxData0.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxData0.ContextMenuStrip = this.contextMenuStrip;
            this.textBoxData0.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.textBoxData0.Location = new System.Drawing.Point(98, 2);
            this.textBoxData0.MaxLength = 2;
            this.textBoxData0.Name = "textBoxData0";
            this.textBoxData0.Size = new System.Drawing.Size(19, 21);
            this.textBoxData0.TabIndex = 3;
            this.textBoxData0.Text = "00";
            this.textBoxData0.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxData0.TextChanged += new System.EventHandler(this.textBoxAll_TextChanged);
            this.textBoxData0.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxHex_KeyPress);
            // 
            // textBoxData1
            // 
            this.textBoxData1.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxData1.ContextMenuStrip = this.contextMenuStrip;
            this.textBoxData1.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.textBoxData1.Location = new System.Drawing.Point(127, 2);
            this.textBoxData1.MaxLength = 2;
            this.textBoxData1.Name = "textBoxData1";
            this.textBoxData1.Size = new System.Drawing.Size(19, 21);
            this.textBoxData1.TabIndex = 4;
            this.textBoxData1.Text = "00";
            this.textBoxData1.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxData1.TextChanged += new System.EventHandler(this.textBoxAll_TextChanged);
            this.textBoxData1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxHex_KeyPress);
            // 
            // textBoxData6
            // 
            this.textBoxData6.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxData6.ContextMenuStrip = this.contextMenuStrip;
            this.textBoxData6.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.textBoxData6.Location = new System.Drawing.Point(272, 2);
            this.textBoxData6.MaxLength = 2;
            this.textBoxData6.Name = "textBoxData6";
            this.textBoxData6.Size = new System.Drawing.Size(19, 21);
            this.textBoxData6.TabIndex = 9;
            this.textBoxData6.Text = "00";
            this.textBoxData6.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxData6.TextChanged += new System.EventHandler(this.textBoxAll_TextChanged);
            this.textBoxData6.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxHex_KeyPress);
            // 
            // CycleCtrl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.textBoxData6);
            this.Controls.Add(this.textBoxData1);
            this.Controls.Add(this.textBoxData0);
            this.Controls.Add(this.textBoxData4);
            this.Controls.Add(this.textBoxData3);
            this.Controls.Add(this.textBoxData5);
            this.Controls.Add(this.textBoxData2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxIntTime);
            this.Controls.Add(this.textBoxData7);
            this.Controls.Add(this.textBoxDlc);
            this.Controls.Add(this.textBoxId);
            this.Controls.Add(this.checkBoxEnable);
            this.Name = "CycleCtrl";
            this.Size = new System.Drawing.Size(396, 24);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxEnable;
        private System.Windows.Forms.TextBox textBoxId;
        private System.Windows.Forms.TextBox textBoxDlc;
        private System.Windows.Forms.TextBox textBoxData7;
        private System.Windows.Forms.TextBox textBoxIntTime;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxData2;
        private System.Windows.Forms.TextBox textBoxData5;
        private System.Windows.Forms.TextBox textBoxData3;
        private System.Windows.Forms.TextBox textBoxData4;
        private System.Windows.Forms.TextBox textBoxData0;
        private System.Windows.Forms.TextBox textBoxData1;
        private System.Windows.Forms.TextBox textBoxData6;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
    }
}
