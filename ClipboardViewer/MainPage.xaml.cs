using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static Windows.ApplicationModel.DataTransfer.StandardDataFormats;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace ClipboardViewer
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Clipboard.ContentChanged += ClipboardContentChangedEventHandler;
        }
        ObservableCollection<StorageItemsBinding> storageItems = new ObservableCollection<StorageItemsBinding>();

        private async void ClipboardContentChangedEventHandler(object sender, object e)
        {
            DataPackageView data = Clipboard.GetContent();
            WriteLog("开始获取数据，来源：剪贴板");
            await GetData(data);
            WriteLog("获取数据结束");
        }

        const string ToMushMessage = "包含的内容太大，请保存到文件查看。";

        public bool DoingWork
        {
            set
            {
                grdLoding.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                prgLoading.IsActive = value;
            }
        }

        byte[] imageBytes;
        string webString;
        string rtfString;

        public byte[] ImageBytes
        {
            get => imageBytes;
            set
            {
                imageBytes = value;
                btnImg.IsEnabled = value != null;
            }
        }
        public string WebString
        {
            get => webString;
            set
            {
                webString = value;
                btnWeb.IsEnabled = value != null;
            }
        }
        public string RtfString
        {
            get => rtfString;
            set
            {
                rtfString = value;
                btnRtf.IsEnabled = value != null;
            }
        }

        private async Task GetData(DataPackageView data)
        {
            DoingWork = true;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var i in lbxUsable.Items)
            {
                (i as CheckBox).IsChecked = false;
            }
            ImageBytes = null;
            WebString = null;
            RtfString = null;


            if (data.Contains(Bitmap))
            {
                try
                {
                    var stream = await (await data.GetBitmapAsync()).OpenReadAsync();
                    BitmapImage image = new BitmapImage();
                    await image.SetSourceAsync(stream);
                    img.Source = image;
                   
                    ImageBytes = new byte[stream.Size];
                    stream.Seek(0);
                    await stream.AsStreamForRead().ReadAsync(ImageBytes, 0, (int)stream.Size);
                    if (ImageBytes.Length > 1e5)
                    {

                        txtImg.Text = ToMushMessage;
                    }
                    else
                    {
                        WriteBinaryToTextBox(ImageBytes, txtImg);
                    }
                    chkImage.IsChecked = true;
                }
                catch
                {
                    WriteLog("检测到图片，但是获取失败");
                }
            }


            if (data.Contains(Html))
            {
                try
                {
                    WebString = await data.GetHtmlFormatAsync();
                    int index = WebString.IndexOf('<');
                    if (index != -1)
                    {
                        web.NavigateToString(WebString.Remove(0, index));
                    }

                    if (WebString.Length > 1e5)
                    {
                        txtWeb.Text = ToMushMessage;
                    }
                    else
                    {
                        txtWeb.Text = WebString;
                    }
                    chkHtml.IsChecked = true;
                }
                catch
                {
                    WriteLog("检测到HTML，但是获取失败");
                }
            }

            if (data.Contains(Rtf))
            {
                try
                {
                    RtfString = await data.GetRtfAsync();
                    rtfEdit.IsReadOnly = false;
                    rtfEdit.Document.SetText(Windows.UI.Text.TextSetOptions.FormatRtf, Encoding.Unicode.GetString(new UTF8Encoding(false).GetBytes(RtfString)));
                    rtfEdit.IsReadOnly = true;
                    if (RtfString.Length > 1e5)
                    {
                        txtWeb.Text = ToMushMessage;
                    }
                    else
                    {
                        rtfTxt.Text = RtfString.ToString();
                    }
                    chkRtf.IsChecked = true;
                }
                catch
                {
                    WriteLog("检测到RTF，但是获取失败");
                }
            }
            if (data.Contains(StorageItems))
            {
                storageItems.Clear();
                try
                {
                    var items = await data.GetStorageItemsAsync();
                    foreach (var i in items)
                    {
                        if (i.Attributes.HasFlag(Windows.Storage.FileAttributes.Directory))
                        {

                            storageItems.Add(new StorageItemsBinding()
                            {
                                FileName = i.Name,
                                Size ="",
                                Path = i.Path,
                                Image = new BitmapImage(new Uri(BaseUri, "/Assets/Fonder.png")) ,
                            });
                        }
                        else
                        {
                            StorageItemThumbnail thumbnail = await (i as StorageFile).GetThumbnailAsync(ThumbnailMode.SingleItem);
                            BitmapImage image = new BitmapImage();
                            await image.SetSourceAsync(thumbnail);
                            storageItems.Add(new StorageItemsBinding()
                            {
                                FileName = i.Name,
                                Size = ToReadableSize((await i.GetBasicPropertiesAsync()).Size),
                                Path = i.Path,
                                Image =image, //new BitmapImage(new Uri(BaseUri, "/Assets/File.png")),
                            });
                        }
                    }
                    chkStorage.IsChecked = true;
                }
                catch
                {
                    WriteLog("检测到存储器类型数据，但是获取失败");
                }
            }
            if (data.Contains(Text))
            {
                try
                {
                    txt.Text = await data.GetTextAsync();
                    chkText.IsChecked = true;
                }

                catch
                {
                    WriteLog("检测到文本，但是获取失败");
                }
            }
            if (data.Contains(WebLink))
            {
                try
                {
                    Uri uri = await data.GetWebLinkAsync();
                    txtOriginal.Text = uri.OriginalString;
                    txtAbsPath.Text = uri.AbsolutePath;
                    txtAbsUri.Text = uri.AbsoluteUri;
                    txtAuthority.Text = uri.Authority;
                    txtHost.Text = uri.Host;
                    chkLink.IsChecked = true;
                }
                catch
                {
                    WriteLog("检测到网页链接，但是获取失败");
                }
            }
            stopwatch.Stop();
            tbkStatus.Text = stopwatch.ElapsedMilliseconds.ToString() + "毫秒";
            DoingWork = false;
        }


        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            lvwFiles.ItemsSource = storageItems;
            DataPackageView data = Clipboard.GetContent();
            WriteLog("开始获取数据，来源：剪贴板（启动时自动）");
            await GetData(data);
            WriteLog("获取数据结束");
        }

        private string ToReadableSize(ulong size)
        {
            double sizeD = size;
            if (sizeD < 1024)
            {
                return size + "B";
            }
            sizeD /= 1024;
            if (sizeD < 1024)
            {
                return sizeD.ToString("0.00") + "KB";
            }
            sizeD /= 1024;
            if (sizeD < 1024)
            {
                return sizeD.ToString("0.00") + "MB";
            }
            sizeD /= 1024;
            if (sizeD < 1024)
            {
                return sizeD.ToString("0.00") + "GB";
            }
            return (sizeD / 1024).ToString("0.00") + "TB";
        }
        private void WriteBinaryToTextBox(byte[] bytes, DataDisplayTextBox txt)
        {
            if (cbbBinaryDisplayMode.SelectedIndex == 0)
            {
                StringBuilder str = new StringBuilder();
                foreach (var i in bytes)
                {
                    str.Append(i.ToString("X2") + " ");
                }
                txt.Text = str.ToString();
            }
            else
            {
                txt.Text = Convert.ToBase64String(bytes);
            }
        }
        private void WriteLog(string log = "")
        {
            if (log == "")
            {
                txtLog.Text += Environment.NewLine;
                return;
            }
            txtLog.Text += DateTime.Now.ToString() + "\t" + log + Environment.NewLine;
        }

        public static async Task<bool> PickAndSaveFile(byte[] data, IDictionary<string, string> filter, bool AllFileType=true, string sugguestedName = null)
        {
            if (data == null || (filter == null && !AllFileType))
            {
                throw new ArgumentNullException();
            }
            if (filter.Count == 0 && !AllFileType)
            {
                throw new Exception("筛选器内容为空");
            }
            FileSavePicker picker = new FileSavePicker();
            if (filter != null && filter.Count > 0)
            {
                foreach (var i in filter)
                {
                    picker.FileTypeChoices.Add(i.Key, new List<string>() { i.Value });
                }
            }
            if (AllFileType)
            {
                picker.FileTypeChoices.Add("所有文件", new List<string>() { "." });
            }

            if (sugguestedName != null)
            {
                picker.SuggestedFileName = sugguestedName;
            }

            var file = await picker.PickSaveFileAsync();
            if(file==null)
            {
                return false;
            }
            await Task.Run(() => FileIO.WriteBufferAsync(file, data.AsBuffer()));
            return true;
        }

        private async Task<Encoding> ChooseEncoding()
        {
            Encoding encoding = null;
            MessageDialog dialog = new MessageDialog("请选择文件编码：", "保存文件");
            dialog.Commands.Add(new UICommand("UTF8",  (p1) =>
            {
                encoding = Encoding.UTF8;
            }));
            dialog.Commands.Add(new UICommand("GB2312",  (p1) =>
            {
                encoding = Encoding.GetEncoding("GB2312");
            }));
           await dialog.ShowAsync();
            return encoding;
        }

        private async void btnWeb_Click(object sender, RoutedEventArgs e)
        {
            Encoding encoding;
            if((encoding=await ChooseEncoding())!=null)
            {
                try
                {
                    if (await PickAndSaveFile(encoding.GetBytes(WebString), new Dictionary<string, string>() { { "HTML文件", ".html" }, { "HTM文件", ".htm" }, { "文本文件", ".txt" } }))
                    {
                        await new MessageDialog("保存成功，此文件包含HTML代码以外的信息，若不需要删除<html>之前的数据即可。").ShowAsync();
                    }
                }
                catch (Exception ex)
                {
                    await new MessageDialog("保存失败：" + Environment.NewLine + Environment.NewLine + ex.ToString()).ShowAsync();

                }
            }
        }

        private async void btnImg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (await PickAndSaveFile(ImageBytes, new Dictionary<string, string>() { { "png", ".bmp" }, { "文本文件", ".txt" } }))
                {
                    await new MessageDialog("保存成功。").ShowAsync();
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog("保存失败：" + Environment.NewLine + Environment.NewLine + ex.ToString()).ShowAsync();

            }
        }

        private async void btnRtf_Click(object sender, RoutedEventArgs e)
        {
            Encoding encoding;
            if ((encoding = await ChooseEncoding()) != null)
            {
                try
                {
                  if(  await PickAndSaveFile(encoding.GetBytes(RtfString), new Dictionary<string, string>() { { "RTF文件", ".rtf" }, { "文本文件", ".txt" } }))
                    {
                        await new MessageDialog("保存成功。").ShowAsync();
                    }
                }
                catch (Exception ex)
                {
                    await new MessageDialog("保存失败：" + Environment.NewLine + Environment.NewLine + ex.ToString()).ShowAsync();

                }
            }
        }



    }
}
