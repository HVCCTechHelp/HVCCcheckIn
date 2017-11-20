namespace HVCC.Shell.Views
{
    using DevExpress.Xpf.Editors;
    using HVCC.Shell.Helpers;
    using HVCC.Shell.ViewModels;
    using System;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Interaction logic for RelationshipDialogView.xaml
    /// </summary>
    public partial class RelationshipDialogView : UserControl
    {
        public RelationshipDialogView()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        /// <summary>
        /// Invoked from the class constructor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            RelationshipViewModel vm = ((RelationshipViewModel)this.DataContext);
            this.teFName.Validate += Te_Validate;
            this.teLName.Validate += Te_Validate;
            this.cbRelationToOwner.Validate += editform_Validate;

            this.teFName.Focus();
        }

        private void Te_Validate(object sender, ValidationEventArgs e)
        {
            string input = e.Value as string;
            if (string.IsNullOrEmpty(input))
            {
                e.IsValid = false;
            }
            else
            {
                e.IsValid = true;
            }
            e.Handled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editform_ValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            RelationshipViewModel vm = this.DataContext as RelationshipViewModel;

            // The only control that can be updated is the photo.  Therefore, if the value is
            // null, than the user cleared the image without replacing it with a new image.  In 
            // this case, we ignore the cancel and retain the existing picture.
            if (null == e.NewValue)
            {
                e.Handled = true;
                return;
            }

            //// When the ImageEdit control's value changes, we update the Photo property value of the relationship
            //// and encode the Image property value which is bound to the ImageEdit control.
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //// NOTE: I found that just checking the byte array length of the Relationship.Photo without first encoding 
            ////       the Relationship.Photo into a BitmapImage will always return different lengths.
            ////       So the workaround for this is to first convert the Relationship.Photo to a bitmap, then convert both
            ////       the existing (Photo) with the value from the ImageEdit control (e.Value) back to byte[].
            if (typeof(BitmapImage) == e.NewValue.GetType())
            {
                BitmapImage oldImage = Helper.ArrayToBitmapImage(vm.Relationship.Photo.ToArray());

                byte[] newPhoto = Helper.BitmapImageToArray(e.NewValue as BitmapImage);
                byte[] oldPhoto = Helper.BitmapImageToArray(oldImage);

                if (newPhoto.Count() != oldPhoto.Count())
                {
                    // TO-DO: These properties should be set in the ViewModel, not here.....
                    vm.Relationship.Photo = newPhoto;
                    vm.Relationship.Image = e.NewValue as BitmapImage;
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editform_Validate(object sender, ValidationEventArgs e)
        {
            //// The ImageEdit control doesn't validate the image by default, therefore
            //// we manually validate the control here so validation is passed.
            if (sender is ImageEdit)
            {
                if (((ImageEdit)sender).HasImage)
                {
                    e.IsValid = true;
                }
                else
                {
                    e.IsValid = false;
                }
            }
            // Validate the RelationToOwner ComboBox
            if (sender is ComboBoxEdit)
            {
                if (string.IsNullOrEmpty((string)e.Value))
                {
                    e.IsValid = false;
                }
                else
                {
                    e.IsValid = true;
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// Handles a drop event on the ImageEdit control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void imgEdit_Drop(object sender, DragEventArgs e)
        {
            try
            {
                RelationshipViewModel vm = ((RelationshipViewModel)this.DataContext);

                // Only supports file drag & drop
                if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    return;
                }

                //Drag the file access
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                //string path = @"C:\TEMP\";

                // Only supports single file drag & drop
                if (1 < files.Count())
                {
                    MessageBox.Show("You can only drop a single image on this control");
                    return;
                }

                //Note that, because the program supports both pulled also supports drag the past, then ListBox can receive its drag and drop files
                //In order to prevent conflict mouse clicking and dragging, need to be shielded from the program itself to drag the file
                //Here to determine whether a document from outside the program drag in, is to determine the image is in the working directory
                if (files.Length > 0 && (e.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy)
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }

                foreach (string file in files)
                {
                    try
                    {
                        //If the image is from the external drag in, make a backup copy of the file to the working directory
                        ////string destFile = path + System.IO.Path.GetFileName(file);

                        switch (e.Effects)
                        {
                            case DragDropEffects.Copy:
                                ////File.Copy(file, destFile, false);
                                if ((Path.GetExtension(file)).Contains(".png")  ||
                                    (Path.GetExtension(file)).Contains(".PNG")  ||
                                    (Path.GetExtension(file)).Contains(".jpg")  ||
                                    (Path.GetExtension(file)).Contains(".JPG")  ||
                                    (Path.GetExtension(file)).Contains(".jpeg") ||
                                    (Path.GetExtension(file)).Contains(".JPEG") ||
                                    (Path.GetExtension(file)).Contains(".gif")  ||
                                    (Path.GetExtension(file)).Contains(".GIF"))
                                {
                                    vm.Relationship.Photo = Helper.LoadImage(file);
                                    vm.Relationship.Image = Helper.ArrayToBitmapImage(vm.Relationship.Photo.ToArray());
                                }
                                else
                                {
                                    MessageBox.Show("Only JPG, GIF and PNG files are supported");
                                    return;
                                }
                                //bmi = new BitmapImage(new Uri(destFile));
                                //imgShow.Source = bmi;
                                //lstImage.Items.Add(destFile);
                                break;
                            default:
                                break;
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Already exists in this file or import the non image files！");
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error processing image file. " + ex.Message);
            }
        }

        private void LayoutGroup_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // NOTE:  This code effectivly does nothing......  Other than to ignore the <cr> being
            // used to advance the focus, which was/is the intent. Focus is still advanced via <tab>
            if (!e.Key.Equals(Key.Enter)) return;

            //var element = sender as UIElement;
            var source = e.Source as UIElement;
            if (source != null) source.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            e.Handled = true;
        }

        /// <summary>
        /// Event Handler for the unload event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unload Error: " + ex.Message);
            }
        }
    }
}
