﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System.Threading;
using Q42.HueApi.Converters;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.Original;
using PubSub;
using ServiceStack.Redis;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace lightbot_net_app
{
    public partial class Form1 : Form
    {
        public IEnumerable<Q42.HueApi.Models.Bridge.LocatedBridge> bridgeIPs;
        public IBridgeLocator locator = new HttpBridgeLocator();
        public ILocalHueClient client;
        public IReadOnlyCollection<Q42.HueApi.Models.Groups.Group> groups;
        public System.Threading.Thread pubsubThread;

        private IRedisPubSubServer redisPubSub;


        public Form1()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.Visible == false)
            {
                this.ShowDialog();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            /// Checkboxes
            checkBox1.Checked = Properties.Settings.Default.cheering;
            checkBox2.Checked = Properties.Settings.Default.largeCheer;
            checkBox3.Checked = Properties.Settings.Default.onOff;
            checkBox4.Checked = Properties.Settings.Default.subs;

            /// int Textboxes
            textBox2.Text = Properties.Settings.Default.cheerFloor.ToString();
            textBox3.Text = Properties.Settings.Default.largeCheerFloor.ToString();
            textBox4.Text = Properties.Settings.Default.offFloor.ToString();
            textBox5.Text = Properties.Settings.Default.onFloor.ToString();

            /// Comboboxes
            comboBox1.Text = Properties.Settings.Default.largeCheerAction;
            comboBox2.Text = Properties.Settings.Default.primeSubAction;
            comboBox3.Text = Properties.Settings.Default.Tier1SubAction;
            comboBox4.Text = Properties.Settings.Default.Tier2SubAction;
            comboBox5.Text = Properties.Settings.Default.Tier3SubAction;

            exitToolStripMenuItem.Click += new EventHandler(exitToolStripMenuItem_Click);

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
            return;
        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private async void button4_Click(object sender, EventArgs e)
        {
            bridgeIPs = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(10));
            client = new LocalHueClient(bridgeIPs.First().IpAddress);
            if (Properties.Settings.Default.appkey != "")
            {
                client.Initialize(Properties.Settings.Default.appkey);
                logEvent("Connected with previous Hue Auth");
            }
            else
            {
                string messageBoxText = "Press the button on your Hue bridge and then click Ok.";
                string caption = "Action Required";
                MessageBoxButtons button = MessageBoxButtons.OK;
                DialogResult result = MessageBox.Show(messageBoxText, caption, button);

                switch (result)
                {
                    case DialogResult.OK:
                        var appKey = await client.RegisterAsync("HueLightBot", Environment.MachineName);
                        Properties.Settings.Default.appkey = appKey;
                        Properties.Settings.Default.Save();
                        logEvent("Connected with new Hue Auth");
                        break;
                }
            }
            
            groups = await client.GetGroupsAsync();
            List<string> groupNames =  groups.Select(x => x.Name).ToList();

            comboBox6.DataSource = groupNames;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            /// Checkboxes
            Properties.Settings.Default.cheering = checkBox1.Checked;
            Properties.Settings.Default.largeCheer = checkBox2.Checked;
            Properties.Settings.Default.onOff = checkBox3.Checked;
            Properties.Settings.Default.subs = checkBox4.Checked;

            /// int Textboxes
            Properties.Settings.Default.cheerFloor = Int32.Parse(textBox2.Text);
            Properties.Settings.Default.largeCheerFloor = Int32.Parse(textBox3.Text);
            Properties.Settings.Default.offFloor = Int32.Parse(textBox4.Text);
            Properties.Settings.Default.onFloor = Int32.Parse(textBox5.Text);

            /// Comboboxes
            Properties.Settings.Default.largeCheerAction = comboBox1.Text;
            Properties.Settings.Default.primeSubAction = comboBox2.SelectedText;
            Properties.Settings.Default.Tier1SubAction = comboBox3.Text;
            Properties.Settings.Default.Tier2SubAction = comboBox4.Text;
            Properties.Settings.Default.Tier3SubAction = comboBox5.Text;

            Properties.Settings.Default.Save();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            /// stop
            if (pubsubThread.IsAlive)
            {
                pubsubThread.Interrupt();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            /// start
            if (pubsubThread == null)
            {
                pubsubThread = (new Thread(() => pubsubRunner(client)));
                pubsubThread.Start();

                //HandleOnMessage("geoff", "{\"type\": \"cheer\", \"nick\": \"aetaric\", \"amount\": 200, \"message\": \"cheer200 this is a test #ff0000\"}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.WindowsShutDown)
            {
                return;
            }
            else
            {
                this.Visible = false;
                e.Cancel = true;
            }
        }

        private void pubsubRunner(ILocalHueClient client)
        {
            Console.WriteLine("Started pubsub");

            var clientsManager = new PooledRedisClientManager("3rogK4bOMIAFDSJ8P0FqjNYxwDIk6UatbbMlUVV7uySHnPhxhrh0IFIwnf3ZEjo@pubsub.huelightbot.com");
            
            redisPubSub = new RedisPubSubServer(clientsManager, textBox1.Text)
            {
                OnMessage = (channel, msg) => HandleOnMessage(channel, msg),
                OnError = (ex) => HandleOnError(ex)
            }.Start();

            return;
        }

        private void HandleOnError(Exception ex)
        {
            logEvent(String.Format("Received '{0}' error from pubsub", ex.Message));
            
        }


        private void HandleOnMessage(string channel, string msg)
        {
            logEvent(String.Format("Received '{0}' from pubsub", msg, channel));

            if (msg.Contains("cheer"))
            {
                CheerVO cheer = JsonConvert.DeserializeObject<CheerVO>(msg);

                if (cheer.amount >= Properties.Settings.Default.cheerFloor)
                {

                    foreach (Match match in Regex.Matches(cheer.message, @"#([0-9a-fA-F]{6})"))
                    {
                        if (!String.IsNullOrEmpty(match.Value))
                            SetHexColor(match.Value);
                    }

                }
                else if(cheer.amount >= Properties.Settings.Default.largeCheerFloor && Properties.Settings.Default.largeCheer)
                {
                    if (Properties.Settings.Default.largeCheerAction == "Blink")
                    {
                        DoBlink();
                    }
                    else
                    {
                        DoColorLoop();
                    }
                      
                }
                else if(cheer.amount >= Properties.Settings.Default.offFloor && cheer.message.Contains("!off") && Properties.Settings.Default.onOff)
                {
                    SetLightsOff();
                }
                else if(cheer.amount >= Properties.Settings.Default.onFloor && cheer.message.Contains("!on") && Properties.Settings.Default.onOff)
                {
                    SetLightsOn();
                }

            }
        }

        private void DoColorLoop()
        {
            var command = new LightCommand();
            command.Effect = Effect.ColorLoop;

            client.SendCommandAsync(command);
            logEvent("Doing a Color Loop");
        }

        private void DoBlink()
        {
            var command = new LightCommand();
            //command.Alert = Alerts.Once;

            client.SendCommandAsync(command);
            logEvent("Doing a Color Loop");
        }

        private void SetHexColor(string hex)
        {
            var command = new LightCommand();
            command.SetColor(new RGBColor(hex));

            client.SendCommandAsync(command);
            logEvent("Setting Lights to " + hex);
        }

        private void SetLightsOff()
        {
            var command = new LightCommand();
            command.TurnOff();

            client.SendCommandAsync(command);
        }

        private void SetLightsOn()
        {
            var command = new LightCommand();
            command.TurnOn();

            client.SendCommandAsync(command);
        }
       private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private Q42.HueApi.Models.Groups.Group getSelectedGroup()
        {
            Q42.HueApi.Models.Groups.Group selectedGroup = null;
            foreach (Q42.HueApi.Models.Groups.Group group in groups)
            {
                string g = comboBox6.SelectedItem.ToString();
                Console.WriteLine(g);
                if (group.Name.Equals(g, StringComparison.Ordinal))
                {
                    selectedGroup = group;
                    break;
                }
            }
            return selectedGroup;
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            // Color Loop
            if (await client.CheckConnection() == true)
            {
                var commandLoopOn = new LightCommand();
                commandLoopOn.Effect = Effect.ColorLoop;
                var commandLoopOff = new LightCommand();
                commandLoopOff.Effect = Effect.None;
                Q42.HueApi.Models.Groups.Group selectedGroup = getSelectedGroup();

                await client.SendCommandAsync(commandLoopOn, selectedGroup.Lights);
                System.Threading.Thread.Sleep(20000);
                await client.SendCommandAsync(commandLoopOff, selectedGroup.Lights);
                logEvent("Looped Lights via UI");
            }
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            // Blink
            if (await client.CheckConnection() == true)
            {
                var command = new LightCommand();
                command.Alert = Alert.Multiple;
                Q42.HueApi.Models.Groups.Group selectedGroup = getSelectedGroup();

                await client.SendCommandAsync(command, selectedGroup.Lights);
                logEvent("Blinked Lights via UI");
            }
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            // Custom Color
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                if (await client.CheckConnection() == true)
                {
                    var command = new LightCommand();
                    Q42.HueApi.ColorConverters.RGBColor color = new Q42.HueApi.ColorConverters.RGBColor();
                    color.R = colorDialog1.Color.R;
                    color.G = colorDialog1.Color.G;
                    color.B = colorDialog1.Color.B;
                    command.SetColor(color);
                    Q42.HueApi.Models.Groups.Group selectedGroup = getSelectedGroup();

                    await client.SendCommandAsync(command, selectedGroup.Lights);
                    logEvent("Changed color via UI");
                }
            }
        }

        private async void button8_Click(object sender, EventArgs e)
        {
            // Lights On
            if (await client.CheckConnection() == true)
            {
                var command = new LightCommand();
                command.TurnOn();
                Q42.HueApi.Models.Groups.Group selectedGroup = getSelectedGroup();

                await client.SendCommandAsync(command, selectedGroup.Lights);
                logEvent("Turned On Lights via UI");
            }
        }

        private async void button9_Click(object sender, EventArgs e)
        {
            // Lights Off
            if (await client.CheckConnection() == true)
            {
                var command = new LightCommand();
                command.TurnOff();
                Q42.HueApi.Models.Groups.Group selectedGroup = getSelectedGroup();

                await client.SendCommandAsync(command, selectedGroup.Lights);
                logEvent("Turned Off Lights via UI");
            }
        }

        private void eventLog1_EntryWritten(object sender, System.Diagnostics.EntryWrittenEventArgs e)
        {
            if (textBox6.InvokeRequired)
            {
                textBox6.Invoke(new Action(() => { textBox6.AppendText(e.Entry.Message + Environment.NewLine); }));
                textBox6.Update();
            }
            else
            {
                textBox6.AppendText(e.Entry.Message + Environment.NewLine);
                textBox6.Update();
            }
        }

        private void logEvent(string eventText)
        {
            eventLog1.WriteEntry(eventText);
            textBox6.Invoke(new Action(() => { textBox6.AppendText(eventText + Environment.NewLine); }));
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            
        }
    }


    public class CheerVO
    {
        public string type { get; set; }
        public string nick { get; set; }
        public int amount { get; set; }
        public string message { get; set; }
    }

    public class SubVO
    {
        public string type { get; set; }
        public string nick { get; set; }
        public string message { get; set; }
    }
}