using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Media;
using System.Net;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime;

namespace ROBLOXTextureAssetStudio
{
    public partial class Form1 : Form
    {

        static string DecalOverlayImageName = "";
        static bool allowExportStuds = false;
        static string[] studsCanvases = { "outlets", "inlets", "universal", "smooth", "glue" };
        static Image[] studsRef;
        static bool[] studsChecked;

        static Bitmap templateOutlets = Properties.Resources.studs;
        static Bitmap templateInlets = Properties.Resources.inlets;
        static Bitmap templateUniversal = Properties.Resources.universal;
        static Bitmap templateGlue = Properties.Resources.glue;

        //Audio setup, this is probably what you're not looking for
        SoundPlayer soundDecalDownload = new SoundPlayer(Properties.Resources.DecalDownload);
        SoundPlayer soundTada = new SoundPlayer(Properties.Resources.Victory);
        SoundPlayer soundBass = new SoundPlayer(Properties.Resources.Bass);
        SoundPlayer soundMaterialPaint = new SoundPlayer(Properties.Resources.MaterialPaint);
        SoundPlayer soundPaint = new SoundPlayer(Properties.Resources.Paint);
        SoundPlayer soundStudImprint = new SoundPlayer(Properties.Resources.StudImprint);
        SoundPlayer soundFail = new SoundPlayer(Properties.Resources.Fail);
        SoundPlayer soundPing = new SoundPlayer(Properties.Resources.Ping);

        //This is used to track how long the program has been open for
        //If it's been open for less than 5 seconds, prompt a position reset
        static Stopwatch timer = new Stopwatch();

        //Defines Source Material Names
        enum MaterialTypes
        {
            brick,
            carpet,
            chain,
            concrete,
            dirt,
            flesh,
            glass,
            grass,
            gravel,
            ice,
            metal,
            metalvent,
            plaster,
            plastic,
            quicksand,
            rock,
            rubber,
            snow,
            water,
            watermelon,
            wood
        }
        //Defines Shader Names
        enum ShaderTypes
        {
            LightmappedGeneric,
            VertexLitGeneric,
            UnlitGeneric
        }

        //ROBLOX Material Base Images
        static Bitmap[] BaseImages = {
            Properties.Resources.wood,
            Properties.Resources.woodplanks,
            Properties.Resources.slate,
            Properties.Resources.concrete,
            Properties.Resources.metal,
            Properties.Resources.corrodedmetal,
            Properties.Resources.diamondplate,
            Properties.Resources.grass,
            Properties.Resources.brick,
            Properties.Resources.sand,
            Properties.Resources.fabric,
            Properties.Resources.granite,
            Properties.Resources.marble,
            Properties.Resources.pebble,
            Properties.Resources.cobblestone
        };

        //ROBLOX Material Names
        enum ROBLOXMaterial
        {
            wood,
            woodplanks,
            slate,
            concrete,
            metal,
            corrodedmetal,
            diamondplate,
            grass,
            brick,
            sand,
            fabric,
            granite,
            marble,
            pebble,
            cobblestone
        }

        
        public Form1()
        {
            InitializeComponent();
            //Loads variables from settings file
            //Yes this code is boring and repetative, but it's important.
            comboShaderType.SelectedIndex = Properties.Settings.Default.ShaderType;
            comboSurfaceType.SelectedIndex = Properties.Settings.Default.SurfaceType;
            textboxExport.Text = Properties.Settings.Default.ExportPath;
            textboxVMTDirectory.Text = Properties.Settings.Default.VMTDirectory;
            pictureboxColour.BackColor = Properties.Settings.Default.DecalPainterColour;
            pictureboxColouredDecal.BackColor = Properties.Settings.Default.DecalPainterColour;
            numericDPColourTransparency.Value = Properties.Settings.Default.DecalColourTransparency;
            numericDPDecalTransparency.Value = Properties.Settings.Default.DecalDecalTransparency;
            comboMaterialType.SelectedIndex = Properties.Settings.Default.MaterialType;
            numericMaterialIntensity.Value = Properties.Settings.Default.Intensity;
            pictureboxMaterialColour.BackColor = Properties.Settings.Default.MaterialColour;
            checkStudsOutlets.Checked = Properties.Settings.Default.StudsOutlets;
            checkStudsInlets.Checked = Properties.Settings.Default.StudsInlets;
            checkStudsUniversal.Checked = Properties.Settings.Default.StudsUniversal;
            checkStudsSmooth.Checked = Properties.Settings.Default.StudsSmooth;
            checkStudsGlue.Checked = Properties.Settings.Default.StudsGlue;
            pictureboxStudsColour.BackColor = Properties.Settings.Default.StudsColour;
            numericStudsIntensity.Value = Properties.Settings.Default.StudsIntensity;
            checkVMTCategory.Checked = Properties.Settings.Default.ExportCategory;
            comboStudStyle.SelectedIndex = Properties.Settings.Default.StudsStyle;
            comboMaterialStyle.SelectedIndex = Properties.Settings.Default.MaterialsStyle;

            if (Properties.Settings.Default.PromptPositionReset)
            {
                string body = "This program didn't stay open longer than 5 seconds last time it was opened.\n" +
                    "This may indicate a problem with the program's position on screen.\n" +
                    "Would you like for the program's position to be reset?";

                if (MessageBox.Show(body, "Program's Position", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    this.Location = new Point(50, 50);
                    Properties.Settings.Default.FormStartLocation = this.Location;
                    Properties.Settings.Default.Save();
                }
                else
                    this.Location = Properties.Settings.Default.FormStartLocation;

                //We've already prompted it so we can reset the variable
                Properties.Settings.Default.PromptPositionReset = false;
                Properties.Settings.Default.Save();
            }
            else
                this.Location = Properties.Settings.Default.FormStartLocation;
            pictureboxMaterial.Image = (Image)BaseImages[0].Clone();
            pictureboxStudsOutlets.Image = (Image)Properties.Resources.studs.Clone();
            pictureboxStudsInlets.Image = (Image)Properties.Resources.inlets.Clone();
            pictureboxStudsUniversal.Image = (Image)Properties.Resources.universal.Clone();
            pictureboxStudsSmooth.Image = new Bitmap(2,2);
            pictureboxStudsGlue.Image = (Image)Properties.Resources.glue.Clone();

            ReplaceBaseImages();



            timer.Start();
        }

        private void DecalID_ValueChanged(object sender, EventArgs e)
        {
            DecalID.Enabled = false;
            decalDownloadExportButton.Enabled = false;
            UpdateDecalPreview(Convert.ToUInt64(DecalID.Value));
            DecalID.Enabled = true;
        }

        // Prevents exclamation sound from playing if enter is pressed
        private void DecalID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        private void DecalID_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = e.SuppressKeyPress = true;
            }
        }


