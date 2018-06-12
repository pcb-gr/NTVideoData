using NTVideoData.Daos;
using NTVideoData.Util;
using NTVideoData.Victims;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using NTVideoData.Controls;
using NTVideoData.Services;
using NTVideoData_v1;
using NTVideoData_v1.Utils;

namespace NTVideoData
{
    public partial class MainForm : Form
    {
        public bool connectionIsAvailable = false, isInserting = false, isUpdating = false, isDownload = false, isUpload = false;
        private Thread insertThread, updateThread, downloadThread, uploadThread, checkConnectionThread;
        BaseVictim victimService;
        BaseService baseService = new BaseService();
        LogForm logForm = new LogForm();
        int typeToGet;
        public List<string> checkFields = null;

        public MainForm()
        {
            //WebDriverHelper.singleton().ExampleUse();
            //var a =CryptoUtil.encrypt("http://media.phimbathu.com/uploads/2016/08/300/hack-201608339.jpg");
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            PopulateDataToCombobox();
            loadInputDefault();
            InitializeTimerCheckConnection();
        }

        private void loadInputDefault()
        {
            cbSelectOne.SelectedIndex = 0;
            cbVictim.SelectedIndex = 0;
        }

        public Dictionary<int, string> SELECT_ONE = new Dictionary<int, string>()
        {
            {0, "Tất cả"},
            {1, "Phim lẻ"},
            {2, "Phim bộ"},
            {3, "Phim mới"}
        };

        Dictionary<object, string> VICTIM_INSTANCE = new Dictionary<object, string>()
        {
            {new PhimbathuVictim(), "Phim Bất Hủ"},
            {new PhimMoiVictim(), "Phim Mới"},
            {new BilutvVictim(), "Bilutv"},
            {new TvHayOrg(), "TvHayOrg"}
        };

        private void PopulateDataToCombobox()
        {
            // select one combobox
            foreach (KeyValuePair<int, string> pair in SELECT_ONE)
            {
                cbSelectOne.Items.Add(new ComboboxItem() {
                       Text = pair.Value,
                       Value = pair.Key
                });
            }
           
            // victim combobox
            foreach (KeyValuePair<object, string> pair in VICTIM_INSTANCE)
            {
                cbVictim.Items.Add(new ComboboxItem()
                {
                    Text = pair.Value,
                    Value = pair.Key
                });
            }
        }

        private void cbVictim_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboboxItem victimSelected = (ComboboxItem)(((ComboBox)sender).SelectedItem);
            victimService = (BaseVictim)victimSelected.Value;
            victimService.checkFields = checkFields;
            BaseVictim.setInstance(victimService);
            victimService.setLogForm(logForm);
            victimService.showForm();

        }

