﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Piceon.DatabaseAccess;
using Piceon.Models;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Search;

namespace Piceon.Services
{
    public static class FolderManagerService
    {
        public static async Task<FolderItem> OpenFolderAsync()
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };

            folderPicker.FileTypeFilter.Add("*");
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder is object)
            {
                var dbvf = await AddToDatabase(folder);
                string token = StorageApplicationPermissions.FutureAccessList.Add(folder);
                return await VirtualFolderItem.FromDatabaseVirtualFolder(dbvf);
            }

            return null;
        }

        private static async Task<DatabaseVirtualFolder> AddToDatabase(StorageFolder folder, int parentId = -1)
        {
            var dbvf = await DatabaseAccessService.InsertVirtualFolderAsync(folder.Name, parentId);
            var subs = await folder.GetFoldersAsync();

            foreach(var item in subs)
            {
                await AddToDatabase(item, dbvf.Id);
            }

            List<string> fileTypeFilter = new List<string>();
            fileTypeFilter.Add(".jpg");
            fileTypeFilter.Add(".png");
            fileTypeFilter.Add(".bmp");
            fileTypeFilter.Add(".gif");
            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter);
            queryOptions.FolderDepth = FolderDepth.Shallow;
            var files = await folder.CreateFileQueryWithOptions(queryOptions).GetFilesAsync();

            foreach (var file in files)
            {
                await DatabaseAccessService.InsertImageAsync(file.Path, dbvf.Id);
            }

            return dbvf;
        }

        public static async Task<List<FolderItem>> GetAllFolders()
        {
            var result = new List<FolderItem>();

            var virtualFoldersRootNodes = await DatabaseAccessService.GetRootVirtualFoldersAsync();

            foreach (var item in virtualFoldersRootNodes)
            {
                result.Add(await VirtualFolderItem.FromDatabaseVirtualFolder(item));
            }

            var tokenTupleList = await DatabaseAccessService.GetAccessedFoldersAsync();

            foreach (var tokenTuple in tokenTupleList)
            {
                var storageFolder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(tokenTuple.Item2);
                result.Add(await StorageFolderItem.FromStorageFolderAsync(storageFolder, tokenTuple.Item1));
            }

            return result;
        }

        public static async Task PickAndImportImagesToFolder(FolderItem folder)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".gif");

            var files = await picker.PickMultipleFilesAsync();
            if (files != null)
            {
                await folder.AddFilesToFolder(files);
            }
        }
    }
}