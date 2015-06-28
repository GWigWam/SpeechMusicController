﻿using SpeechMusicController.AppSettings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpeechMusicController {
    public partial class SystemVarsEdit : Form {

        public SystemVarsEdit() {
            InitializeComponent();

            Lb_Path.Text = Settings.Instance.FullFilePath;

            Lb_List.Items.AddRange(Settings.Instance.GetAllSettingNames());
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            string key = (string)Lb_List.SelectedItem;
            string value = Settings.Instance.GetSetting(key);

            Tb_NewString.Text = value;
        }

        private void Bt_Ok_Click(object sender, EventArgs e) {
            if (Lb_List.SelectedItem as string != null && !string.IsNullOrEmpty(Tb_NewString.Text)) {
                string key = (string)Lb_List.SelectedItem;
                Settings.Instance.SetSetting(key, Tb_NewString.Text.Trim());

                Bt_Ok.Enabled = false;
            }
        }

        private void Bt_SaveToFile_Click(object sender, EventArgs e) {
            Settings.Instance.WriteToDisc();
        }

        private void Tb_NewString_TextChanged(object sender, EventArgs e) {
            if (Tb_NewString.Text != Settings.Instance.GetSetting((string)Lb_List.SelectedItem)) {
                Bt_Ok.Enabled = true;
            } else {
                Bt_Ok.Enabled = false;
            }
        }
    }
}