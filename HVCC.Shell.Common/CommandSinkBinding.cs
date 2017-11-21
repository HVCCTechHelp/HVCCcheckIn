/*
 * The ComandSinkBinding implementation was taken from Josh Smith's 
 * article "Using RoutedCommands with a ViewModel in WPF" (link:
 * http://www.codeproject.com/script/Articles/ViewDownloads.aspx?aid=28093).
 */

namespace HVCC.Shell.Common
{
    using System;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// A CommandBinding subclass that will attach its
    /// CanExecute and Executed events to the event handling
    /// methods on the object referenced by its CommandSink property.  
    /// Set the attached CommandSink property on the element 
    /// whose CommandBindings collection contain CommandSinkBindings.
    /// If you dynamically create an instance of this class and add it 
    /// to the CommandBindings of an element, you must explicitly set
    /// its CommandSink property.
    /// </summary>
    public class CommandSinkBinding : CommandBinding
    {
        #region CommandSink [instance property]
        private ICommandSink commandSink;

        public ICommandSink CommandSink
        {
            get
            {
                return this.commandSink;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Cannot set CommandSink to null.");
                }

                if (this.commandSink != null)
                {
                    throw new InvalidOperationException("Cannot set CommandSink more than once.");
                }

                this.commandSink = value;

                this.CanExecute += (s, e) =>
                {
                    bool handled;
                    e.CanExecute = this.commandSink.CanExecuteCommand(e.Command, e.Parameter, out handled);
                    e.Handled = handled;
                };

                this.Executed += (s, e) =>
                {
                    bool handled;
                    this.commandSink.ExecuteCommand(e.Command, e.Parameter, out handled);
                    e.Handled = handled;
                };
            }
        }
        #endregion // CommandSink [instance property]

        #region CommandSink [attached property]
        public static ICommandSink GetCommandSink(DependencyObject obj)
        {
            return (ICommandSink)obj.GetValue(CommandSinkProperty);
        }

        public static void SetCommandSink(DependencyObject obj, ICommandSink value)
        {
            obj.SetValue(CommandSinkProperty, value);
        }

        public static readonly DependencyProperty CommandSinkProperty =
            DependencyProperty.RegisterAttached(
            "CommandSink",
            typeof(ICommandSink),
            typeof(CommandSinkBinding),
            new UIPropertyMetadata(null, OnCommandSinkChanged));

        private static void OnCommandSinkChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            ICommandSink commandSink = e.NewValue as ICommandSink;

            if (!ConfigureDelayedProcessing(depObj, commandSink))
            {
                ProcessCommandSinkChanged(depObj, commandSink);
            }
        }

        // This method is necessary when the CommandSink attached property is set on an element 
        // in a template, or any other situation in which the element's CommandBindings have not 
        // yet had a chance to be created and added to its CommandBindings collection.
        private static bool ConfigureDelayedProcessing(DependencyObject depObj, ICommandSink commandSink)
        {
            bool isDelayed = false;

            CommonElement elem = new CommonElement(depObj);
            if (elem.IsValid && !elem.IsLoaded)
            {
                RoutedEventHandler handler = null;
                handler = delegate
                {
                    elem.Loaded -= handler;
                    ProcessCommandSinkChanged(depObj, commandSink);
                };
                elem.Loaded += handler;
                isDelayed = true;
            }

            return isDelayed;
        }

        private static void ProcessCommandSinkChanged(DependencyObject depObj, ICommandSink commandSink)
        {
            CommandBindingCollection cmdBindings = GetCommandBindings(depObj);
            if (cmdBindings == null)
            {
                throw new ArgumentException("The CommandSinkBinding.CommandSink attached property was set on an element that does not support CommandBindings.");
            }

            foreach (CommandBinding cmdBinding in cmdBindings)
            {
                CommandSinkBinding csb = cmdBinding as CommandSinkBinding;
                if (csb != null && csb.CommandSink == null)
                {
                    csb.CommandSink = commandSink;
                }
            }
        }

        private static CommandBindingCollection GetCommandBindings(DependencyObject depObj)
        {
            var elem = new CommonElement(depObj);
            return elem.IsValid ? elem.CommandBindings : null;
        }
        #endregion // CommandSink [attached property]

        #region CommonElement [nested class]
        /// <summary>
        /// This class makes it easier to write code that works 
        /// with the common members of both the FrameworkElement
        /// and FrameworkContentElement classes.
        /// </summary>
        private class CommonElement
        {
            public readonly bool IsValid;

            public CommonElement(DependencyObject depObj)
            {
                this.frameworkElement = depObj as FrameworkElement;
                this.frameworkContentElement = depObj as FrameworkContentElement;

                this.IsValid = this.frameworkElement != null || this.frameworkContentElement != null;
            }

            public CommandBindingCollection CommandBindings
            {
                get
                {
                    this.Verify();

                    if (this.frameworkElement != null)
                    {
                        return this.frameworkElement.CommandBindings;
                    }
                    else
                    {
                        return this.frameworkContentElement.CommandBindings;
                    }
                }
            }

            public bool IsLoaded
            {
                get
                {
                    this.Verify();

                    if (this.frameworkElement != null)
                    {
                        return this.frameworkElement.IsLoaded;
                    }
                    else
                    {
                        return this.frameworkContentElement.IsLoaded;
                    }
                }
            }

            public event RoutedEventHandler Loaded
            {
                add
                {
                    this.Verify();

                    if (this.frameworkElement != null)
                    {
                        this.frameworkElement.Loaded += value;
                    }
                    else
                    {
                        this.frameworkContentElement.Loaded += value;
                    }
                }

                remove
                {
                    this.Verify();

                    if (this.frameworkElement != null)
                    {
                        this.frameworkElement.Loaded -= value;
                    }
                    else
                    {
                        this.frameworkContentElement.Loaded -= value;
                    }
                }
            }

            private void Verify()
            {
                if (!this.IsValid)
                {
                    throw new InvalidOperationException("Cannot use an invalid CommmonElement.");
                }
            }

            private readonly FrameworkElement frameworkElement;
            private readonly FrameworkContentElement frameworkContentElement;
        }
        #endregion // CommonElement [nested class]
    }
}