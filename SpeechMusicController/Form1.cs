﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpeechMusicController {
    public partial class Form1 : Form {

        SpeechInput speechInput;
        bool Listening = true;

        public Form1() {
            InitializeComponent();
            speechInput = new SpeechInput(this);
            speechInput.start();
            NotifyIcon.Visible = false;
        }

        private void btnSwitch_Click(object sender, EventArgs e) {
            if(Listening) {
                Listening = false;
                btnSwitch.Text = "Start";
                speechInput.Enabled = false;
            } else {
                Listening = true;
                btnSwitch.Text = "Stop";
                speechInput.Enabled = true;
            }
        }

        public void WriteLine(string message) {
            textBox1.Text = "\r\n" + message + textBox1.Text;
        }

        private void frmMain_Resize(object sender, EventArgs e) {
            NotifyIcon.BalloonTipTitle = "Minimize to Tray App";
            NotifyIcon.BalloonTipText = "SpeechMusicController is down here";

            if(FormWindowState.Minimized == this.WindowState) {
                NotifyIcon.Visible = true;
                NotifyIcon.ShowBalloonTip(500);
                this.Hide();
            } else if(FormWindowState.Normal == this.WindowState) {
                NotifyIcon.Visible = false;
            }
        }

        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e) {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void MenuItemExit_Click(object sender, EventArgs e) {
            Environment.Exit(0);
        }
    }
}
