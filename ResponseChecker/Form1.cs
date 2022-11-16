using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Tesseract;

namespace ResponseChecker
{
    public partial class Form1 : Form
    {























        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        private void takeIMg()
        {
            Process proc = Process.GetProcessesByName("wow").FirstOrDefault();


            proc.WaitForInputIdle();
            if (SetForegroundWindow(proc.MainWindowHandle))
            {
                RECT srcRect;
                if (!proc.MainWindowHandle.Equals(IntPtr.Zero))
                {
                    if (GetWindowRect(proc.MainWindowHandle, out srcRect))
                    {
                        int width = srcRect.Right - srcRect.Left;
                        int height = srcRect.Bottom - srcRect.Top;

                        Bitmap bmp = new Bitmap(width, height);
                        Graphics screenG = Graphics.FromImage(bmp);

                        try
                        {
                            screenG.CopyFromScreen(srcRect.Left, srcRect.Top,
                                    0, 0, new Size(width, height),
                                    CopyPixelOperation.SourceCopy);

                            bmp.Save("wow.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                        finally
                        {
                            screenG.Dispose();
                            bmp.Dispose();
                        }
                    }
                }
            }
        }





















        public static Rectangle SelectedArea;
        int LastCheck;
        Timer maintimer = new Timer();
        bool buzy = false;
        int under100 = 0;
        int didntFindNumber = 0;
        int ReportLiveCount = 0;


        public Form1()
        {
            InitializeComponent();
            takeIMg();
            loadLastValues();
            saveValues(null,null);
            maintimer.Tick += Timer_Tick;
            maintimer.Interval = 1000;
            MusicPlayer.DoWork += MusicPlayer_DoWork;

            Timer watchTimer = new Timer();
            watchTimer.Interval = 1;
            watchTimer.Tick += (l, a) =>
            {
            };
            watchTimer.Start();
        }
        string cacheFile = "cache.json";
        class cache
        {
            public string realm { get; set; }
            public string region { get; set; }
            public string name { get; set; }
            public bool reportLive { get; set; }
            public bool soundAlert { get; set; }
        }
        private void loadLastValues()
        {
            if (File.Exists(cacheFile))
            {
                var cache = JsonConvert.DeserializeObject<cache>(File.ReadAllText(cacheFile));
                textBox1.Text = cache.realm;
                textBox2.Text = cache.name;
                comboBox1.Text = cache.region;

                checkBox1.Checked = cache.reportLive;
                checkBox2.Checked = cache.soundAlert;
            }
            
        }
        private void saveValues(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                if (textBox1.Text == "" || textBox1.Text == null)
                {
                    buttonstart.Enabled = false;
                }
                if (textBox2.Text == "" || textBox2.Text == null)
                {
                    buttonstart.Enabled = false;
                }
                if (comboBox1.Text == "" || comboBox1.Text == null)
                {
                    buttonstart.Enabled = false;
                }
                if (!(textBox1.Text == "" || textBox1.Text == null) && !(textBox2.Text == "" || textBox2.Text == null) && !(comboBox1.Text == "" || comboBox1.Text == null))
                {
                    buttonstart.Enabled = true;
                }
            }
            File.WriteAllText(cacheFile, JsonConvert.SerializeObject(new cache(){ realm = textBox1.Text, region = comboBox1.Text, name = textBox2.Text, reportLive = checkBox1.Checked, soundAlert = checkBox2.Checked }));
        }

            Stopwatch watch = new Stopwatch();

        private void Timer_Tick(object sender, EventArgs e)
        {

            if (!buzy)
            {
                buzy = true;

                var size = new Size(SelectedArea.Width, SelectedArea.Height);

                var bmpScreenshot = new Bitmap(SelectedArea.Width,
                                   SelectedArea.Height,
                                   PixelFormat.Format32bppArgb);

                var gfxScreenshot = Graphics.FromImage(bmpScreenshot);
                gfxScreenshot.CopyFromScreen(SelectedArea.Left, SelectedArea.Top, 0, 0, size);

                Bitmap c = bmpScreenshot;
                Bitmap d;
                int x, y;

                // Loop through the images pixels to reset color.
                for (x = 0; x < c.Width; x++)
                {
                    for (y = 0; y < c.Height; y++)
                    {
                        Color pixelColor = c.GetPixel(x, y);
                        Color newColor = Color.FromArgb(pixelColor.R, 0, 0);
                        c.SetPixel(x, y, newColor); // Now greyscale
                    }
                }
                d = c;   // d is grayscale version of c  


                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                {
                    Page page = engine.Process(d, PageSegMode.Auto);

                    string result = page.GetText().Replace("\n", "");

                    try
                    {

                        var queue = int.Parse(result);
                        didntFindNumber = 0;
                        if (queue < 100)
                        {
                            if (under100 > 5)
                            {
                                Alert(queue);
                                under100 = 0;
                            }
                            else
                            {
                                under100++;
                            }
                        }
                        else
                        {
                            under100 = 0;
                        }
                        if (ReportLiveCount > 60)
                        {
                            ReportLiveCount = 0;

                            if (checkBox1.Checked)
                            {
                                reportLive(queue, "confirmed");
                            }
                        }
                        else
                        {
                            ReportLiveCount++;
                        }
                    }
                    catch (Exception)
                    {
                        didntFindNumber++;
                        if (didntFindNumber > 60)
                        {
                            Alert(0);
                            if (ReportLiveCount > 60)
                            {
                                ReportLiveCount = 0;

                                if (checkBox1.Checked)
                                {
                                    reportLive(null, "unknown");
                                }
                            }
                            else
                            {
                                ReportLiveCount++;
                            }
                        }
                    }

                    queue_value.Text = result;


                }



                buzy = false;
            }

        }

        private void Alert(int queue)
        {
            if (checkBox2.Checked && !MusicPlayer.IsBusy)
            {
                MusicPlayer.RunWorkerAsync();
            }            
        }
        

        private void reportLive(int? queue, string status)
        {
            var url = ($@"https://api2.bestkeystone.com/api/ClassicQueue/save?realm={textBox1.Text}&region={comboBox1.Text}&name={textBox2.Text}&status={status}");
            if (queue.HasValue)
            {
                url += $@"&queue={queue.Value}";
            }
            else
            {
                url += $@"&queue=0";

            }
            Post(url);
        }
        public void Post(string url)
        {
            HttpClient client = new HttpClient();
            var response =  client.GetAsync(url);
        }
        private void MusicPlayer_DoWork(object sender, DoWorkEventArgs e)
        {

            System.Media.SoundPlayer player = new System.Media.SoundPlayer();

            player.SoundLocation = @"alert.wav";
            player.PlaySync();
        }

        BackgroundWorker MusicPlayer = new BackgroundWorker();

        private void buttonstart_Click(object sender, EventArgs e)
        {
            buttonstart.Enabled = false;
            var re = new RegionSelector();
            re.FormClosed += Re_FormClosed;
        }

        private void Re_FormClosed(object sender, FormClosedEventArgs e)
        {

            int i = 0;
            Timer timer = new Timer();
            timer.Interval = 1000;
            timer.Start();
            timer.Tick += (s, f) =>
            {
                i++;
                buttonstart.Text = (5 - i).ToString();
                if (i == 5)
                {
                    buttonstart.Text = "scanning...";
                    textBox1.Enabled = false;
                    textBox2.Enabled = false;
                    comboBox1.Enabled = false;
                    maintimer.Start();
                   
                    watch.Restart(); ;
                    timer.Stop();

                }
            };
        }

        private void stopbutton_Click(object sender, EventArgs e)
        {
            maintimer.Stop();
            watch.Stop();
            watch.Reset();

            buttonstart.Enabled = true;
        }
    }
}
