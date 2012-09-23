using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeStreamsFace
{
    public class OptimizedObservableCollection<T> : ObservableCollection<T>
    {
        private bool suppressOnCollectionChanged;

        public void AddRange(IEnumerable<T> items)
        {
            if (null == items)
            {
                throw new ArgumentNullException("items");
            }

            if (items.Any())
            {
                try
                {
                    suppressOnCollectionChanged = true;
                    foreach (var item in items)
                    {
                        Add(item);
                    }
                }
                finally
                {
                    suppressOnCollectionChanged = false;
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        public void RemoveRange(IEnumerable<T> items)
        {
            if (null == items)
            {
                throw new ArgumentNullException("items");
            }

            if (items.Any())
            {
                try
                {
                    suppressOnCollectionChanged = true;
                    foreach (var item in items)
                    {
                        Remove(item);
                    }
                }
                finally
                {
                    suppressOnCollectionChanged = false;
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!suppressOnCollectionChanged)
            {
                base.OnCollectionChanged(e);
            }
        }
    }
}
