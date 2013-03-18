using Parse;
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
using System.Threading.Tasks;

namespace IpConfig {
    public partial class MainWindow : Form {
        private bool writeDataLocked = false;
        private ManagementObjectCollection adapters;
        private IpCtrl ctrl = new IpCtrl();
        private String settingsFileName = "settings.json";
        private int lastEditedListItemIndex = -1;
        private int lastSelectedListItemIndex = -1;

        public MainWindow() {
            InitializeComponent();

            Bitmap bmp = IpManager.Properties.Resources.icon;
            this.Icon = Icon.FromHandle(bmp.GetHicon());

            loadConfigAndPopulateList();
            if (listBox1.Items.Count > 0)
                listBox1.SelectedIndex = 0;

            adapters = ctrl.getAdapters();
        }

        // Labels  =========================================

        private void label1_Click(object sender, EventArgs e) {
            textBox1.Focus();
        }

        private void label2_Click(object sender, EventArgs e) {
            textBox2.Focus();
        }

        private void label3_Click(object sender, EventArgs e) {
            textBox3.Focus();
        }

        private void label4_Click(object sender, EventArgs e) {
            textBox4.Focus();
        }

        private void label5_Click(object sender, EventArgs e) {
            textBox5.Focus();
        }

        // Text inputs  =========================================

        private void textBox1_TextChanged(object sender, EventArgs e) {
            if (testData(textBox1.Text, "ip"))
                parseInputsAndSaveData();
        }

        private void textBox2_TextChanged(object sender, EventArgs e) {
            if (testData(textBox2.Text, "mask"))
                parseInputsAndSaveData();
        }

        private void textBox3_TextChanged(object sender, EventArgs e) {
            if (testData(textBox3.Text, "submask"))
                parseInputsAndSaveData();
        }

        private void textBox4_TextChanged(object sender, EventArgs e) {
            if (testData(textBox4.Text, "ip"))
                parseInputsAndSaveData();
        }

        private void textBox5_TextChanged(object sender, EventArgs e) {
            if (testData(textBox5.Text, "ip"))
                parseInputsAndSaveData();
        }

        private void textBoxHidden_Leave(object sender, EventArgs e) {
            if (lastEditedListItemIndex >= 0) {
                listBox1.Items[lastEditedListItemIndex] = textBoxHidden.Text;
                textBoxHidden.Visible = false;

                parseInputsAndSaveData();
            }
        }

