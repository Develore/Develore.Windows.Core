
using Develore.Windows.Input;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Develore.Windows.Models
{
    public abstract class ModelBase : INotifyPropertyChanged
    {
        protected ModelBase()
        {

        }



        private PropertyChangedEventHandler PropertyChangedHandler;
        public event PropertyChangedEventHandler PropertyChanged
        {
            add { this.PropertyChangedHandler += value; }
            remove { this.PropertyChangedHandler -= value; }
        }

        private EventHandler<CommandExecutingEventArgs> CommandExecutingHandler;
        public event EventHandler<CommandExecutingEventArgs> CommandExecuting
        {
            add { this.CommandExecutingHandler += value; }
            remove { this.CommandExecutingHandler -= value; }
        }

        private EventHandler<CommandExecutedEventArgs> CommandExecutedHandler;
        public event EventHandler<CommandExecutedEventArgs> CommandExecuted
        {
            add { this.CommandExecutedHandler += value; }
            remove { this.CommandExecutedHandler -= value; }
        }


        public const string HasErrorPropertyName = "HasError";
        /// <summary>
        /// Returns <c>true</c> of the <see cref="LastError"/> property contains an exception.
        /// </summary>
        public bool HasError
        {
            get { return this.GetProperty<bool>(HasErrorPropertyName); }
            private set { this.SetProperty(HasErrorPropertyName, value); }
        }

        public const string LastErrorPropertyName = "LastError";
        /// <summary>
        /// Sets or returns an error that may occur during processing in a model class.
        /// </summary>
        public Exception LastError
        {
            get { return this.GetProperty<Exception>(LastErrorPropertyName); }
            protected set
            {
                this.SetProperty(LastErrorPropertyName, value);
                this.HasError = null != value;
            }
        }




        private Dictionary<string, object> Properties = new Dictionary<string, object>();

        protected object GetProperty(string propertyName)
        {
            return this.GetProperty<object>(propertyName);
        }

        protected T GetProperty<T>(string propertyName)
        {
            if (this.Properties.ContainsKey(propertyName) && this.Properties[propertyName] is T)
            {
                return (T)this.Properties[propertyName];
            }

            return default(T);
        }

        protected T GetProperty<T>(string propertyName, Func<T> defaultValue)
        {
            if (this.Properties.ContainsKey(propertyName) && this.Properties[propertyName] is T)
            {
                return (T)this.Properties[propertyName];
            }

            T v = default(T);

            if(SynchronizationContext.Current.TryInvoke(defaultValue, out v))
            {
                this.SetProperty(propertyName, v);
            }
            return v;
        }

        protected T GetProperty<T>(string propertyName, T defaultValue)
        {
            return this.GetProperty<T>(propertyName, delegate () { return defaultValue; });
        }

        /// <summary>
        /// Returns the property names of all properties currently stored in the model object.
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<string> GetPropertyNames()
        {
            var keys = new List<string>(this.Properties.Keys);
            return keys;
        }

        /// <summary>
        /// Enumerates all properties stored in the current model, and disposes any disposable property.
        /// </summary>
        protected void DisposeProperties()
        {
            var keys = new List<string>(this.Properties.Keys);
            foreach (var key in keys)
            {
                this.Properties[key].TryDispose();
            }
        }

        protected virtual void OnCommandExecuting(CommandExecutingEventArgs e)
        {
            if (null != this.CommandExecutingHandler)
            {
                this.CommandExecutingHandler.Invoke(this, e);
            }
        }

        protected virtual void OnCommandExecuted(CommandExecutedEventArgs e)
        {
            if (null != this.CommandExecutedHandler)
            {
                this.CommandExecutedHandler.Invoke(this, e);
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (null != this.PropertyChangedHandler)
            {
                this.PropertyChangedHandler.Invoke(this, e);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
            this.OnPropertyChanged(e);
        }

        protected void SetProperty(string propertyName, object propertyValue)
        {
            bool changed = false;
            bool referenceChanged = false;

            if (this.Properties.ContainsKey(propertyName))
            {
                object propVal = this.Properties[propertyName];
                if (!object.Equals(propVal, propertyValue))
                {
                    changed = true;
                }

                referenceChanged = !object.ReferenceEquals(propVal, propertyValue);
            }
            else
            {
                changed = true;
                referenceChanged = true;
            }

            this.Properties[propertyName] = propertyValue;

            if (changed)
            {
                this.OnPropertyChanged(propertyName);
            }

            if (referenceChanged && null != propertyValue)
            {
                if (propertyValue is INotifyCollectionChanged)
                {
                    var coll = (INotifyCollectionChanged)propertyValue;
                    coll.CollectionChanged += delegate (object sender, NotifyCollectionChangedEventArgs e)
                    {
                        this.OnPropertyChanged(propertyName);
                    };
                }

                if (propertyValue is ICommand)
                {
                    var cmd = (ICommand)propertyValue;
                    cmd.CanExecuteChanged += (s, e) =>
                    {
                        this.OnPropertyChanged(propertyName);
                    };
                }

                if (propertyValue is RelayCommand)
                {
                    var cmd = (RelayCommand)propertyValue;
                    cmd.Executing += (s, e) =>
                    {
                        this.OnCommandExecuting(e);
                    };
                    cmd.Executed += (s, e) =>
                    {
                        this.OnCommandExecuted(e);
                    };
                }
            }
        }



        private void DelayedPropertyTimerCallback(object state)
        {
            var name = state as string;
            if (!string.IsNullOrEmpty(name))
            {
                this.OnPropertyChanged(name);
            }
        }

    }
}