        //Gets the decal image from the id and applies it to the picturebox
        private void UpdateDecalPreview(UInt64 DecalID)
        {
            try
            {
                //Get decal details
                string jsonString;
                HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(string.Format("https://economy.roblox.com/v2/assets/" + DecalID+ "/details"));
                WebReq.Method = "GET";
                HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();
                using (Stream stream = WebResp.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                    jsonString = reader.ReadToEnd();
                }
                RobloxMarketplaceRequest decalDetails = JsonConvert.DeserializeObject<RobloxMarketplaceRequest>(jsonString);
                decalName.Text = decalDetails.Name;

                //Done getting decal details, now onto image grabbing

                //If picture box has an image in it, dispose of it as we don't need it anymore
                //This doesn't seem to actually work
                if (pictureBoxDecal.Image != null)
                    pictureBoxDecal.Image.Dispose();


                //If id provided is not the AssetId for the decal, use the F3X api to find the AssetID.

                string IDToDownload = "";
                if (decalDetails.AssetTypeId == "13")
                {
                    throw new InvalidDataException();

                    //The API is broken now, thanks ROBLOX for deciding to deprecate your APIs a month after i started using this 10 year old API
                    //For now we just have to throw an InvalidDataException
                    using (var client = new WebClient { })
                    {
                        //We need to get the actual image id
                        //This API is now broken and F3X is probably not going to fix it.
                        IDToDownload = client.DownloadString("http://f3xteam.com/bt/getDecalImageID/" + DecalID);
                    }
                }
                else if (decalDetails.AssetTypeId == "1")
                    IDToDownload = Convert.ToString(DecalID);
                else
                    throw new InvalidDataException();

                //Get image data
                var request = WebRequest.Create("https://assetdelivery.roblox.com/v1/asset/?id=" + IDToDownload);
                request.Method = "GET";

                using (var response = request.GetResponse())
                {
                    pictureBoxDecal.Image = Image.FromStream(response.GetResponseStream());
                }

                    string newName = "";
                for (int i = 0; i < decalName.Text.Length; i++)
                {
                    if (Char.IsLetterOrDigit(decalName.Text[i]))
                        newName += decalName.Text[i];
                }
                decalName.Text = newName;
                //Play sound
                soundDecalDownload.Play();
                decalDownloadExportButton.Enabled = true;
                decalDownloadPNG.Enabled = true;
                decalDownloadVMT.Enabled = true;

            }
            catch (WebException)
            {
                decalDownloadExportButton.Enabled = false;
                decalDownloadPNG.Enabled = false;
                decalDownloadVMT.Enabled = false;
                if (pictureBoxDecal.Image != null)
                    pictureBoxDecal.Image = new Bitmap(1, 1);
                decalName.Text = "Invalid-ID";
                return;
            }
            catch (InvalidDataException)
            {
                decalDownloadExportButton.Enabled = false;
                decalDownloadPNG.Enabled = false;
                decalDownloadVMT.Enabled = false;
                if (pictureBoxDecal.Image != null)
                    pictureBoxDecal.Image = new Bitmap(1, 1);
                decalName.Text = "Not-A-Decal";
                return;
            }

        }

        //Checks wether we can export at the current time.
        private bool isOutputPathValid(bool exportVMT)
        {
            if (!Directory.Exists(textboxExport.Text))
            {
                MessageBox.Show("Please configure a valid export path!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if(exportVMT)
            {
                if (string.IsNullOrWhiteSpace(textboxVMTDirectory.Text) || string.IsNullOrEmpty(textboxVMTDirectory.Text))
                {
                    MessageBox.Show("Please configure a VMT path!\n\nExample:\n\"*YourName*\\*ProjectName*\"", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            return true;
        }

        private void decalDownloadExportButton_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(true))
            {
                bool ConfirmedTransparent = false;
                Image outputImage = pictureBoxDecal.Image;
                string outputname = decalName.Text;

                if (numericDecalTransparency.Value != 100)
                {
                    outputname += "-trans_" + numericDecalTransparency.Value;
                    outputImage = ChangeTransparency(outputImage, numericDecalTransparency.Value);
                    ConfirmedTransparent = true;
                }

                //Prevents overwriting
                if (ConfirmedTransparent)
                {
                    if (File.Exists(textboxExport.Text + "\\" + outputname + ".png"))
                    {
                        int counter = 1;
                        while (File.Exists(textboxExport.Text + "\\" + outputname +"-"+ counter + ".png"))
                        {
                            counter++;
                        }
                        outputname += "-"+counter;
                    }
                }
                else
                {
                    if (File.Exists(textboxExport.Text + "\\" + outputname + ".png"))
                    {
                        int counter = 1;
                        while (File.Exists(textboxExport.Text + "\\" + outputname + counter + ".png"))
                        {
                            counter++;
                        }
                        outputname += counter;
                    }
                }

                //Now we need to build the vmt

                Image dummyimage = (Image)outputImage.Clone();
                if (!ExportVMT(outputname, "decals", dummyimage))
                {
                    MessageBox.Show("VMT Failed to Save", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                dummyimage.Dispose();


                outputImage.Save(textboxExport.Text + "\\" +outputname+".png", ImageFormat.Png);
                soundTada.Play();
            }
            this.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "Any Files (*.*)|*.*";
            ofd.CheckFileExists = false;
            ofd.FileName = "[Folder Selection]";
            if(ofd.ShowDialog() == DialogResult.OK)
            {
                if (Directory.Exists(Directory.GetParent(ofd.FileName).FullName))
                {
                    textboxExport.Text = Directory.GetParent(ofd.FileName).FullName;
                    Properties.Settings.Default.ExportPath = textboxExport.Text;
                    Properties.Settings.Default.Save();
                }
                else
                    return;
            }
        }


        private void textboxVMTDirectory_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.VMTDirectory = textboxVMTDirectory.Text;
            Properties.Settings.Default.Save();
        }

        private void buttonColourSelect_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            ColorDialog dlg = new ColorDialog();
            dlg.Color = pictureboxColour.BackColor;
            dlg.AnyColor = true;

            if(dlg.ShowDialog() == DialogResult.OK)
            {
                pictureboxColour.BackColor = dlg.Color;
                pictureboxColouredDecal.BackColor = dlg.Color;
                Properties.Settings.Default.DecalPainterColour = dlg.Color;
                Properties.Settings.Default.Save();
                soundPaint.Play();
            }
            dlg.Dispose();
            this.Enabled = true;
        }

        private void Form1_Moved(object sender, EventArgs e)
        {
            Properties.Settings.Default.FormStartLocation = this.Location;
            Properties.Settings.Default.Save();
        }

        private void Form1_Closing(object sender, EventArgs e)
        {
            timer.Stop();
            if(timer.ElapsedMilliseconds <= 5000)
            {
                Properties.Settings.Default.PromptPositionReset = true;
                Properties.Settings.Default.Save();
            }
        }

        private void pictureboxColouredDecal_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "PNG Image Files (*.png)|*.png";
            if(ofd.ShowDialog() == DialogResult.OK)
            {
                if (pictureboxColouredDecal.Image != null)
                    pictureboxColouredDecal.Image.Dispose();
                //This stops the file system from hogging this file
                pictureboxColouredDecal.Image = (Image)Image.FromFile(ofd.FileName).Clone();
                if (!decalPaintExportButton.Enabled && !decalPaintExportPNGButton.Enabled && !decalPaintExportVMTButton.Enabled)
                {
                    decalPaintExportButton.Enabled = true;
                    decalPaintExportPNGButton.Enabled = true;
                    decalPaintExportVMTButton.Enabled = true;
                }
                soundPing.Play();
                DecalOverlayImageName = ofd.SafeFileName.Substring(0, ofd.SafeFileName.Length - 4);
            }
            ofd.Dispose();
        }

        private void decalDownloadPNG_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(false))
            {
                bool ConfirmedTransparent = false;
                Image outputImage = pictureBoxDecal.Image;
                string outputname = decalName.Text;

                if (numericDecalTransparency.Value != 100)
                {
                    outputname += "-trans_" + numericDecalTransparency.Value;
                    outputImage = ChangeTransparency(outputImage, numericDecalTransparency.Value);
                    ConfirmedTransparent = true;
                }

                //Prevents overwriting
                if (ConfirmedTransparent)
                {
                    if (File.Exists(textboxExport.Text + "\\" + outputname + ".png"))
                    {
                        int counter = 1;
                        while (File.Exists(textboxExport.Text + "\\" + outputname + "-" + counter + ".png"))
                        {
                            counter++;
                        }
                        outputname += "-" + counter;
                    }
                }
                else
                {
                    if (File.Exists(textboxExport.Text + "\\" + outputname + ".png"))
                    {
                        int counter = 1;
                        while (File.Exists(textboxExport.Text + "\\" + outputname + counter + ".png"))
                        {
                            counter++;
                        }
                        outputname += counter;
                    }
                }


                outputImage.Save(textboxExport.Text + "\\" + outputname + ".png", ImageFormat.Png);
                soundTada.Play();
            }
            this.Enabled = true;
        }

