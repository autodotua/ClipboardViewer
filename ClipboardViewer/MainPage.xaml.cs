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

        byte[] imageBytes;
        string webString;
        string rtfString;

        private async Task GetData(DataPackageView data)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var i in lbxUsable.Items)
            {
                (i as CheckBox).IsChecked = false;
            }
     

            if (data.Contains(Bitmap))
            {
                try
                {
                    var stream = await (await data.GetBitmapAsync()).OpenReadAsync();
                    BitmapImage image = new BitmapImage();
                    await image.SetSourceAsync(stream);
                    img.Source = image;

                    byte[] bytes = new byte[stream.Size];
                    stream = await (await data.GetBitmapAsync()).OpenReadAsync();
                    await stream.AsStreamForRead().ReadAsync(bytes, 0, (int)stream.Size);

                    if (bytes.Length > 1e5)
                    {
                        imageBytes = bytes;
                        txtImg.Text = ToMushMessage;
                    }
                    else
                    {
                        WriteBinaryToTextBox(bytes, txtImg);
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
                string source = await data.GetHtmlFormatAsync();
             int index=   source.IndexOf('<');
                if(index!=-1)
                {
                    web.NavigateToString(source.Remove(0,index));
                }
                
                if(source.Length>1e5)
                {
                    webString = source;
                    txtWeb.Text = ToMushMessage;
                }
                else
                {
                    txtWeb.Text = source;
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
                string rtf = await data.GetRtfAsync();
                    rtfEdit.IsReadOnly = false;
                rtfEdit.Document.SetText(Windows.UI.Text.TextSetOptions.FormatRtf, Encoding.Unicode.GetString(new UTF8Encoding(false).GetBytes(rtf)));
                    rtfEdit.IsReadOnly = true;
                    if (rtf.Length > 1e5)
                {
                    webString = rtf;
                    txtWeb.Text = ToMushMessage;
                }
                else
                {
                    rtfTxt.Text = rtf.ToString();
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
                try
                { 
                var items = await data.GetStorageItemsAsync();
                foreach (var i in items)
                {
                    storageItems.Add(new StorageItemsBinding()
                    {
                        FileName = i.Name,
                        Size = i.Attributes.HasFlag(Windows.Storage.FileAttributes.Directory) ? "" : ToReadableSize((await i.GetBasicPropertiesAsync()).Size),
                        Path = i.Path,
                        Image = i.Attributes.HasFlag(Windows.Storage.FileAttributes.Directory) ?
                        new BitmapImage(new Uri(BaseUri, "/Assets/Fonder.png")) :
                        new BitmapImage(new Uri(BaseUri, "/Assets/File.png")),
                    });
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
            if(sizeD<1024)
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
        

        private void WriteBinaryToTextBox(byte[] bytes,DataDisplayTextBox txt)
        {
            if(cbbBinaryDisplayMode.SelectedIndex==0)
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
        private void WriteLog(string log="")
        {
            if(log=="")
            {
                txtLog.Text += Environment.NewLine;
                return;
            }
            txtLog.Text += DateTime.Now.ToString() + "\t" + log + Environment.NewLine;
        }
    }
}