        private void textBoxHidden_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                // Move focus to list box
                listBox1.Focus();
            }
        }

        // Buttons  =========================================

        // Apply network settings
        private void button1_Click(object sender, EventArgs e) {
            foreach (ManagementObject objMO in adapters) {
                if (!(bool)objMO["ipEnabled"])
                    continue;

                if (checkBox1.Checked)
                    ctrl.EnableDHCP(objMO);
                else
                    ctrl.setIP(objMO, textBox1.Text, textBox2.Text, textBox3.Text);

                if (checkBox2.Checked)
                    ctrl.EnableAutoDns(objMO);
                else {
                    ctrl.setDNS(objMO, new string[] { textBox4.Text, textBox5.Text });
                }
            }
        }

        // Menu items ===========================================

        // Add new empty item to list
        private void addNewItemToolStripMenuItem_Click(object sender, EventArgs e) {
            addNewListElement();
            clearTextFields();
            parseInputsAndSaveData(true);
        }

        // Remove an item from the list
        private void removeSelectedItemToolStripMenuItem_Click(object sender, EventArgs e) {
            if (MessageBox.Show("Are you sure?\nIt will erase the data permanently.", "Item removal", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                == DialogResult.Yes) {
                removeSelectedListItem();
            }
        }

        // Load data from cloud
        private void loadFromCloudToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadListFromCloud();
        }

        // About
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
            MessageBox.Show("For details go to https://github.com/bumbu/IpManager", "About application", MessageBoxButtons.OK);
        }

        // ListBox  =========================================

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            // Load data            
            if (loadByIndexAndPopulate(listBox1.SelectedIndex)) {
                lastSelectedListItemIndex = listBox1.SelectedIndex;
            } else if(lastSelectedListItemIndex >= 0) {
                listBox1.SelectedIndex = lastSelectedListItemIndex;
            }
        }

        // Ask for new title on double click
        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e) {
            int index = listBox1.IndexFromPoint(e.Location);
            if (index != System.Windows.Forms.ListBox.NoMatches) {
                // Change hidden input location
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

        // Functions  =========================================

        // Add new element without name (called by user)
        private void addNewListElement() {
            listBox1.Items.Add("Item " + (listBox1.Items.Count + 1).ToString());
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
            checkBox1.Checked = true;
            checkBox2.Checked = true;
        }

        // Add new elemend with name (called by settings importer)
        private void addNewListElement(string name = "") {
            listBox1.Items.Add(name);
        }

        private void loadConfigAndPopulateList() {
            string line;

            StreamReader file = new StreamReader(settingsFileName);
            while ((line = file.ReadLine()) != null) {
                if (line != "") {
                    // decompress from JSON
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HostData));
                    MemoryStream ms = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(line));
                    HostData hd = (HostData)ser.ReadObject(ms);
                    ms.Close();

                    // add to list
                    addNewListElement(hd.name);
                }
            }

            file.Close();
        }

        private bool loadByIndexAndPopulate(int index) {
            string line;
            int counter = 0;

            // If file is blocked by writing, then deny to load
            if (writeDataLocked)
                return false;

            // Load data
            System.IO.StreamReader file = new System.IO.StreamReader(settingsFileName);
            while ((line = file.ReadLine()) != null) {
                if (line != "" && counter == listBox1.SelectedIndex) {
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

            return true;
        }

        private void clearTextFields() {
            clearTextFieldsAddress();
            clearTextFieldsDNS();
        }

        private void clearTextFieldsAddress() {
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
        }

        private void clearTextFieldsDNS() {
            textBox4.Text = "";
            textBox5.Text = "";
        }

        private void disableTextFieldsAdress() {
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            textBox3.Enabled = false;
        }

        private void enableTextFieldsAdress() {
            textBox1.Enabled = true;
            textBox2.Enabled = true;
            textBox3.Enabled = true;
        }

        private void disableTextFieldsDNS() {
            textBox4.Enabled = false;
            textBox5.Enabled = false;
        }

        private void enableTextFieldsDNS() {
            textBox4.Enabled = true;
            textBox5.Enabled = true;
        }

        private void updateTextFields(HostData hd) {
            writeDataLocked = true;

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

            writeDataLocked = false;
        }

        private bool testData(string data, string type) {
            // ToDo
            return true;
        }

        private void parseInputsAndSaveData(bool appended = false) {
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

            saveData(hd, appended);
        }

        private void saveData(HostData hd, bool appended = false) {
            if (writeDataLocked)
                return;
            writeDataLocked = true;

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HostData));
            MemoryStream ms = new MemoryStream();
            ser.WriteObject(ms, hd);
            string hd_json = Encoding.Default.GetString(ms.ToArray());

            if (appended) {
                // TODO: check if file is not blocked
                using (StreamWriter log_file = File.AppendText(settingsFileName)) {
                    log_file.WriteLine();
                    log_file.Write(hd_json);
                    log_file.Close();
                }
            } else {
                string[] data = File.ReadAllLines(settingsFileName);
                string data_new = "";

                if (listBox1.SelectedIndex >= 0) {
                    for (int i = 0; i < data.Length; i++) {
                        if (i == listBox1.SelectedIndex) {
                            data_new += hd_json;
                        } else {
                            data_new += data[i];
                        }

                        if (i < data.Length - 1)
                            data_new += Environment.NewLine;
                    }

                    File.WriteAllText(settingsFileName, data_new);
                }
            }

            writeDataLocked = false;
        }

        private void removeSelectedListItem() {
            if (listBox1.SelectedIndex >= 0) {
                removeByIndex(listBox1.SelectedIndex);
                listBox1.Items.Remove(listBox1.SelectedItem);
            }

            if (listBox1.Items.Count > 0) {
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
            } else {
                clearTextFields();
            }
        }

        private void removeByIndex(int index) {
            if (writeDataLocked)
                return;
            writeDataLocked = true;

            string[] data = File.ReadAllLines(settingsFileName);
            string data_new = "";

            if (index >= 0) {
                for (int i = 0; i < data.Length; i++) {
                    if (i != index) {
                        data_new += data[i];

                        if (i < data.Length - 1)
                            data_new += Environment.NewLine;
                    }
                }

                File.WriteAllText(settingsFileName, data_new);
            }

            writeDataLocked = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) {
            if (checkBox1.Checked) {
                clearTextFieldsAddress();
                disableTextFieldsAdress();
            } else {
                enableTextFieldsAdress();
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e) {
            if (checkBox2.Checked) {
                clearTextFieldsDNS();
                disableTextFieldsDNS();
            } else {
                enableTextFieldsDNS();
            }
        }

        public async Task LoadListFromCloud() {
            try {
                var query = from gameScore in ParseObject.GetQuery("Settings")
                            where gameScore.Get<string>("name") != ""
                            select gameScore;
                IEnumerable<ParseObject> results = await query.FindAsync();
                foreach (ParseObject result in results) {
                    HostData hd = new HostData();

                    hd.name = result.Get<string>("name");

                    hd.auto_addr = result.Get<bool>("auto_addr");
                    if (!hd.auto_addr) {
                        hd.ip = result.Get<string>("ip");
                        hd.mask = result.Get<string>("mask");
                        hd.submask = result.Get<string>("submask");
                    }

                    hd.auto_dns = result.Get<bool>("auto_dns");
                    if (!hd.auto_dns) {
                        hd.dns = result.Get<string>("dns");
                        hd.dns2 = result.Get<string>("dns2");
                    }

                    saveData(hd, true);

                    addNewListElement(hd.name);
                }
            } catch (Exception e) {
                Console.WriteLine("{0} Exception caught.", e);
            }
        }

    }

    [DataContract]
    class HostData {
        [DataMember]
        internal string name;

        [DataMember]
        internal bool auto_addr = false;

        [DataMember]
        internal string ip = "";

        [DataMember]
        internal string mask = "";

        [DataMember]
        internal string submask = "";

        [DataMember]
        internal bool auto_dns = false;

        [DataMember]
        internal string dns = "";

        [DataMember]
        internal string dns2 = "";
    }
}
