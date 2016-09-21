using System;
using System.Windows.Forms;

namespace User_Control
{
    public partial class CycleCtrl : UserControl
    {
        private bool check;
        private int id;
        private int dlc;
        private byte[] dat = new byte[8];
        private int interval;

        public bool Check { get { return check; } }
        public int Id { get { return id; } }
        public int Dlc { get { return dlc; } }
        public byte[] Dat { get { return dat; } }
        public int Interval { get { return interval; } }

        public int TimeCnt = 0;

        public CycleCtrl()
        {
            InitializeComponent();
            SetStyle(
                      ControlStyles.OptimizedDoubleBuffer
                      | ControlStyles.ResizeRedraw
                      | ControlStyles.Selectable
                      | ControlStyles.AllPaintingInWmPaint
                      | ControlStyles.UserPaint
                      | ControlStyles.SupportsTransparentBackColor,
                      true);
            Clear();
            updateData();
        }
        public bool Init(string str)
        {
            try
            {
                string[] split = str.Split(new char[] { ' ' });
                checkBoxEnable.Checked = bool.Parse(split[0]);
                textBoxId.Text = split[1];
                textBoxDlc.Text = split[2];
                textBoxData0.Text = split[3];
                textBoxData1.Text = split[4];
                textBoxData2.Text = split[5];
                textBoxData3.Text = split[6];
                textBoxData4.Text = split[7];
                textBoxData5.Text = split[8];
                textBoxData6.Text = split[9];
                textBoxData7.Text = split[10];
                textBoxIntTime.Text = split[11];
            }
            catch
            {
                Clear();
                return false;
            }
            return true;
        }
        public void Clear()
        {
            checkBoxEnable.Checked = false;
            textBoxId.Text = "000";
            textBoxDlc.Text = "8";
            textBoxData0.Text = "00";
            textBoxData1.Text = "00";
            textBoxData2.Text = "00";
            textBoxData3.Text = "00";
            textBoxData4.Text = "00";
            textBoxData5.Text = "00";
            textBoxData6.Text = "00";
            textBoxData7.Text = "00";
            textBoxIntTime.Text = "100";
        }
        public override string ToString()
        {
            string str = id.ToString("X3") + " "
                + dlc.ToString("D1") + " "
                + dat[0].ToString("X2") + " "
                + dat[1].ToString("X2") + " "
                + dat[2].ToString("X2") + " "
                + dat[3].ToString("X2") + " "
                + dat[4].ToString("X2") + " "
                + dat[5].ToString("X2") + " "
                + dat[6].ToString("X2") + " "
                + dat[7].ToString("X2")
                ;
            return str;
        }
        public string ToSetting()
        {
            return check.ToString() + " "
                + ToString() + " "
                + interval.ToString("D3");
        }
        private void textBoxHex_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            if (e.KeyChar != 8  /* 允许使用退格符 */
             && !Char.IsDigit(e.KeyChar)
             && !(((int)e.KeyChar >= 'A' && (int)e.KeyChar <= 'F'))
             && !(((int)e.KeyChar >= 'a' && (int)e.KeyChar <= 'f')))
            {
                e.Handled = true;
            }
        }

        private void textBoxValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            if (e.KeyChar != 8  /* 允许使用退格符 */
             && !Char.IsDigit(e.KeyChar)
             )
            {
                e.Handled = true;
            }
        }

        private void updateData()
        {
            try
            {
                id = int.Parse(textBoxId.Text, System.Globalization.NumberStyles.HexNumber);
                dlc = int.Parse(textBoxDlc.Text);
                dat[0] = Byte.Parse(textBoxData0.Text, System.Globalization.NumberStyles.HexNumber);
                dat[1] = Byte.Parse(textBoxData1.Text, System.Globalization.NumberStyles.HexNumber);
                dat[2] = Byte.Parse(textBoxData2.Text, System.Globalization.NumberStyles.HexNumber);
                dat[3] = Byte.Parse(textBoxData3.Text, System.Globalization.NumberStyles.HexNumber);
                dat[4] = Byte.Parse(textBoxData4.Text, System.Globalization.NumberStyles.HexNumber);
                dat[5] = Byte.Parse(textBoxData5.Text, System.Globalization.NumberStyles.HexNumber);
                dat[6] = Byte.Parse(textBoxData6.Text, System.Globalization.NumberStyles.HexNumber);
                dat[7] = Byte.Parse(textBoxData7.Text, System.Globalization.NumberStyles.HexNumber);
                interval = int.Parse(textBoxIntTime.Text);
                if (dlc > 8 || interval < 10)
                {
                    checkBoxEnable.Checked = false;
                }
            }
            catch
            {
                checkBoxEnable.Checked = false;
            }
            check = checkBoxEnable.Checked;
        }

        private void textBoxAll_TextChanged(object sender, EventArgs e)
        {
            updateData();
        }
    }
}