        private void cbSelectOne_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboboxItem selectOneSelected = (ComboboxItem)(((ComboBox)sender).SelectedItem);
            typeToGet = (int)selectOneSelected.Value;
        }

        private bool isDoStartMysql = false;
        private void InitializeTimerCheckConnection()
        {
            checkConnectionThread = new Thread(() =>
            {
                while (checkConnectionThread.IsAlive)
                {
                    if (!BaseDao.checkConnection() && !isDoStartMysql)
                    {
                        switchVisibleForm(false);
                        connectionIsAvailable = false;
                        CMDUtil.startMysql();
                        isDoStartMysql = true;
                        isInserting = false;
                        isUpdating = false;
                    }
                    else if (BaseDao.checkConnection())
                    {
                        if(checkFields == null)
                        {
                            checkFields = victimService.findCheckFieldAllMovies();
                            victimService.checkFields = checkFields;
                        }
                        
                        switchVisibleForm(true);
                        connectionIsAvailable = true;
                        isDoStartMysql = false;
                    }
                    Thread.Sleep(10);
                }
                
            });
            checkConnectionThread.IsBackground = true;
            checkConnectionThread.SetApartmentState(ApartmentState.STA);
            checkConnectionThread.Start();
        }

        private void InitializeTimerInsert()
        {
            if (insertThread != null)
            {
                return;
            }
            
            insertThread = new Thread(() =>
            {
                while (insertThread.IsAlive && connectionIsAvailable && !isInserting)
                {
                    disableInput();
                    isInserting = true;
                    if (victimService.beginInsertContent(typeToGet))
                    {
                        isInserting = false;
                    }
                }
            });
            insertThread.IsBackground = true;
            insertThread.SetApartmentState(ApartmentState.STA);
            insertThread.Start();
        }

        private void InitializeTimerUpdate()
        {
            if (updateThread != null)
            {
                return;
            }
            updateThread = new Thread(() =>
            {
                while (updateThread.IsAlive && connectionIsAvailable && !isUpdating)
                {
                    isUpdating = true;
                    if (victimService.beginUpdateContent())
                    {
                        isUpdating = false;
                    }
                }
            });
            updateThread.IsBackground = true;
            updateThread.SetApartmentState(ApartmentState.STA);
            updateThread.Start();
        }

        private void InitializeTimerDownload()
        {
            if (downloadThread != null)
            {
                return;
            }
            downloadThread = new Thread(() =>
            {
                while (downloadThread.IsAlive && connectionIsAvailable && !isDownload)
                {
                    isDownload = true;
                    if (baseService.beginDownLoad())
                    {
                        isDownload = false;
                    }
                }
            });
            downloadThread.IsBackground = true;
            downloadThread.SetApartmentState(ApartmentState.STA);
            downloadThread.Start();
        }

        private void InitializeTimerUpload()
        {
            if (uploadThread != null)
            {
                return;
            }
            uploadThread = new Thread(() =>
            {
                while (uploadThread.IsAlive && connectionIsAvailable && !isUpload)
                {
                    isUpload = true;
                    if (baseService.beginUpLoad())
                    {
                        isUpload = false;
                    }
                }
            });
            uploadThread.IsBackground = true;
            uploadThread.SetApartmentState(ApartmentState.STA);
            uploadThread.Start();
        }

        private void btInsert_Click(object sender, EventArgs e)
        {
            InitializeTimerInsert();
            switchInsertBt((Button)(sender));
        }

        private void uploadBt_Click(object sender, EventArgs e)
        {
            InitializeTimerUpload();
            switchUploadBt((Button)(sender));
        }

        

        private void downloadBt_Click(object sender, EventArgs e)
        {
            InitializeTimerDownload();
            switchDownloadBt((Button)(sender));
        }

        private void btUpdate_Click(object sender, EventArgs e)
        {
            InitializeTimerUpdate();
            switchUpdateBt((Button)(sender));
        }

        private void switchUpdateBt(Button btUpdate)
        {
            if (UIUtil.ControlInvokeRequired(this, () => switchUpdateBt(btUpdate))) return;
            if ((btUpdate.Text.IndexOf("Updating") != -1))
            {
                victimService.stopUpdate();
                btUpdate.Text = "Update";
            }
            else
            {
                victimService.startUpdate();
                btUpdate.Text = "Updating !!!\n Click here to stop.";
            }
        }

        private void switchInsertBt(Button btInsert)
        {
            if (UIUtil.ControlInvokeRequired(this, () => switchInsertBt(btInsert))) return;
            if ((btInsert.Text.IndexOf("Inserting") != -1))
            {
                victimService.stopInsert();
                btInsert.Text = "Insert";
            } 
            else
            {
                victimService.startInsert();
                btInsert.Text = "Inserting !!!\n Click here to stop.";
            }
        }

        private void switchDownloadBt(Button btDownload)
        {
            if (UIUtil.ControlInvokeRequired(this, () => switchDownloadBt(btDownload))) return;
            if ((btDownload.Text.IndexOf("Downloading") != -1))
            {
                baseService.stopDownload();
                btDownload.Text = "Download";
            }
            else
            {
                baseService.startDownload();
                btDownload.Text = "Downloading !!!\n Click here to stop.";
            }
        }

        private void switchUploadBt(Button btUpload)
        {
            if (UIUtil.ControlInvokeRequired(this, () => switchUploadBt(btUpload))) return;
            if ((btUpload.Text.IndexOf("Uploading") != -1))
            {
                baseService.stopDownload();
                btUpload.Text = "Upload";
            }
            else
            {
                baseService.startDownload();
                btUpload.Text = "Uploading !!!\n Click here to stop.";
            }
        }

        private void switchVisibleForm(bool state)
        {
            if (UIUtil.ControlInvokeRequired(this, () => switchVisibleForm(state))) return;
            this.Enabled = state;
        }

        private void disableInput()
        {
            if (UIUtil.ControlInvokeRequired(pnInput, () => disableInput())) return;
            pnInput.Enabled = false;
        }
    }
}
