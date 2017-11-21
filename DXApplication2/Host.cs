using HVCC.Shell.Common.Interfaces;
using HVCC.Shell.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using HVCC.Shell.Common;

namespace HVCC.Shell
{
    public class Host : IHost
    {
        private Host()
        {
            this.OpenMvvmBinders = new ObservableCollection<IMvvmBinder>();
        }

        public static Host Instance
        {
            get { return Nested.Instance; }
        }

        private class Nested
        {
            internal static readonly Host Instance = new Host();
        }

        public ObservableCollection<IMvvmBinder> OpenMvvmBinders { get; private set; }

        public void Execute(HostVerb verb, object param)
        {
            if (verb == HostVerb.Open)
            {
                if (param.ToString() == "Golf Cart")
                {
                    var binder = GetNewGolfCartView();
                    this.OpenMvvmBinders.Add(binder);
                }
            }
            else if (verb == HostVerb.Close)
            {
                var r = this.OpenMvvmBinders.Where(x => x.View == param).FirstOrDefault();
                this.OpenMvvmBinders.Remove(r);
            }
        }

        public static IMvvmBinder GetNewGolfCartView()
        {
            ////IDataContext dc = new SqlServerConnectionDataContext();
            ////IDataContext dc = new UnitTextConnectionDataContext();
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext();
            IViewModel vm = new GolfCartViewModel(dc) { Caption = "Golf Carts " };
            IView v = new HVCC.Shell.Views.GolfCartView(vm);
            return new MvvmBinder(dc, v, vm);
        }

        public void Close(IMvvmBinder mvvmBinder)
        {
            throw new NotImplementedException();
        }

        public bool PromptYesNo(string messagePrompt, string caption)
        {
            throw new NotImplementedException();
        }

        public bool? PromptYesNoCancel(string messagePrompt, string caption)
        {
            throw new NotImplementedException();
        }

        public void RefocusOrOpenViewModel(IMvvmBinder mvvmBinder)
        {
            throw new NotImplementedException();
        }

        public void ShowMessage(string message, string caption, HostMessageType messageType = HostMessageType.None)
        {
            throw new NotImplementedException();
        }
    }
}
