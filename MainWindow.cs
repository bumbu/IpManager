using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Management;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace Lab_2
{
    public partial class MainWindow : Form
    {
        private bool saveDataLocked = false;
        private ManagementObjectCollection adapters;
        private IpCtrl ctrl = new IpCtrl();
        private String settingsFileName = "settings.json";
        private int lastEditedListItemIndex = -1;

        public MainWindow()
        {
            InitializeComponent();

            Bitmap bmp = IpManager.Properties.Resources.icon;
            this.Icon = Icon.FromHandle(bmp.GetHicon());

            loadConfig();
            if (listBox1.Items.Count > 0)
                listBox1.SelectedIndex = 0;


            adapters = ctrl.getAdapters();
        }

        // labels  =========================================

        private void label1_Click(object sender, EventArgs e)
        {
            textBox1.Focus();
        }

        private void label2_Click(object sender, EventArgs e)
        {
            textBox2.Focus();
        }

        private void label3_Click(object sender, EventArgs e)
        {
            textBox3.Focus();
        }

        private void label4_Click(object sender, EventArgs e)
        {
            textBox4.Focus();
        }

        private void label5_Click(object sender, EventArgs e)
        {
            textBox5.Focus();
        }

        // text inputs  =========================================

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (testData(textBox1.Text,"ip"))
                saveData();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (testData(textBox2.Text, "mask"))
                saveData();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (testData(textBox3.Text, "submask"))
                saveData();
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            if (testData(textBox4.Text, "ip"))
                saveData();
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            if (testData(textBox5.Text, "ip"))
                saveData();
        }

        private void textBoxHidden_Leave(object sender, EventArgs e)
        {
            if (lastEditedListItemIndex >= 0)
            {
                listBox1.Items[lastEditedListItemIndex] = textBoxHidden.Text;
                textBoxHidden.Visible = false;

                saveData(false);
            }
        }

        private void textBoxHidden_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // Move focus to list box
                listBox1.Focus();
            }
        }

        // buttons  =========================================

        // apply

        private void button1_Click(object sender, EventArgs e)
        {
            // save data
            //String ip = textBox1.Text;
            //String mask = textBox2.Text;
            //ListIP();
            //setIP(textBox1.Text, textBox2.Text, textBox3.Text);

            foreach (ManagementObject objMO in adapters)
            {
                if (!(bool)objMO["ipEnabled"])
                    continue;

                if(checkBox1.Checked)
                    ctrl.EnableDHCP(objMO);
                else
                    ctrl.setIP(objMO, textBox1.Text, textBox2.Text, textBox3.Text);

                if (checkBox2.Checked)
                    ctrl.EnableAutoDns(objMO);
                else{
                    ctrl.setDNS(objMO, new string[] { textBox4.Text, textBox5.Text });
                }
            }

            //if (cbAutoIp.Checked)
            //{
            //    ctrl.EnableDHCP(current);
            //}
            //else
            //{
            //    ctrl.setIP(current, tbIp.Text, tbMask.Text, tbGateway.Text);
            //}

            //if (cbAutoDns.Checked)
            //{
            //    //ctrl.EnableAutoDns
            //}
            //else
            //{
            //    ctrl.SetDNS(current, new string[] { tbDns1.Text, tbDns1.Text });
            //}
        }

        // remove item from list
        private void button2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure?\nIt will erase the data permanently.", "Item removal", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                == DialogResult.Yes )
            {
                int remove_id;
                if (listBox1.SelectedIndex >= 0) {
                    remove_id = listBox1.SelectedIndex;
                    removeByIndex(remove_id);
                    listBox1.Items.Remove(listBox1.SelectedItem);
                }

                if (listBox1.Items.Count > 0) {
                    listBox1.SelectedIndex = listBox1.Items.Count - 1;
                } else {
                    clearTextFields();
                }
            }
        }

        // add new items in list
        private void button3_Click(object sender, EventArgs e)
        {
            addNewListElement(true);
            clearTextFields();
            saveData(true);
        }

        private void addNewListElement(bool save = false)
        {
            int count = listBox1.Items.Count;
            int next;

            if (count > 0)
            {
                listBox1.BeginUpdate();
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
                next = Convert.ToInt32(listBox1.SelectedItem.ToString().Substring(4)) + 1;
                listBox1.SelectedIndex = -1;
                listBox1.EndUpdate();
            }
            else
            {
                next = 1;
            }

            listBox1.Items.Add("Item " + next.ToString());
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
            checkBox1.Checked = true;
            checkBox2.Checked = true;
        }

        private void addNewListElement(string name)
        {
            listBox1.Items.Add(name);                
        }

        // selector  =========================================

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string line;
            int counter = 0;
            // load data

            System.IO.StreamReader file = new System.IO.StreamReader(settingsFileName);
            while ((line = file.ReadLine()) != null)
            {   
                if (counter == listBox1.SelectedIndex)
                {
                    // decompress from JSON
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HostData));
                    System.IO.MemoryStream ms = new System.IO.MemoryStream(System.Text.Encoding.ASCII.GetBytes(line));
                    HostData hd = (HostData)ser.ReadObject(ms);
                    ms.Close();

                    updateTextFields(hd);

                    break;
                }
                counter++;
            }

            file.Close();
        }

        // Ask for new title on double click
        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int index = listBox1.IndexFromPoint(e.Location);
            if (index != System.Windows.Forms.ListBox.NoMatches)
            {
                // Change input location
                Point textBoxLocation = textBoxHidden.Location;
                textBoxLocation.Y = listBox1.Location.Y + index * listBox1.ItemHeight;
                textBoxHidden.Location = textBoxLocation;
                // Change input value
                textBoxHidden.Text = listBox1.Items[index].ToString();
                // Display input
                textBoxHidden.Visible = true;
                // Focus input
                textBoxHidden.Focus();

                // Set global value of this item
                lastEditedListItemIndex = index;
            }
        }

        // functions  =========================================

        private void loadConfig()
        {
            string line;

            StreamReader file = new StreamReader(settingsFileName);
            while ((line = file.ReadLine()) != null)
            {
                // decompress from JSON
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HostData));
                MemoryStream ms = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(line));
                HostData hd = (HostData)ser.ReadObject(ms);
                ms.Close();
                // add to list
                addNewListElement(hd.name);
            }

            file.Close();
        }

        private void clearTextFields()
        {
            clearTextFieldsAddress();
            clearTextFieldsDNS();
        }

        private void clearTextFieldsAddress()
        {
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
        }

        private void clearTextFieldsDNS()
        {
            textBox4.Text = "";
            textBox5.Text = "";
        }

        private void disableTextFieldsAdress()
        {
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            textBox3.Enabled = false;
        }

        private void enableTextFieldsAdress()
        {
            textBox1.Enabled = true;
            textBox2.Enabled = true;
            textBox3.Enabled = true;
        }

        private void disableTextFieldsDNS()
        {
            textBox4.Enabled = false;
            textBox5.Enabled = false;
        }

        private void enableTextFieldsDNS()
        {
            textBox4.Enabled = true;
            textBox5.Enabled = true;
        }

        private void updateTextFields(HostData hd)
        {
            saveDataLocked = true;

            textBox1.Text = hd.ip;
            textBox2.Text = hd.mask;
            textBox3.Text = hd.submask;
            if (hd.auto_addr)
                checkBox1.Checked = true;
            else
                checkBox1.Checked = false;

            textBox4.Text = hd.dns;
            textBox5.Text = hd.dns2;
            if (hd.auto_dns)
                checkBox2.Checked = true;
            else
                checkBox2.Checked = false;
            
            saveDataLocked = false;
        }

        private bool testData(string data, string type)
        {
            // ToDo
            return true;
        }

        private void saveData(bool appended = false)
        {
            if (saveDataLocked)
                return;
            saveDataLocked = true;

            HostData hd = new HostData();
            if (listBox1.SelectedIndex >= 0)
                hd.name = listBox1.SelectedItem.ToString();
            else
                hd.name = "";
            hd.ip = textBox1.Text;
            hd.mask = textBox2.Text;
            hd.submask = textBox3.Text;
            hd.dns = textBox4.Text;
            hd.dns2 = textBox5.Text;
            hd.auto_addr = checkBox1.Checked;
            hd.auto_dns = checkBox2.Checked;

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HostData));
            MemoryStream ms = new MemoryStream();
            ser.WriteObject(ms, hd);
            string hd_json = Encoding.Default.GetString(ms.ToArray());

            if (appended)
            {
                // TODO: check if file is not blocked
                using (StreamWriter log_file = File.AppendText(settingsFileName))
                {
                    log_file.WriteLine();
                    log_file.Write(hd_json);
                    log_file.Close();
                }
            }
            else
            {
                string[] data = File.ReadAllLines(settingsFileName);
                string data_new = "";
                if (listBox1.SelectedIndex >= 0)
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (i == listBox1.SelectedIndex)
                        {
                            data_new += hd_json;
                        }
                        else
                        {
                            data_new += data[i];
                        }

                        if (i < data.Length - 1)
                            data_new += Environment.NewLine;
                    }
                    File.WriteAllText(settingsFileName, data_new);
                }
            }
            saveDataLocked = false;
        }

        private void removeByIndex(int index)
        {
            if (saveDataLocked)
                return;
            saveDataLocked = true;

            string[] data = File.ReadAllLines(settingsFileName);
            string data_new = "";
            if (index >= 0)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (i != index)
                    {
                        data_new += data[i];
                        if (i < data.Length - 1)
                            data_new += Environment.NewLine;
                    }                    
                }
                File.WriteAllText(settingsFileName, data_new);
            }

            saveDataLocked = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                clearTextFieldsAddress();
                disableTextFieldsAdress();
            }
            else
            {
                enableTextFieldsAdress();
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                clearTextFieldsDNS();
                disableTextFieldsDNS();
            }
            else
            {
                enableTextFieldsDNS();
            }
        }

    }

    [DataContract]
    class HostData
    {
        [DataMember]
        internal bool auto_addr;

        [DataMember]
        internal string name;

        [DataMember]
        internal string ip;

        [DataMember]
        internal string mask;

        [DataMember]
        internal string submask;

        [DataMember]
        internal bool auto_dns;

        [DataMember]
        internal string dns;

        [DataMember]
        internal string dns2;
    }
}
