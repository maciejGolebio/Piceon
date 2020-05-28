﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Piceon.Models;
using System.Threading.Tasks;

namespace Piceon.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ImageGalleryWithTagFilterPage : Page
    {
        private FolderItem AccessedFolder { get; set; }
        private List<ImageItem> SelectedImages { get; set; } = new List<ImageItem>();

        public bool IsPaneOpen = false;
        public bool IsLoading = false;



        public event EventHandler ImageClicked;



        public ImageGalleryWithTagFilterPage()
        {
            this.InitializeComponent();
        }

        private void ImageGalleryPage_ImageClicked(object sender, EventArgs e)
        {
            ImageClicked?.Invoke(this, e);
        }
        public async Task AccessFolder(FolderItem folder)
        {
            AccessedFolder = folder;
            ShowTagsLoadingIndicator();
            await tagFilterPage.AccessFolderAsync(folder);
            HideTagsLoadingIndicator();
            await imageGalleryPage.AccessFolder(folder);
        }

        private async void TagFilterPage_SelectedTagsChanged(object sender, SelectedTagsChangedEventArgs e)
        {
            await imageGalleryPage.SetTagsToFilter(e.SelectedTags);
        }

        private void OpenPaneButton_Click(object sender, RoutedEventArgs e)
        {
            splitView.OpenPaneLength = 300;
            tagFilterPage.Visibility = Visibility.Visible;
            openPaneButton.Visibility = Visibility.Collapsed;
            closePaneButton.Visibility = Visibility.Visible;
            IsPaneOpen = true;

            if (IsLoading)
                loadingTextBlock.Visibility = Visibility.Visible;
        }

        private void ClosePaneButton_Click(object sender, RoutedEventArgs e)
        {
            splitView.OpenPaneLength = 40;
            tagFilterPage.Visibility = Visibility.Collapsed;
            openPaneButton.Visibility = Visibility.Visible;
            closePaneButton.Visibility = Visibility.Collapsed;
            loadingTextBlock.Visibility = Visibility.Collapsed;
            IsPaneOpen = false;
        }

        public void ShowTagsLoadingIndicator()
        {
            IsLoading = true;
            if (IsPaneOpen)
                loadingTextBlock.Visibility = Visibility.Visible;
        }

        public void HideTagsLoadingIndicator()
        {
            IsLoading = false;
            if (IsPaneOpen)
                loadingTextBlock.Visibility = Visibility.Collapsed;
        }

        public void SetSelectedTagsFromImages(List<ImageItem> images)
        {
            List<string> tagsToHighlight = new List<string>();
            images.ForEach(i =>
            {
                tagsToHighlight.AddRange(i.Tags);
            });
            tagFilterPage.HighlightTags(tagsToHighlight);
        }

        private void ImageGalleryPage_SelectionChanged(object sender, ImageGalleryPageSelectionChangedEventArgs e)
        {
            SelectedImages.Clear();
            SelectedImages.AddRange(e.SelectedItems);
            SetSelectedTagsFromImages(SelectedImages);
            if (e.SelectedItems.Any())
                openAddTagsFlyoutButton.Visibility = Visibility.Visible;
            else
                openAddTagsFlyoutButton.Visibility = Visibility.Collapsed;
        }

        private async void addTagsButton_Click(object sender, RoutedEventArgs e)
        {
            var tags = addTagsFlyoutTextBox.Text.Split(';').ToList();
            addTagsFlyoutTextBox.Text = "";
            addTagsFlyout.Hide();

            for (int i = 0; i < tags.Count; i++)
            {
                var s = tags[i].Trim();
                tags[i] = s;
            }
            foreach (var item in SelectedImages)
            {
                await item.AddTagsAsync(tags);
            }

            await tagFilterPage.AccessFolderAsync(AccessedFolder);
            SetSelectedTagsFromImages(SelectedImages);
        }

        private void openAddTagsFlyoutButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
