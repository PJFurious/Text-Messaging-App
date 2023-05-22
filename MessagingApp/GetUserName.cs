using System;
using System.Windows.Forms;

namespace MessagingApp
{
    public partial class GetUserName : Form
    {
        public string username { get; set; }

        public GetUserName()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            username = textBox1.Text;
            this.Close();
        }
    }
}
