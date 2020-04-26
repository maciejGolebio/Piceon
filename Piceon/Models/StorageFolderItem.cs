﻿using Piceon.DatabaseAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI.Popups;

namespace Piceon.Models
{
    public class StorageFolderItem : FolderItem
    {
        private StorageFolder SourceStorageFolder { get; set; }

        private StorageFileQueryResult SourceStorageFolderImageQuery { get; set; }

        public override event EventHandler ContentsChanged;

        public static async Task<StorageFolderItem> FromStorageFolderAsync(StorageFolder folder, int databaseId = -1)
        {
            StorageFolderItem result = new StorageFolderItem
            {
                SourceStorageFolder = folder,
                Name = folder.Name
            };
            result.Subfolders = await result.GetSubfoldersAsync();

            List<string> fileTypeFilter = new List<string>
            {
                ".jpg", ".png", ".bmp", ".gif"
            };
            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter);

            // Create query and retrieve files
            result.SourceStorageFolderImageQuery = result.SourceStorageFolder.CreateFileQueryWithOptions(queryOptions);
            result.SourceStorageFolderImageQuery.ContentsChanged += result.Query_ContentsChanged;
            result.DatabaseId = databaseId;

            return result;
        }

        public override async Task<IReadOnlyList<StorageFile>> GetStorageFilesRangeAsync(int firstIndex, int length)
        {
            if (SourceStorageFolder == null || SourceStorageFolderImageQuery == null)
            {
                throw new MemberAccessException();
            }

            return await SourceStorageFolderImageQuery.GetFilesAsync((uint)firstIndex, (uint)length);
        }

        public override async Task<IReadOnlyList<ImageItem>> GetImageItemsRangeAsync(int firstIndex, int length, CancellationToken ct)
        {
            throw new NotImplementedException();
        }


        public override async Task<int> GetFilesCountAsync()
        {
            if (SourceStorageFolder == null || SourceStorageFolderImageQuery == null)
            {
                throw new MemberAccessException();
            }

            return (int)await SourceStorageFolderImageQuery.GetItemCountAsync();
        }

        protected override async Task<List<FolderItem>> GetSubfoldersAsync()
        {
            if (SourceStorageFolder == null)
            {
                throw new MemberAccessException();
            }

            var subfolders = await SourceStorageFolder.GetFoldersAsync();

            var result = new List<FolderItem>();

            foreach (var item in subfolders)
            {
                var newFolder = await FromStorageFolderAsync(item);
                newFolder.ParentFolder = this;
                result.Add(newFolder);
            }

            return result;
        }

        public override async Task RenameAsync(string newName)
        {
            if (SourceStorageFolder == null)
            {
                throw new MemberAccessException();
            }

            await SourceStorageFolder.RenameAsync(newName);
        }

        public override async Task SetParentAsync(FolderItem folder)
        {
            var messageDialog = new MessageDialog("Error: unable to move storage folder");
            messageDialog.Commands.Add(new UICommand("Close"));
            messageDialog.DefaultCommandIndex = 0;
            messageDialog.CancelCommandIndex = 0;
            await messageDialog.ShowAsync();
        }

        public override async Task DeleteAsync()
        {
            await Launcher.LaunchFolderAsync(SourceStorageFolder);
            if (DatabaseId > 0)
                await DatabaseAccessService.DeleteAccessedFolderAsync(DatabaseId);
            //await SourceStorageFolder.DeleteAsync();
            //SourceStorageFolder = null;
            //SourceStorageFolderImageQuery = null;
            //Name = null;
            //Subfolders = null;
            //ParentFolder = null;
        }

        public override async Task CheckContentAsync()
        {
            throw new NotImplementedException();
        }

        public override async Task AddFilesToFolder(IReadOnlyList<StorageFile> files)
        {
            throw new NotImplementedException();
        }

        public static bool operator ==(StorageFolderItem f1, StorageFolderItem f2)
        {
            if ((f1 is object && f2 is null) ||
                (f1 is null && f2 is object))
                return false;

            if (f1 is null && f2 is null)
                return true;

            return (f1.Name == f2.Name &&
                EqualityComparer<StorageFolder>.Default.Equals(f1.SourceStorageFolder, f2.SourceStorageFolder));
        }

        public static bool operator !=(StorageFolderItem f1, StorageFolderItem f2)
        {
            if ((f1 is object && f2 is null) ||
                (f1 is null && f2 is object))
                return true;

            if (f1 is null && f2 is null)
                return false;

            return !(f1.Name == f2.Name &&
                EqualityComparer<StorageFolder>.Default.Equals(f1.SourceStorageFolder, f2.SourceStorageFolder));
        }

        public override bool Equals(object obj)
        {
            return obj is StorageFolderItem item &&
                   Name == item.Name &&
                   EqualityComparer<StorageFolder>.Default.Equals(SourceStorageFolder, item.SourceStorageFolder);
        }

        public override int GetHashCode()
        {
            int hashCode = -422771793;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<StorageFolder>.Default.GetHashCode(SourceStorageFolder);
            return hashCode;
        }

        private void Query_ContentsChanged(IStorageQueryResultBase sender, object args)
        {
            ContentsChanged?.Invoke(this, new EventArgs());
        }
    }
}