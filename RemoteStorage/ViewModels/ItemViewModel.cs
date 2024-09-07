using Azure.Storage.Blobs.Models;
using RemoteStorage.Dal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteStorage.ViewModels
{
    internal class ItemViewModel
    {
        public const string ForwardSlash = "/";

        public ObservableCollection<BlobItem> Items { get;  }
        public ObservableCollection<string> Directories { get;  }
        private string? directory;
        public string? Directory 
        { 
            get => directory;
            set
            {
                directory = value;
                Refresh();
            }
        }
        public ItemViewModel()
        {
            Items = new ObservableCollection<BlobItem>();
            Directories = new ObservableCollection<string>();
            Refresh();
        }

        private void Refresh()
        {
            Directories.Clear();
            Items.Clear();
            Repository.Container.GetBlobs().ToList().ForEach(item =>
            {
                // directories first
                if (item.Name.Contains(ForwardSlash))
                {
                    var dir = item.Name[..item.Name.LastIndexOf(ForwardSlash)];
                    if (!Directories.Contains(dir))
                    {
                        Directories.Add(dir);
                    }
                }

                // then, handle all the files from root
                if (string.IsNullOrEmpty(Directory) && !item.Name.Contains(ForwardSlash))
                {
                    Items.Add(item);
                }
                // finally if I am in the directory, put only files from that directory
                else if (!string.IsNullOrEmpty(Directory) 
                        && item.Name.StartsWith($"{Directory}{ForwardSlash}"))
                {
                    Items.Add(item);
                }

            });
        }

        public async Task  UploadAsync (string path, string directory)
        {
            var filename = path[(path.LastIndexOf(Path.DirectorySeparatorChar) + 1)..];
            var extension = Path.GetExtension(filename).TrimStart('.').ToLower();
            var extensionFilePath= $"{extension}{ForwardSlash}{ForwardSlash}{filename}";


            if (!string.IsNullOrEmpty(directory))
            {
                directory=$"{directory}{ForwardSlash}{extension}";
            }
            else
            {
                directory = extension;
            }
            var blobName= $"{directory}{ForwardSlash}{filename}";
            using var fs = File.OpenRead(path);
            await Repository.Container.GetBlobClient(blobName).UploadAsync(fs,true);
            Refresh();
        }
        public async Task DownloadAsync(BlobItem item, string path)
        {
            
            using var fs = File.OpenWrite(path);
            await Repository.Container.GetBlobClient(item.Name).DownloadToAsync(fs);
            
        }
        public async Task DeleteAsync(BlobItem item)
        {

            await Repository.Container.GetBlobClient(item.Name).DeleteAsync();
            Refresh();
        }
    }
}