        private void decalDownloadVMT_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(true))
            {
                bool ConfirmedTransparent = false;
                Image outputImage = pictureBoxDecal.Image;
                string outputname = decalName.Text;

                if (numericDecalTransparency.Value != 100)
                {
                    outputname += "-trans_" + numericDecalTransparency.Value;
                    outputImage = ChangeTransparency(outputImage, numericDecalTransparency.Value);
                    ConfirmedTransparent = true;
                }

                //Prevents overwriting
                if (ConfirmedTransparent)
                {
                    if (File.Exists(textboxExport.Text + "\\" + outputname + ".vmt"))
                    {
                        int counter = 1;
                        while (File.Exists(textboxExport.Text + "\\" + outputname + "-" + counter + ".vmt"))
                        {
                            counter++;
                        }
                        outputname += "-" + counter;
                    }
                }
                else
                {
                    if (File.Exists(textboxExport.Text + "\\" + outputname + ".vmt"))
                    {
                        int counter = 1;
                        while (File.Exists(textboxExport.Text + "\\" + outputname + counter + ".vmt"))
                        {
                            counter++;
                        }
                        outputname += counter;
                    }
                }

                //Now we need to build the vmt

                Image dummyimage = (Image)outputImage.Clone();
                if (!ExportVMT(outputname, "decals", dummyimage))
                {
                    MessageBox.Show("VMT Failed to Save", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                dummyimage.Dispose();

                soundTada.Play();
            }
            this.Enabled = true;
        }

        private bool ExportVMT(string materialname, string materialparentfolder, Image OutputImage)
        {
            Image Template = OutputImage;
            if (!isOutputPathValid(true))
            {
                return false;
            }
            string Shader = ((ShaderTypes)comboShaderType.SelectedIndex).ToString();
            string BaseTexture;
            if (checkVMTCategory.Checked)
            {
                BaseTexture = "$basetexture " + textboxVMTDirectory.Text + "\\" + materialparentfolder + "\\" + materialname;
            }
            else
            {
                BaseTexture = "$basetexture " + textboxVMTDirectory.Text+ "\\" + materialname;
            }
            
            string SurfaceProp = "$surfaceprop " + ((MaterialTypes) comboSurfaceType.SelectedIndex).ToString();
            string Translucent = "";

            if (IsTransparent(Template))
                Translucent = "$translucent 1";
            else
                Translucent = "$translucent 0";
            
            //Get rid of this
            if (File.Exists(textboxExport.Text + "\\" + materialname + ".vmt") && false)
            {
                if (MessageBox.Show("VMT File Already Exists? Overwrite?", "Clash", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    Template.Dispose();
                    return false;
                }
                    
            }
            try
            {
                FileStream OutputVMT = File.Create(textboxExport.Text + "\\" + materialname + ".vmt");
                BinaryWriter binaryWriter = new BinaryWriter(OutputVMT);
                OutputVMT.Position = 0;
                binaryWriter.Write((Shader+"\n{\n\t"+BaseTexture+"\n\t"+SurfaceProp+"\n\t"+Translucent+"\n}").ToCharArray());
                OutputVMT.Dispose();

            }
            catch (IOException)
            {
                Template.Dispose();
                return false;
            }


            Template.Dispose();
            return true;
        }

        public bool IsTransparent(Image Input)
        {
            Bitmap ImageData = (Bitmap)Input.Clone();
            for(int i = 0; i < ImageData.Width; i++)
            {
                for(int x = 0; x < ImageData.Height; x++)
                {
                    if (ImageData.GetPixel(i, x).A < 255)
                    {
                        ImageData.Dispose();
                        return true;
                    }
                }
            }
            ImageData.Dispose();
            return false;
        }


        private void comboShaderType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ShaderType = comboShaderType.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        private void comboSurfaceType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.SurfaceType = comboSurfaceType.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        private void decalName_TextChanged(object sender, EventArgs e)
        {
            string newName = "";
            for(int i = 0; i < decalName.Text.Length; i++)
            {
                if(Char.IsLetterOrDigit(decalName.Text[i]) || decalName.Text[i]=='_' || decalName.Text[i] == '-')
                    newName+=decalName.Text[i];
            }
            decalName.Text = newName;
        }

        private Image ChangeTransparency(Image InputImage, Decimal InputTransparencyLevel)
        {
            if (InputTransparencyLevel == 100)
                return InputImage;

            Bitmap InputCanvas = (Bitmap)InputImage;
            Bitmap OutputCanvas = new Bitmap(InputImage.Width, InputImage.Height);
            for(int i = 0; i < InputCanvas.Width; i++)
            {
                for(int x = 0; x < InputCanvas.Height; x++)
                {
                    Color Final = InputCanvas.GetPixel(i, x);
                    Final = Color.FromArgb(Convert.ToInt32(Final.A * (InputTransparencyLevel / 100)), Final.R, Final.G, Final.B);
                    OutputCanvas.SetPixel(i, x, Final);
                }
            }


            return OutputCanvas;
        }

        private void decalPaintExportButton_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(true))
            {
                Image overlay = pictureboxColouredDecal.Image;

                //Thanks https://stackoverflow.com/questions/1720160/how-do-i-fill-a-bitmap-with-a-solid-color
                Bitmap canvas = new Bitmap(overlay.Width, overlay.Height);
                using (Graphics gfx = Graphics.FromImage(canvas))
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(pictureboxColouredDecal.BackColor.R, pictureboxColouredDecal.BackColor.G, pictureboxColouredDecal.BackColor.B)))
                {
                    gfx.FillRectangle(brush, 0, 0, overlay.Width, overlay.Height);
                }
                Image underlayImage = (Image)canvas;

                string outputname = DecalOverlayImageName;

                if (numericDPDecalTransparency.Value != 100)
                {
                    outputname += "-trans_" + numericDPDecalTransparency.Value;
                    overlay = ChangeTransparency(overlay, numericDPDecalTransparency.Value);
                }
                else
                    outputname +="-trans_100";

                if (numericDPColourTransparency.Value != 100)
                {
                    outputname += "-trans_" + numericDPColourTransparency.Value;
                    underlayImage = ChangeTransparency(underlayImage, numericDPColourTransparency.Value);
                }
                else
                    outputname += "-trans_100";

                Image outputImage = (Image)new Bitmap(overlay.Width, overlay.Height);
                using (Graphics gr = Graphics.FromImage(outputImage))
                {
                    gr.DrawImage(underlayImage, 0, 0, outputImage.Width, outputImage.Height);
                    gr.DrawImage(overlay, 0, 0, outputImage.Width, outputImage.Height);
                }


                //Now we need to build the vmt

                Image dummyimage = (Image)outputImage.Clone();
                if (!ExportVMT(outputname, "decals", dummyimage))
                {
                    MessageBox.Show("VMT Failed to Save", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                dummyimage.Dispose();


                outputImage.Save(textboxExport.Text + "\\" +outputname+".png", ImageFormat.Png);
                soundTada.Play();
            }
            this.Enabled = true;
        }

        private void decalPaintExportPNGButton_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(false))
            {
                Image overlay = pictureboxColouredDecal.Image;

                //Thanks https://stackoverflow.com/questions/1720160/how-do-i-fill-a-bitmap-with-a-solid-color
                Bitmap canvas = new Bitmap(overlay.Width, overlay.Height);
                using (Graphics gfx = Graphics.FromImage(canvas))
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(pictureboxColouredDecal.BackColor.R, pictureboxColouredDecal.BackColor.G, pictureboxColouredDecal.BackColor.B)))
                {
                    gfx.FillRectangle(brush, 0, 0, overlay.Width, overlay.Height);
                }
                Image underlayImage = (Image)canvas;

                string outputname = DecalOverlayImageName;

                if (numericDPDecalTransparency.Value != 100)
                {
                    outputname += "-trans_" + numericDPDecalTransparency.Value;
                    overlay = ChangeTransparency(overlay, numericDPDecalTransparency.Value);
                }
                else
                    outputname += "-trans_100";

                if (numericDPColourTransparency.Value != 100)
                {
                    outputname += "-trans_" + numericDPColourTransparency.Value;
                    underlayImage = ChangeTransparency(underlayImage, numericDPColourTransparency.Value);
                }
                else
                    outputname += "-trans_100";

                Image outputImage = (Image)new Bitmap(overlay.Width, overlay.Height);
                using (Graphics gr = Graphics.FromImage(outputImage))
                {
                    gr.DrawImage(underlayImage, 0, 0, outputImage.Width, outputImage.Height);
                    gr.DrawImage(overlay, 0, 0, outputImage.Width, outputImage.Height);
                }




                outputImage.Save(textboxExport.Text + "\\" + outputname + ".png", ImageFormat.Png);
                soundTada.Play();
            }
            this.Enabled = true;
        }

        private void decalPaintExportVMTButton_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(true))
            {
                Image overlay = pictureboxColouredDecal.Image;

                //Thanks https://stackoverflow.com/questions/1720160/how-do-i-fill-a-bitmap-with-a-solid-color
                Bitmap canvas = new Bitmap(overlay.Width, overlay.Height);
                using (Graphics gfx = Graphics.FromImage(canvas))
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(pictureboxColouredDecal.BackColor.R, pictureboxColouredDecal.BackColor.G, pictureboxColouredDecal.BackColor.B)))
                {
                    gfx.FillRectangle(brush, 0, 0, overlay.Width, overlay.Width);
                }
                Image underlayImage = (Image)canvas;

                string outputname = DecalOverlayImageName;

                if (numericDPDecalTransparency.Value != 100)
                {
                    outputname += "-trans_" + numericDPDecalTransparency.Value;
                    overlay = ChangeTransparency(overlay, numericDPDecalTransparency.Value);
                }
                else
                    outputname += "-trans_100";

                if (numericDPColourTransparency.Value != 100)
                {
                    outputname += "-trans_" + numericDPColourTransparency.Value;
                    underlayImage = ChangeTransparency(underlayImage, numericDPColourTransparency.Value);
                }
                else
                    outputname += "-trans_100";

                Image outputImage = (Image)new Bitmap(overlay.Width, overlay.Height);
                using (Graphics gr = Graphics.FromImage(outputImage))
                {
                    gr.DrawImage(underlayImage, 0, 0, outputImage.Width, outputImage.Height);
                    gr.DrawImage(overlay, 0, 0, outputImage.Width, outputImage.Height);
                }


                //Now we need to build the vmt

                Image dummyimage = (Image)outputImage.Clone();
                if (!ExportVMT(outputname, "decals", dummyimage))
                {
                    MessageBox.Show("VMT Failed to Save", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                dummyimage.Dispose();
                soundTada.Play();
            }
            this.Enabled = true;
        }

        private void comboMaterialType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.MaterialType = comboMaterialType.SelectedIndex;
            Properties.Settings.Default.Save();

        }

        private Image GenerateMaterial(bool sendtoPreview)
        {
            //Base value, used as a fallback
            byte ImageTweak = 126;

            //This might not be neccesary, but this allows for minor adjustments
            //for each image, as some are darker or lighter.
            switch ((ROBLOXMaterial)comboMaterialType.SelectedIndex)
            {
                case ROBLOXMaterial.wood:
                    ImageTweak = 244;
                    break;
                case ROBLOXMaterial.woodplanks:
                    ImageTweak = 247;
                    break;
                case ROBLOXMaterial.slate:
                    ImageTweak = 217;
                    break;
                case ROBLOXMaterial.concrete:
                    ImageTweak = 203;
                    break;
                case ROBLOXMaterial.metal:
                    ImageTweak = 240;
                    break;
                case ROBLOXMaterial.corrodedmetal:
                    ImageTweak = 148;
                    break;
                case ROBLOXMaterial.diamondplate:
                    ImageTweak = 230;
                    break;
                case ROBLOXMaterial.grass:
                    ImageTweak = 198;
                    break;
                case ROBLOXMaterial.brick:
                    ImageTweak = 222;
                    break;
                case ROBLOXMaterial.sand:
                    ImageTweak = 238;
                    break;
                case ROBLOXMaterial.fabric:
                    ImageTweak = 132;
                    break;
                case ROBLOXMaterial.granite:
                    ImageTweak = 255;
                    break;
                case ROBLOXMaterial.marble:
                    ImageTweak = 240;
                    break;
                case ROBLOXMaterial.pebble:
                    ImageTweak = 239;
                    break;
                case ROBLOXMaterial.cobblestone:
                    ImageTweak = 238;
                    break;
            }

            Bitmap TemplateImage = BaseImages[comboMaterialType.SelectedIndex];


            //Output image, blank at the moment
            Bitmap ExitImage = new Bitmap(1024, 1024);
            int NumberOfPixels = 1024 * 1024;
            int DeltaX = 0;
            int DeltaY = 0;
            byte localR;
            byte localG;
            byte localB;
            byte localA = Convert.ToByte(255*(numericMaterialTransaprency.Value / 100));
            int toomanyexceptions = 0;
            for (int i = 0; i < NumberOfPixels; i++)
            {
                try
                {
                    if (toomanyexceptions >= 1)
                        throw new InvalidDataException();
                    localR = pictureboxMaterialColour.BackColor.R;
                    localG = pictureboxMaterialColour.BackColor.G;
                    localB = pictureboxMaterialColour.BackColor.B;
                    if (DeltaX == 1024)
                    {
                        DeltaX = 0;
                        DeltaY++;
                    }
                    Color ImprintPixel = TemplateImage.GetPixel(DeltaX, DeltaY);

                    if (ImprintPixel.R > ImageTweak)
                    {
                        byte offset = Convert.ToByte(ImprintPixel.R - ImageTweak);
                        try
                        {
                            offset = Convert.ToByte((offset * Convert.ToSingle(numericMaterialIntensity.Value)));
                        }
                        catch
                        {
                            offset = 255;
                            toomanyexceptions++;
                        }
                        localR = (byte)Ext.Clamp(localR + offset, 0, 255);
                        localG = (byte)Ext.Clamp(localG + offset, 0, 255);
                        localB = (byte)Ext.Clamp(localB + offset, 0, 255);

                    }
                    if (ImprintPixel.R < ImageTweak)
                    {
                        byte offset = Convert.ToByte(ImageTweak - ImprintPixel.R);
                        try
                        {
                            offset = Convert.ToByte(offset * Convert.ToSingle(numericMaterialIntensity.Value));
                        }
                        catch
                        {
                            offset = 255;
                            toomanyexceptions++;
                        }
                        localR = (byte)Ext.Clamp(localR - offset, 0, 255);
                        localG = (byte)Ext.Clamp(localG - offset, 0, 255);
                        localB = (byte)Ext.Clamp(localB - offset, 0, 255);
                    }
                    ExitImage.SetPixel(DeltaX, DeltaY, Color.FromArgb(localA, localR, localG, localB));


                    DeltaX++;
                }
                catch (InvalidDataException)
                {
                    //MessageBox.Show("Material Painter Failed!\nTurn down the intensity!", "Turn it down", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    soundFail.Play();
                    if (sendtoPreview)
                    {
                        if (pictureboxMaterial.Image != null)
                        {
                            pictureboxMaterial.Image.Dispose();
                            pictureboxMaterial.Image = (Image)Properties.Resources.ErrorTexture;
                            return Properties.Resources.ErrorTexture;
                        }
                    }
                    return null;
                }
                
            }


            //This allows for other images to be overlayed on top, useful for the brick and cmetal materials
            Graphics Overlay = Graphics.FromImage(ExitImage);
            switch ((ROBLOXMaterial)comboMaterialType.SelectedIndex)
            {
                case ROBLOXMaterial.corrodedmetal:
                    Overlay.DrawImage(Properties.Resources.corrodedmetal_overlay, 0, 0, 1024, 1024);
                    break;
                case ROBLOXMaterial.brick:
                    Overlay.DrawImage(Properties.Resources.brick_overlay, 0, 0, 1024, 1024);
                    break;
            }

            //We don't need it anymore
            Overlay.Dispose();
            if (sendtoPreview)
            {
                if(pictureboxMaterial.Image != null)
                {
                    pictureboxMaterial.Image.Dispose();
                    pictureboxMaterial.Image = (Image)ExitImage;
                    soundMaterialPaint.Play();
                    return null;
                }
            }
            return (Image)ExitImage;

        }

        private void numericMaterialIntensity_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Intensity = numericMaterialIntensity.Value;
            Properties.Settings.Default.Save();;
        }

        private void buttonMaterialColourSelect_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            ColorDialog dlg = new ColorDialog();
            dlg.Color = pictureboxMaterialColour.BackColor;
            dlg.AnyColor = true;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                pictureboxMaterialColour.BackColor = dlg.Color;
                Properties.Settings.Default.MaterialColour = dlg.Color;
                Properties.Settings.Default.Save();
            }
            dlg.Dispose();
            this.Enabled = true;
        }

        private void buttonMaterialUpdate_Click(object sender, EventArgs e)
        {
            this.Enabled=false;
            GenerateMaterial(true);
            buttonMaterialExport.Enabled = true;
            buttonMaterialExportPNG.Enabled = true;
            buttonMaterialExportVMT.Enabled = true;
            this.Enabled = true;
        }

        private void buttonMaterialExport_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(true))
            {
                bool ConfirmedTransparent = false;
                Image outputImage = pictureboxMaterial.Image;
                string outputname = ((ROBLOXMaterial)comboMaterialType.SelectedIndex).ToString() + "_" + pictureboxMaterialColour.BackColor.R + "_" + pictureboxMaterialColour.BackColor.G + "_" + pictureboxMaterialColour.BackColor.B;


                if (numericMaterialTransaprency.Value != 100)
                {
                    outputname += "-trans_" + numericMaterialTransaprency.Value;
                    outputImage = ChangeTransparency(outputImage, numericMaterialTransaprency.Value);
                    ConfirmedTransparent = true;
                }

                //Prevents overwriting
                if (ConfirmedTransparent)
                {
                    if (File.Exists(textboxExport.Text + "\\" + outputname + ".png"))
                    {
                        int counter = 1;
                        while (File.Exists(textboxExport.Text + "\\" + outputname + "-" + counter + ".png"))
                        {
                            counter++;
                        }
                        outputname += "-" + counter;
                    }
                }
                else
                {
                    if (File.Exists(textboxExport.Text + "\\" + outputname + ".png"))
                    {
                        int counter = 1;
                        while (File.Exists(textboxExport.Text + "\\" + outputname + counter + ".png"))
                        {
                            counter++;
                        }
                        outputname += counter;
                    }
                }

                //Now we need to build the vmt

                Image dummyimage = (Image)outputImage.Clone();
                if (!ExportVMT(outputname, "materials", dummyimage))
                {
                    MessageBox.Show("VMT Failed to Save", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                dummyimage.Dispose();


                outputImage.Save(textboxExport.Text + "\\" + outputname + ".png", ImageFormat.Png);
                soundTada.Play();
            }
            this.Enabled = true;
        }

        private void buttonMaterialExportVMT_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(true))
            {
                bool ConfirmedTransparent = false;
                Image outputImage = pictureboxMaterial.Image;
                string outputname = ((ROBLOXMaterial)comboMaterialType.SelectedIndex).ToString() + "_" + pictureboxMaterialColour.BackColor.R + "_" + pictureboxMaterialColour.BackColor.G + "_" + pictureboxMaterialColour.BackColor.B;


                if (numericMaterialTransaprency.Value != 100)
                {
                    outputname += "-trans_" + numericMaterialTransaprency.Value;
                    ConfirmedTransparent = true;
                }

                //Prevents overwriting
                if (ConfirmedTransparent)
                {
                    if (File.Exists(textboxExport.Text + "\\" + outputname + ".vmt"))
                    {
                        int counter = 1;
                        while (File.Exists(textboxExport.Text + "\\" + outputname + "-" + counter + ".vmt"))
                        {
                            counter++;
                        }
                        outputname += "-" + counter;
                    }
                }
                else
                {
                    if (File.Exists(textboxExport.Text + "\\" + outputname + ".vmt"))
                    {
                        int counter = 1;
                        while (File.Exists(textboxExport.Text + "\\" + outputname + counter + ".vmt"))
                        {
                            counter++;
                        }
                        outputname += counter;
                    }
                }

                //Now we need to build the vmt

                Image dummyimage = (Image)outputImage.Clone();
                if (!ExportVMT(outputname, "materials", dummyimage))
                {
                    MessageBox.Show("VMT Failed to Save", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                dummyimage.Dispose();

                soundTada.Play();
            }
            this.Enabled = true;
        }

        private void buttonMaterialExportPNG_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(true))
            {
                bool ConfirmedTransparent = false;
                Image outputImage = pictureboxMaterial.Image;
                string outputname = ((ROBLOXMaterial)comboMaterialType.SelectedIndex).ToString() + "_" + pictureboxMaterialColour.BackColor.R + "_" + pictureboxMaterialColour.BackColor.G + "_" + pictureboxMaterialColour.BackColor.B;


                if (numericMaterialTransaprency.Value != 100)
                {
                    outputname += "-trans_" + numericMaterialTransaprency.Value;
                    outputImage = ChangeTransparency(outputImage, numericMaterialTransaprency.Value);
                    ConfirmedTransparent = true;
                }

                //Prevents overwriting
                if (ConfirmedTransparent)
                {
                    if (File.Exists(textboxExport.Text + "\\" + outputname + ".png"))
                    {
                        int counter = 1;
                        while (File.Exists(textboxExport.Text + "\\" + outputname + "-" + counter + ".png"))
                        {
                            counter++;
                        }
                        outputname += "-" + counter;
                    }
                }
                else
                {
                    if (File.Exists(textboxExport.Text + "\\" + outputname + ".png"))
                    {
                        int counter = 1;
                        while (File.Exists(textboxExport.Text + "\\" + outputname + counter + ".png"))
                        {
                            counter++;
                        }
                        outputname += counter;
                    }
                }

                outputImage.Save(textboxExport.Text + "\\" + outputname + ".png", ImageFormat.Png);
                soundTada.Play();
            }
            this.Enabled = true;
        }


        private Image GenerateStuds(Bitmap Imprint, Color color, float intensity)
        {
            if (Imprint == null)
            {
                Bitmap Bmp = new Bitmap(templateOutlets.Width, templateOutlets.Height);
                using (Graphics gfx = Graphics.FromImage(Bmp))
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, color.R, color.G, color.B)))
                {
                    gfx.FillRectangle(brush, 0, 0, templateOutlets.Width, templateOutlets.Height);
                }
                return Bmp;
            }
            Bitmap TemplateImage = Imprint;
            Bitmap ExitImage = new Bitmap(TemplateImage.Width, TemplateImage.Height);
            int NumberOfPixels = TemplateImage.Width * TemplateImage.Height;
            int DeltaX = 0;
            int DeltaY = 0;
            byte localR;
            byte localG;
            byte localB;
            for (int i = 0; i < NumberOfPixels; i++)
            {
                localR = color.R;
                localG = color.G;
                localB = color.B;
                if (DeltaX == TemplateImage.Width)
                {
                    DeltaX = 0;
                    DeltaY++;
                }
                Color ImprintPixel = TemplateImage.GetPixel(DeltaX, DeltaY);

                if (ImprintPixel.R > 128)
                {
                    byte offset = Convert.ToByte(ImprintPixel.R - 128);
                    offset = Convert.ToByte(offset * intensity);
                    localR = (byte)Ext.Clamp(localR + offset, 0, 255);
                    localG = (byte)Ext.Clamp(localG + offset, 0, 255);
                    localB = (byte)Ext.Clamp(localB + offset, 0, 255);

                }
                if (ImprintPixel.R < 128)
                {
                    byte offset = Convert.ToByte(128 - ImprintPixel.R);
                    offset = Convert.ToByte(offset * intensity);
                    localR = (byte)Ext.Clamp(localR - offset, 0, 255);
                    localG = (byte)Ext.Clamp(localG - offset, 0, 255);
                    localB = (byte)Ext.Clamp(localB - offset, 0, 255);
                }
                ExitImage.SetPixel(DeltaX, DeltaY, Color.FromArgb(255, localR, localG, localB));

                DeltaX++;
            }
            return ExitImage;
        }

        private void buttonStudsInvert_Click(object sender, EventArgs e)
        {
            checkStudsOutlets.Checked = !checkStudsOutlets.Checked;
            checkStudsInlets.Checked = !checkStudsInlets.Checked;
            checkStudsUniversal.Checked = !checkStudsUniversal.Checked;
            checkStudsSmooth.Checked = !checkStudsSmooth.Checked;
            checkStudsGlue.Checked = !checkStudsGlue.Checked;
            checkStudsChanged(null,null);
        }

        private void buttonStudsAll_Click(object sender, EventArgs e)
        {
            checkStudsOutlets.Checked = true;
            checkStudsInlets.Checked = true;
            checkStudsUniversal.Checked = true;
            checkStudsSmooth.Checked = true;
            checkStudsGlue.Checked = true;
            checkStudsChanged(null, null);
        }

        private void buttonStudsNone_Click(object sender, EventArgs e)
        {
            checkStudsOutlets.Checked = false;
            checkStudsInlets.Checked = false;
            checkStudsUniversal.Checked = false;
            checkStudsSmooth.Checked = false;
            checkStudsGlue.Checked = false;
            checkStudsChanged(null, null);
        }

        private void buttonStudsSmooth_Click(object sender, EventArgs e)
        {
            checkStudsOutlets.Checked = false;
            checkStudsInlets.Checked = false;
            checkStudsUniversal.Checked = false;
            checkStudsSmooth.Checked = true;
            checkStudsGlue.Checked = false;
            checkStudsChanged(null, null);
        }

        private void buttonStudsColourSelect_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            ColorDialog dlg = new ColorDialog();
            dlg.Color = pictureboxStudsColour.BackColor;
            dlg.AnyColor = true;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                pictureboxStudsColour.BackColor = dlg.Color;
                Properties.Settings.Default.StudsColour = dlg.Color;
                Properties.Settings.Default.Save();
            }
            dlg.Dispose();
            this.Enabled = true;
        }

        private void buttonStudsUpdate_Click(object sender, EventArgs e)
        {
            this.Enabled = false;

            pictureboxStudsOutlets.Image = GenerateStuds(templateOutlets, pictureboxStudsColour.BackColor, (float)numericStudsIntensity.Value);
            pictureboxStudsInlets.Image = GenerateStuds(templateInlets, pictureboxStudsColour.BackColor, (float)numericStudsIntensity.Value);
            pictureboxStudsUniversal.Image = GenerateStuds(templateUniversal, pictureboxStudsColour.BackColor, (float)numericStudsIntensity.Value);
            pictureboxStudsSmooth.Image = GenerateStuds(null, pictureboxStudsColour.BackColor, (float)numericStudsIntensity.Value);
            pictureboxStudsGlue.Image = GenerateStuds(templateGlue, pictureboxStudsColour.BackColor, (float)numericStudsIntensity.Value);
            soundStudImprint.Play();
            if (!allowExportStuds)
            {
                allowExportStuds = true;
                buttonStudsExportAll.Enabled = true;
                buttonStudsExportPNG.Enabled = true;
                buttonStudsExportVMT.Enabled = true;
                buttonStudsExportSelected.Enabled = true;
                buttonStudsExportSelectedPNG.Enabled = true;
                buttonStudsExportSelectedVMT.Enabled = true;
            }
            this.Enabled = true;
        }

        private void numericStudsIntensity_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.StudsIntensity = numericStudsIntensity.Value;
            Properties.Settings.Default.Save();
        }

        private void buttonStudsExportAll_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(true))
            {
                studsRef = new Image[] { this.pictureboxStudsOutlets.Image, this.pictureboxStudsInlets.Image , this.pictureboxStudsUniversal.Image, this.pictureboxStudsUniversal.Image, this.pictureboxStudsGlue.Image };
                for (int i = 0; i < 5; i++)
                {
                    Image outputImage = studsRef[i];
                    string outputname = (studsCanvases[i] + "_" + pictureboxMaterialColour.BackColor.R + "_" + pictureboxMaterialColour.BackColor.G + "_" + pictureboxMaterialColour.BackColor.B);


                    if (numericStudsTransparency.Value != 100)
                    {
                        outputname += "-trans_" + numericStudsTransparency.Value;
                        outputImage = ChangeTransparency(outputImage, numericStudsTransparency.Value);
                    }

                    //Now we need to build the vmt

                    Image dummyimage = (Image)outputImage.Clone();
                    if (!ExportVMT(outputname, "studs", dummyimage))
                    {
                        MessageBox.Show("VMT Failed to Save", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    dummyimage.Dispose();


                    outputImage.Save(textboxExport.Text + "\\" + outputname + ".png", ImageFormat.Png);
                }  
                soundTada.Play();
            }
            this.Enabled = true;
        }

        private void buttonStudsExportPNG_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(true))
            {
                studsRef = new Image[] { this.pictureboxStudsOutlets.Image, this.pictureboxStudsInlets.Image, this.pictureboxStudsUniversal.Image, this.pictureboxStudsSmooth.Image, this.pictureboxStudsGlue.Image };
                for (int i = 0; i < 5; i++)
                {
                    Image outputImage = studsRef[i];
                    string outputname = (studsCanvases[i] + "_" + pictureboxStudsColour.BackColor.R + "_" + pictureboxStudsColour.BackColor.G + "_" + pictureboxStudsColour.BackColor.B);


                    if (numericStudsTransparency.Value != 100)
                    {
                        outputname += "-trans_" + numericStudsTransparency.Value;
                        outputImage = ChangeTransparency(outputImage, numericStudsTransparency.Value);
                    }

                    //Now we need to build the vmt

                    outputImage.Save(textboxExport.Text + "\\" + outputname + ".png", ImageFormat.Png);
                }
                soundTada.Play();
            }
            this.Enabled = true;
        }

        private void buttonStudsExportVMT_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(true))
            {
                studsRef = new Image[] { this.pictureboxStudsOutlets.Image, this.pictureboxStudsInlets.Image, this.pictureboxStudsUniversal.Image, this.pictureboxStudsUniversal.Image, this.pictureboxStudsGlue.Image };
                for (int i = 0; i < 5; i++)
                {
                    Image outputImage = studsRef[i];
                    string outputname = (studsCanvases[i] + "_" + pictureboxStudsColour.BackColor.R + "_" + pictureboxStudsColour.BackColor.G + "_" + pictureboxStudsColour.BackColor.B);


                    if (numericStudsTransparency.Value != 100)
                    {
                        outputname += "-trans_" + numericStudsTransparency.Value;
                        outputImage = ChangeTransparency(outputImage, numericStudsTransparency.Value);
                    }

                    Image dummyimage = (Image)outputImage.Clone();
                    if (!ExportVMT(outputname, "studs", dummyimage))
                    {
                        MessageBox.Show("VMT Failed to Save", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    dummyimage.Dispose();
                }
                soundTada.Play();
            }
            this.Enabled = true;
        }

        private void buttonStudsExportSelected_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(true))
            {
                studsRef = new Image[] { this.pictureboxStudsOutlets.Image, this.pictureboxStudsInlets.Image, this.pictureboxStudsUniversal.Image, this.pictureboxStudsSmooth.Image, this.pictureboxStudsGlue.Image };
                studsChecked = new bool[] { this.checkStudsOutlets.Checked, this.checkStudsInlets.Checked, this.checkStudsUniversal.Checked, this.checkStudsSmooth.Checked, this.checkStudsGlue.Checked };
                for (int i = 0; i < 5; i++)
                {
                    if (!studsChecked[i])
                    {
                        continue;
                    }
                    Image outputImage = studsRef[i];
                    string outputname = (studsCanvases[i] + "_" + pictureboxStudsColour.BackColor.R + "_" + pictureboxStudsColour.BackColor.G + "_" + pictureboxStudsColour.BackColor.B);


                    if (numericStudsTransparency.Value != 100)
                    {
                        outputname += "-trans_" + numericStudsTransparency.Value;
                        outputImage = ChangeTransparency(outputImage, numericStudsTransparency.Value);
                    }

                    //Now we need to build the vmt

                    Image dummyimage = (Image)outputImage.Clone();
                    if (!ExportVMT(outputname, "studs", dummyimage))
                    {
                        MessageBox.Show("VMT Failed to Save", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    dummyimage.Dispose();


                    outputImage.Save(textboxExport.Text + "\\" + outputname + ".png", ImageFormat.Png);
                }
                soundTada.Play();
            }
            this.Enabled = true;
        }

        private void checkStudsChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.StudsOutlets = checkStudsOutlets.Checked;
            Properties.Settings.Default.StudsInlets = checkStudsInlets.Checked;
            Properties.Settings.Default.StudsUniversal = checkStudsUniversal.Checked;
            Properties.Settings.Default.StudsSmooth = checkStudsSmooth.Checked;
            Properties.Settings.Default.StudsGlue = checkStudsGlue.Checked;
            Properties.Settings.Default.Save();
        }

        private void checkVMTCategory_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ExportCategory = checkVMTCategory.Checked;
            Properties.Settings.Default.Save();
        }

        private void buttonStudsExportSelectedPNG_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(true))
            {
                studsRef = new Image[] { this.pictureboxStudsOutlets.Image, this.pictureboxStudsInlets.Image, this.pictureboxStudsUniversal.Image, this.pictureboxStudsSmooth.Image, this.pictureboxStudsGlue.Image };
                studsChecked = new bool[] { this.checkStudsOutlets.Checked, this.checkStudsInlets.Checked, this.checkStudsUniversal.Checked, this.checkStudsSmooth.Checked, this.checkStudsGlue.Checked };
                for (int i = 0; i < 5; i++)
                {
                    if (!studsChecked[i])
                    {
                        continue;
                    }
                    Image outputImage = studsRef[i];
                    string outputname = (studsCanvases[i] + "_" + pictureboxStudsColour.BackColor.R + "_" + pictureboxStudsColour.BackColor.G + "_" + pictureboxStudsColour.BackColor.B);


                    if (numericStudsTransparency.Value != 100)
                    {
                        outputname += "-trans_" + numericStudsTransparency.Value;
                        outputImage = ChangeTransparency(outputImage, numericStudsTransparency.Value);
                    }

                    outputImage.Save(textboxExport.Text + "\\" + outputname + ".png", ImageFormat.Png);
                }
                soundTada.Play();
            }
            this.Enabled = true;
        }

        private void buttonStudsExportSelectedVMT_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            if (isOutputPathValid(true))
            {
                studsRef = new Image[] { this.pictureboxStudsOutlets.Image, this.pictureboxStudsInlets.Image, this.pictureboxStudsUniversal.Image, this.pictureboxStudsSmooth.Image, this.pictureboxStudsGlue.Image };
                studsChecked = new bool[] { this.checkStudsOutlets.Checked, this.checkStudsInlets.Checked, this.checkStudsUniversal.Checked, this.checkStudsSmooth.Checked, this.checkStudsGlue.Checked };
                for (int i = 0; i < 5; i++)
                {
                    if (!studsChecked[i])
                    {
                        continue;
                    }
                    Image outputImage = studsRef[i];
                    string outputname = (studsCanvases[i] + "_" + pictureboxStudsColour.BackColor.R + "_" + pictureboxStudsColour.BackColor.G + "_" + pictureboxStudsColour.BackColor.B);


                    if (numericStudsTransparency.Value != 100)
                    {
                        outputname += "-trans_" + numericStudsTransparency.Value;
                        outputImage = ChangeTransparency(outputImage, numericStudsTransparency.Value);
                    }

                    //Now we need to build the vmt

                    Image dummyimage = (Image)outputImage.Clone();
                    if (!ExportVMT(outputname, "studs", dummyimage))
                    {
                        MessageBox.Show("VMT Failed to Save", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    dummyimage.Dispose();
                }
                soundTada.Play();
            }
            this.Enabled = true;
        }

        private void toolStripLabel3_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/rmod8/ROBLOX-Texture-Asset-Studio");
        }

        private void comboMaterialStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.MaterialsStyle = comboMaterialStyle.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        private void ReplaceBaseImages()
        {
            this.Enabled = false;
            switch (comboStudStyle.SelectedIndex)
            {
                case 6:
                    pictureboxStudsOutlets.Image = (Image)Properties.Resources.studs;
                    pictureboxStudsInlets.Image = (Image)Properties.Resources.inlets;
                    pictureboxStudsUniversal.Image = (Image)Properties.Resources.universal;
                    pictureboxStudsGlue.Image = (Image)Properties.Resources.glue;
                    break;

                case 5:
                    pictureboxStudsOutlets.Image = (Image)Properties.Resources.e2017_studs;
                    pictureboxStudsInlets.Image = (Image)Properties.Resources.e2017_inlets;
                    pictureboxStudsUniversal.Image = (Image)Properties.Resources.e2017_universal;
                    pictureboxStudsGlue.Image = (Image)Properties.Resources.e2017_glue;
                    break;

                case 4:
                    pictureboxStudsOutlets.Image = (Image)Properties.Resources._2013_studs;
                    pictureboxStudsInlets.Image = (Image)Properties.Resources._2013_inlets;
                    pictureboxStudsUniversal.Image = (Image)Properties.Resources._2013_universal;
                    pictureboxStudsGlue.Image = (Image)Properties.Resources._2013_glue;
                    break;

                case 3:
                    pictureboxStudsOutlets.Image = (Image)Properties.Resources.e2013_studs;
                    pictureboxStudsInlets.Image = (Image)Properties.Resources.e2013_inlets;
                    pictureboxStudsUniversal.Image = (Image)Properties.Resources.e2013_universal;
                    pictureboxStudsGlue.Image = (Image)Properties.Resources.e2013_glue;
                    break;

                case 2:
                    pictureboxStudsOutlets.Image = (Image)Properties.Resources._2009_studs;
                    pictureboxStudsInlets.Image = (Image)Properties.Resources._2009_inlets;
                    pictureboxStudsUniversal.Image = (Image)Properties.Resources._2009_universal;
                    pictureboxStudsGlue.Image = (Image)Properties.Resources._2009_glue;
                    break;

                case 1:
                    pictureboxStudsOutlets.Image = (Image)Properties.Resources.e2009_studs;
                    pictureboxStudsInlets.Image = (Image)Properties.Resources.e2009_inlets;
                    pictureboxStudsUniversal.Image = (Image)Properties.Resources.e2009_universal;
                    pictureboxStudsGlue.Image = (Image)Properties.Resources.e2009_glue;
                    break;

                case 0:
                    pictureboxStudsOutlets.Image = (Image)Properties.Resources._2006_studs;
                    pictureboxStudsInlets.Image = (Image)Properties.Resources._2006_inlets;
                    pictureboxStudsUniversal.Image = (Image)Properties.Resources._2006_universal;
                    pictureboxStudsGlue.Image = (Image)Properties.Resources._2006_glue;     
                    break;
            }
            templateOutlets = (Bitmap)pictureboxStudsOutlets.Image;
            templateInlets = (Bitmap)pictureboxStudsInlets.Image;
            templateUniversal = (Bitmap)pictureboxStudsUniversal.Image;
            templateGlue = (Bitmap)pictureboxStudsGlue.Image;
            this.Enabled = true;
        }

        private void comboStudStyle_DropDownClosed(object sender, EventArgs e)
        {
            Properties.Settings.Default.StudsStyle = comboStudStyle.SelectedIndex;
            Properties.Settings.Default.Save();
            ReplaceBaseImages();
        }
    }

    //API Classes
    internal class RobloxMarketplaceRequest
    {
        public string TargetId { get; set; }
        public string ProductType { get; set; }
        public string AssetID { get; set; }
        public string ProductId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AssetTypeId { get; set; }
        public RobloxMarketplaceCreator Creator { get; set; }
        public string IconImageAssetID { get; set; }
        public string Created { get; set; }
        public string Updated { get; set; }
        public string PriceInRobux { get; set; }
        public string PriceInTickets { get; set; }
        public string Sales { get; set; }
        public bool IsNew { get; set; }
        public bool IsForSale { get; set; }
        public bool IsPublicDomain { get; set; }
        public bool IsLimited { get; set; }
        public bool IsLimitedUnique { get; set; }
        public string Remaining { get; set; }
        public string MinimumMembershipLevel { get; set; }
        public string ContentRatingTypeId { get; set; }
    }
    internal class RobloxMarketplaceCreator
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string CreatorType { get; set; }
        public string CreatorTargetId { get; set; }
    }

    public static class Ext
    {
        // Source: http://stackoverflow.com/a/2683487/1455541
        public static T Clamp<T>(T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }
    }
}
