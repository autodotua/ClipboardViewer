using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace ClipboardViewer
{
    public class StorageItemsBinding
    {
        public string FileName { get; set; }
        public string Size { get; set; }
        public string Path { get; set; }
        public ImageSource Image { get; set; }
    }
    
}
