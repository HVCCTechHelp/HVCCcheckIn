namespace HVCC.Property.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using HVCC.Property.Models;
    using HVCC.Property.ViewModels;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Interaction logic for AdminView.xaml
    /// </summary>
    public partial class AdminView : UserControl
    {
        public AdminView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClick_CreatePropertiesButton(object sender, RoutedEventArgs e)
        {
            PropertiesViewModel vm = ((PropertiesViewModel)this.DataContext);

            int start = Int32.Parse(txtStartLot.Text);
            int end = Int32.Parse(txtEndLot.Text);

            using (HVCCDataContext dc = new HVCCDataContext())
            {
                for (int i = start; i <= end; i++)
                {
                    Property p = new Property()
                    {
                        Section = Int32.Parse(txtSection.Text),
                        Block = Int32.Parse(txtBlock.Text),
                        Lot = i,
                        SubLot = 0
                    };
                    dc.Properties.InsertOnSubmit(p);
                }
                dc.SubmitChanges();
            }
            this.btnCreateProperties.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClick_CreateDuesButton(object sender, RoutedEventArgs e)
        {
            PropertiesViewModel vm = ((PropertiesViewModel)this.DataContext);
            bool standing = true;
            int numCarts = 0;
            decimal cartFees = System.Convert.ToDecimal(this.txtCartFees);

            using (HVCCDataContext dc = new HVCCDataContext())
            {
                foreach(Property p in vm.PropertiesList)
                {
                    List<AnnualPropertyInformation> pA = p.AnnualPropertyInformations.ToList();
                    if (pA.Count > 0)
                    {
                        standing = pA[0].IsInGoodStanding;
                        numCarts = pA[0].NumGolfCart;
                    }
                    else
                    {
                        standing = true;
                        numCarts = 0;
                    }

                    AnnualPropertyInformation a = new AnnualPropertyInformation()
                    {
                        PropertyID = p.PropertyID,
                        Year = Int32.Parse(seForYear.Text),
                        AmountOwed = System.Convert.ToDecimal(txtAnnualDues.Text),
                        IsInGoodStanding = standing,
                        NumGolfCart = numCarts,
                        CartFeesOwed = System.Convert.ToDecimal(numCarts) * cartFees
                    };
                    dc.AnnualPropertyInformations.InsertOnSubmit(a);
                }
                dc.SubmitChanges();
            }
            this.btnCreateDues.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClick_ClearFormButton(object sender, RoutedEventArgs e)
        {
            this.txtSection.Text = String.Empty;
            this.txtBlock.Text = String.Empty;
            this.txtEndLot.Text = String.Empty;
            this.btnClearForm.Visibility = Visibility.Hidden;
            this.btnCreateDues.Visibility = Visibility.Visible;
        }

        }
    }
