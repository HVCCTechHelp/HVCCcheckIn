namespace HVCC.Property.Views
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using HVCC.Property.Models;
    using HVCC.Property.ViewModels;
    using DevExpress.Xpf.Bars;
    using DevExpress.Xpf.Grid;
    using DevExpress.Xpf.Grid.Themes;
    using DevExpress.Xpf.Core;
    using HVCC.Property.Validation;
    using DevExpress.Xpf.Editors;
    using DevExpress.Xpf.Editors.Settings;    /// <summary>
                                              /// Interaction logic for RelationshipView.xaml
                                              /// </summary>
    public partial class RelationshipView : UserControl
    {
        public RelationshipView()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        /// <summary>
        /// This method is invoked when the 'edit' option is selected from the context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editRowItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            ////MessageBox.Show("editRowItem_ItemClick");
            try
            {
                this.relationshipsTableView.AllowEditing = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error from editRowItem_ItemClick: " + ex.Message);
            }

        }


        private void view_CellValueChanging(object sender, DevExpress.Xpf.Grid.CellValueChangedEventArgs e)
        {
            try
            {
                if (e.RowHandle != GridControl.AutoFilterRowHandle)
                {
                    //if (e.Column == this.colThroughput && e.Value != null)
                    //{
                    //    ////Try parsing the value to make sure it is only contains numeric characters and is not negative.
                    //    if (int.Parse((string)e.Value) >= 0 ? false : true)
                    //    {
                    //        throw new Exception();
                    //    }
                    //}
                    //else if (e.Column == this.colThroughputCode)
                    //{
                    //    if (e.Value != null && (int)e.Value != 1)
                    //    {
                    //        ((v_Throughput)e.Row).Throughput = 0;
                    //    }
                    //}
                }

                ((TableView)sender).PostEditor();
            }
            catch
            {
                MessageBox.Show("You can only enter a non-negative numeric value into the throughput field.", "Invalid Value", MessageBoxButton.OK);
                ((TableView)sender).PostEditor();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void view_InitNewRow(object sender, DevExpress.Xpf.Grid.InitNewRowEventArgs e)
        {
            PropertiesViewModel vm = this.DataContext as PropertiesViewModel;
            vm.SelectedRelation.PhotoURI = @"C:\HVCC\Photos\HVCC.jpg";
            //Relationship row = this.relationshipGrid.GetRow(e.RowHandle) as Relationship;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void view_ValidateRow(object sender, GridRowValidationEventArgs e)
        {
            //Relationship row = this.relationshipGrid.GetRow(e.RowHandle) as Relationship;
            PropertiesViewModel viewModel = this.DataContext as PropertiesViewModel;

            Relationship row = e.Row as Relationship;
            if (null != row)
            {
                string results = ValidationRules.IsRelationshipValid(row);
                if (!String.IsNullOrEmpty(results))
                {
                    e.IsValid = false;
                    e.ErrorType = DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical;
                    e.ErrorContent = "The highlighted field cannot be blank";
                    e.SetError(e.ErrorContent);


                    Style style = new Style();
                    Style baseStyle = FindResource(new GridRowThemeKeyExtension() { ResourceKey = GridRowThemeKeys.LightweightCellStyle, ThemeName = ThemeManager.ApplicationThemeName }) as Style;
                    style.BasedOn = baseStyle;
                    style.TargetType = typeof(LightweightCellEditor);
                    style.Setters.Add(new Setter(LightweightCellEditor.BackgroundProperty, new SolidColorBrush(Colors.LightPink)));

                    //// TO-DO.... figure out how to set the style on the control
                    this.viewFName.Style = style;


                }
                //// TO-DO: figure out how to set the IsDirty property......
            }
        }


        /// <summary>
        /// Enables/Disables the controls to allow/disallow editing of the relationship
        /// </summary>
        /// <param name="state"></param>
        ////private void IsEditMode(bool state)
        ////{
        ////    if (state)
        ////    {
        ////        //// Enable the controls to allow this releationship to be edited
        ////    }
        ////    else
        ////    {
        ////        //// Disable the controls to allow this releationship to be edited
        ////    }
        ////}

        private void ItemSourceChanged(object sender, DevExpress.Xpf.Grid.ItemsSourceChangedEventArgs e)
        {
            MessageBox.Show("relationshipGrid_ItemSourceChanged");

        }

        private void EditValueChanges(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {

            if (typeof(BitmapImage) == e.NewValue.GetType())
            {
                PropertiesViewModel vm = this.DataContext as PropertiesViewModel;
                vm.SelectedRelation.PhotoURI = vm.SaveNewImage(e.NewValue as BitmapImage);
            }
        }

        object expandedRow;
        private void grid_MasterRowExpanded(object sender, RowEventArgs e)
        {
            if (expandedRow != null)
            {
                var expandedRowHandle = propertiesGrid.FindRow(expandedRow);
                propertiesGrid.CollapseMasterRow(expandedRowHandle);
            }
            expandedRow = e.Row;
        }

        /// <summary>
        /// Invoked from the class constructor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            PropertiesViewModel vm = ((PropertiesViewModel)this.DataContext);

            //// Bind the properties of the view  to the properties of the FileInformation view model.  
            //// The properties are INotify, so when one changes it registers a PropertyChange
            //// event on the other.  Also note, this code must reside outside of the
            //// constructor or a XAML error will be thrown.
            if (null != vm)
            {
                vm.PropertyChanged +=
                    new System.ComponentModel.PropertyChangedEventHandler(this.PropertiesViewModel_PropertyChanged);
            }
        }


        /// <summary>
        /// Property Changed event handler for the view model
        /// </summary>
        /// <param name="sender">object invoking the method</param>
        /// <param name="e">property change event arguments</param>
        protected void PropertiesViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            PropertiesViewModel vm = this.DataContext as PropertiesViewModel;

            switch (e.PropertyName)
            {
                // TO-DO:  The property changed isn't being called when the details are changed
                case "IsDirty":
                    if (vm.IsDirty)
                    {
                        this.grpProperties.Header += "*";
                    }
                    break;
                case "PropertiesList":
                    this.propertiesGrid.RefreshData();
                    break;

                default:
                    break;
            }
        }

    }
}
