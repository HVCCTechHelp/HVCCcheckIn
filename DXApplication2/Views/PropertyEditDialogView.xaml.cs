namespace HVCC.Shell.Views
{
    using DevExpress.Xpf.Grid;
    using DevExpress.Xpf.Editors;
    using HVCC.Shell.Helpers;
    using HVCC.Shell.ViewModels;
    using Models;
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;
    using System.Linq;
    using System.IO;
    using System.Diagnostics;
    using System.Windows.Navigation;
    using DevExpress.Xpf.Bars;
    using DevExpress.Xpf.Docking;

    /// <summary>
    /// Interaction logic for PropertyEditDialogView.xaml
    /// </summary>
    public partial class PropertyEditDialogView : UserControl
    {
        PropertyViewModel vm = null;

        public PropertyEditDialogView()
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
            //// Bind the properties of the view  to the properties of the view model.  
            //// The properties are INotify, so when one changes it registers a PropertyChange
            //// event on the other.  Also note, this code must reside outside of the
            //// constructor or a XAML error will be thrown.
            vm = ((PropertyViewModel)this.DataContext);
            if (null != vm.ActiveRelationshipsToProperty && 0 < vm.ActiveRelationshipsToProperty.Count())
            {
                vm.SelectedRelation = vm.ActiveRelationshipsToProperty[0];
            }

            this.btnCheckIn.IsEnabled = false; // Set the default state to 'false' until the user changes any of the counts.
            this.lgNotes.Header = string.Format("Property Notes ({0})", vm.NoteCount);

            //if (null != vm)
            //{
            //    vm.PropertyChanged +=
            //        new System.ComponentModel.PropertyChangedEventHandler(this.PropertyViewModel_PropertyChanged);
            //}

        }

        /// <summary>
        /// Property Changed event handler for the view model
        /// </summary>
        /// <param name="sender">object invoking the method</param>
        /// <param name="e">property change event arguments</param>
        //protected void PropertyViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    //PropertiesViewModel vm = this.DataContext as PropertiesViewModel;

        //    switch (e.PropertyName)
        //    {
        //        case "Balance":
        //            if (vm.SelectedProperty.Balance > 0)
        //            {
        //                vm.SelectedProperty.IsInGoodStanding = false;
        //            }
        //            else
        //            {
        //                vm.SelectedProperty.IsInGoodStanding = true;
        //            }
        //            break;
        //        default:
        //            break;
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// Validates a grid row 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableViewDetail_ValidateRow(object sender, DevExpress.Xpf.Grid.GridRowValidationEventArgs e)
        {
            PropertyViewModel vm = this.DataContext as PropertyViewModel;
            Relationship row = e.Row as Relationship;

            try
            {
                if (null != row)
                {
                    // TO-DO:  <?> Add validation rules for Relationships
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Row Error: " + ex.Message);
            }
            finally
            {
                if (e.IsValid)
                {
                    // TO-DO:  ?
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// Cancels addition of new row
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableViewDetail_RowCanceled(object sender, RowEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Grid row updated event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableViewDetail_RowUpdated(object sender, RowEventArgs e)
        {
            //PropertyViewModel vm = this.DataContext as PropertyViewModel;
            Relationship row = e.Row as Relationship;
            if (null == row.Active)
            {
                vm.AssignDefaultValues(row);
            }
            e.Handled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableViewDetail_CellValueChanging(object sender, CellValueChangedEventArgs e)
        {
            this.tableViewDetail.PostEditor();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editform_ValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            try
            {
                PropertyViewModel vm = this.DataContext as PropertyViewModel;

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
                    BitmapImage oldImage = null;
                    if (null != vm.SelectedRelation.Photo)
                    {
                        oldImage = Helper.ArrayToBitmapImage(vm.SelectedRelation.Photo.ToArray());
                    }

                    byte[] newPhoto = Helper.BitmapImageToArray(e.NewValue as BitmapImage);
                    if (null != oldImage)
                    {
                        byte[] oldPhoto = Helper.BitmapImageToArray(oldImage);

                        if (newPhoto.Count() != oldPhoto.Count())
                        {
                            // TO-DO: These properties should be set in the ViewModel, not here.....
                            vm.SelectedRelation.Photo = newPhoto;
                            vm.SelectedRelation.Image = e.NewValue as BitmapImage;
                        }
                    }
                    else
                    {
                        // TO-DO: These properties should be set in the ViewModel, not here.....
                        vm.SelectedRelation.Photo = newPhoto;
                        vm.SelectedRelation.Image = e.NewValue as BitmapImage;
                    }
                }
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Img Error: " + ex.Message);
            }
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
                                if ((Path.GetExtension(file)).Contains(".png") ||
                                    (Path.GetExtension(file)).Contains(".PNG") ||
                                    (Path.GetExtension(file)).Contains(".jpg") ||
                                    (Path.GetExtension(file)).Contains(".JPG") ||
                                    (Path.GetExtension(file)).Contains(".jpeg") ||
                                    (Path.GetExtension(file)).Contains(".JPEG") ||
                                    (Path.GetExtension(file)).Contains(".gif") ||
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckIn_Clicked(object sender, RoutedEventArgs e)
        {
            this.lgCheckIn.Visibility = Visibility.Hidden;
        }

        private void checkin_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            if (this.sePoolMembers.Value > 0 ||
                this.sePoolGuests.Value > 0 ||
                this.seGolfMembers.Value > 0 ||
                this.seGolfGuests.Value > 0)
            {
                this.btnCheckIn.IsEnabled = true;
            }
            else
            {
                this.btnCheckIn.IsEnabled = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void teParcel_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            PropertyViewModel vm = this.DataContext as PropertyViewModel;

            // It is plusuable to assume that the parcel number may get entered incorrectly, or
            // need to be replaced at some point.  Therefore, I've put in a hot-key <LeftCtrl> that
            // will enable editing of this control. Otherwise the control remains read-only.
            if (e.Key == System.Windows.Input.Key.LeftCtrl && vm.ApplPermissions.CanEditPropertyInfo)
            {
                object resource = TryFindResource("TextEditEditStyle");
                this.teParcel.Style = (Style)resource;
                this.teParcel.IsReadOnly = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyDialog_Unloaded(object sender, RoutedEventArgs e)
        {
            this.btnCheckIn.IsEnabled = true; // set true before the window closes, otherwise the 'false' state is retained
            try
            {
                IDisposable disp = this.DataContext as IDisposable;
                if (null != disp)
                {
                    disp.Dispose();
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dispose Error: " + ex.Message);
            }

        }
    }
}
